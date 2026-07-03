using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using DsaThreating;

Console.WriteLine("Hello, World!");
await ThreadingDemo();
/*//Searches && Sorts
Console.WriteLine(Searches.BinarySearch([1,2,3,4,5,7,8,9,15,24,35,42,58,65,70],25));

Console.WriteLine("Bubble Sort");
int[] arr = Sorts.BubbleSort([1,50,-43,-1,0,2,6,88,4,2,3,44,5]);
foreach(int i in arr)
{
    Console.Write($"{i},");
}
Console.WriteLine("\nMerge Sort");
int[] arr2 = Sorts.Merge([1,50,-43,-1,0,2,6,88,4,2,3,44,5]);
foreach(int i in arr2)
{
    Console.Write($"{i},");
}
*/

static async Task ThreadingDemo()
{
    //Lets takea look at how c# manages Threats (OS thrads not CPU threads)
    //In c# Threads are an onject - Like everything else. Typically they are managed
    //by the runtime behind the scenes. For example, when this main runs to print "Hello World"
    //a thread object is created to handle that work

    Console.WriteLine($"Main runs on thread #{Environment.CurrentManagedThreadId}");

    //We can create our won threads - using the Thread class. It's constructori takes one argument
    //It takes a deleagate (We can define with a lambda OR pass it some prewritten method) to run
    //inside the thread
    var workerThread = new Thread(() =>{
        Console.WriteLine($"Hello from Thread #{Environment.CurrentManagedThreadId}");
    });

    //Once we have a thread setup - we have a manually
    Console.WriteLine($"Before Start() call, isAlive = {workerThread.IsAlive}"); //Unstarted

    workerThread.Start(); //Thread is now running
    Console.WriteLine($"During thread delegate code, isAlive = {workerThread.IsAlive}"); //Unstarted
    workerThread.Join();  //Our thread was called from the Main function's thread
    //calling .join() blocks the outer/caller thread similar to an await

    Console.WriteLine($"After join() call, isAlive = {workerThread.IsAlive}"); //stopped

    //Parallelism vs concurrency
    //Interleaving - Below even the runtome the actual OS scheduler (the thing the kernel uses to map
    //os threads to CPU threads) interlayers the threads - switches tham on and off cpu threads rally fast
    //according to rules that we cant influence from our program - so our threads dont really complete
    //in the same order 100% of the time. This can make our ccode non-deterministic - chich is a problem

    //Concurrency . tasks in progress (interleaved, even on one cpu core)
    //Parallelis, - tasks executing on the same time (Multiple cpu cores)

    //Threads gives us concurrency, true parallelism depends on the hardware (and kernel)

    var threads = new List<Thread>(); //empty list of threads

    //lets just use a loop to create a few realy fast
    for(int i = 1; i <= 5; i++)
    {
        int id = i;

        var th =new Thread(() =>
        {
            Thread.Sleep(Random.Shared.Next(5,40)); //Simulating some work
            Console.WriteLine($"Worker {id} finished on thread #{Environment.CurrentManagedThreadId}");
        });

        threads.Add(th);
        th.Start();
    }

    foreach(Thread thread in threads) thread.Join(); //Join call on each thread


    //Threads safe collections

    //ordinary collections are not optimized or built with multiple threads in mind - they would corrupt or
    //more likely throw runtime exceptions if two threads delegates are accessed them concurrently
    //Thankfully there are thread safe version of commomn collections and methods

    var counts = new ConcurrentDictionary<int, int>();

    var threadPool = new List<Thread>(); //list for our threads

    for(int i = 1; i <= 8; i++)
    {
        int id = i;

        var th =new Thread(() =>
        {
            for(int k = 0; k < 1000; k++)
                counts.AddOrUpdate(id, 1, (_,prev) => prev +1);
                // In the line above, AddOrUpdate takes the key, the value and a third argument
                //a delegate to execute if the key already exists
                //_ = c# discard - indicates the key parameter is intentionally ignored becouse the
                //delegate wont use it
                //prev - the existing integer value currently stored for that key
                //prev + 1 = increment that value giving us a new key to insert
        });

        threadPool.Add(th);
        th.Start();
    }

    foreach(var th in threadPool) th.Join(); //
    Console.WriteLine($"Recorded {counts.Values.Sum()} increment across {counts.Count} threads");

    //When woring with Threaads, it's common to not manually create the threads ourselves
    //For short work items like what we did above, we can use the ThreadPool
    //The ThreadPool is just a runtime managed set of background threads that we dont have to
    //create or destroy - they are already there we can just borrow one

    //Lest make a concurrentQueue for FIFO work, we will just have is store ints
    var done = new ConcurrentQueue<int>();


    for(int i = 0; i < 5; i++)
    {
        int n = i;

        //Instead of creating a thread manually and starting it I can just ask for a thread from
        //the background ThreadPool and pass it some delegate or method to execute
        ThreadPool.QueueUserWorkItem(_ => done.Enqueue(n*n));
    }

    //Becouse we dont actually have a Threads themselves at our disposal - we'll do like a CRUD await
    while(done.Count < 5) Thread.Sleep(5); // await - but way dumber

    Console.WriteLine($"Threadpool finished. {string.Join(", ",done.OrderBy(x => x))}");

    //Tasks. We have already seen Tasks. Creating Threads, starting and joinning them manually works.
    //But its vere low level. You manage each thread, you cant return a value in a straight forward way, etc
    //Thankfully we have a Task Parallel library. Its like a moders layer on top
    ParallelSum();


    static void ParallelSum()
    {
        //Jusst a bit int array
        int[] data = Enumerable.Range(1,800000).ToArray();

        //First -lets do this totally sequantially - one thread without tasks

        var sw = Stopwatch.StartNew(); //Using the stopwatch objecr to track execution time
        long sequenatial = SumRange(data,0,data.Length);
        sw.Stop();
        Console.WriteLine($"Sequantial sum = {sequenatial}, {sw.ElapsedTicks} ticks, 1 thread");

        //Before we parallelize this, lets play with Tasks
        //Manually splitting the summing into two tasks, each gets half the tatal numbers
        Task<long> half1 = Task.Run(() => SumRange(data, 0, data.Length/2));
        Task<long> half2 = Task.Run(() => SumRange(data, data.Length/2, data.Length));

        long total = half1.Result + half2.Result; //Asking for the Result of a Task is blocking
        Console.WriteLine($"Two task sum: {total}");

        //Lets parallelize this with Task and the TPL Library
        long parallelTotal = 0;

        sw.Restart(); //restarting my stopwatch back to 0 tickets - then begin counting 

        Parallel.For(0, data.Length,
            //After we give it a start and end values for the loop - this is a For Loop
            //We give it an accumulator
            () => 0L,
            //body for each iteration on a given thread do something
            //i is the loop index, _ discards the ParallelLoopState, local is the current thread subtotal for the sum
            (i, _, local) => local + data[i],
            //localFinally: AFTER a thread finishes all its assigned items this is called
            //Adds the Thread's local Sum (the thing that start witha a value of 0L(Long))
            //to the global paralletTotal
            local => Interlocked.Add(ref parallelTotal, local) //combien per Thread sums to the outer variable
        );
        sw.Stop();
        Console.WriteLine($"Parallel sum = {parallelTotal}. {sw.ElapsedTicks} ticks, multi-thread");
    }

    static long SumRange(int[] a, int start, int end)
    {
        long sum = 0;
        for(int i = start; i < end; i++)
        {
            sum += a[i];
        }
        return sum;
    }

    RaceDemo(); //creates a race condition

    static void RaceDemo()
    {
        var bank = new Bank();
        Parallel.For(0, 100000,_ => bank.DepositUnSafe(1));//100k threads of +1 amount
        Console.WriteLine($"Unsage balance = {bank.Balance} (expected 1000000)");
        //our balance is wrong every time - and it's a different wrong answer every time
        //This is the worst kind of bug. Becouse its not deterministic
    }

    SafeDemo();
    static void SafeDemo()
    {
        var bank = new Bank();
        Parallel.For(0, 1000000,_ => bank.DepositSafe(1));
        Console.WriteLine($"SAFE balance = {bank.Balance} (expected 1000000)");
    }

    //Interlocked - lock free atomic operations against one variable
    InterLockedDemo();

    static void InterLockedDemo()
    {
        long counter = 0;
        // Interlock - faster than a lock when doing single atomic operations
        //if all you need is that - use an interlock over a lock
        Parallel.For(0,1000000,_ => Interlocked.Increment(ref counter));
        Console.WriteLine($"Interlocked = {counter} (expected 1000000)");
    }

    //Deadlocks and starvation

    //Deadlock - if two tasks cerate locks on resources the other ends up needing they can deadlock. 
    //In this case they never resulve - our console app woul de waiting forever

    //Starvation - A thread gets blocked by another thread work - and stays alive but can not progress
    //Different from a deadlock becouse the other thread is able to resolve
    //This starved thread persist

    //Cancellation Tokens
    CancellationDemo();

    //Rather than abruptly killing a thread or having it die some exception potencially leading to data loss
    //we can use a cancellation token to ASK a thread to be ended and it will do so once it has the change to exit gracefully

    static void CancellationDemo()
    {
        // Calling for a CancellationToken, having it auto-cancel after 100ms
        //Side not using: Once we exit the scope where the variable created with using lives in - dispose of it
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        CancellationToken token = cts.Token;

        var work = Task.Run(() =>
        {
            for(long i = 0; ;i++)
            {
                token.ThrowIfCancellationRequested();
                if(i %5000000 == 0){/*Some simulated work*/}
            }
        }, token);

        try
        {
            work.Wait(); // The task is going - we want to have our code wait for it here
        } //Exception filtering - for when exceptions are thrown by other exceptions
        catch(AggregateException ex) when (ex.InnerException is OperationCanceledException)
        {
            Console.WriteLine("Work was cancelled cooperatively");
        }//When doing Task parallel library stuff, we need to unwrap the AggregateExceptions
        //To allow for specific catch. Same logic as multiple catch blocks
        //just more convoluted becouse AggregateExceptions are like an exception list
        catch(AggregateException ex) when (ex.InnerException is InvalidOperationException)
        {//
            Console.WriteLine("How did you get here ?");
        }
    }

    ExceptionDemo();

    static void ExceptionDemo()
    { 
        //Our task starts uo here we call run...
        var t = Task.Run(() => throw new InvalidOperationException("Oops - but in a task"));

        //Counter - intuitively, an exception inside a task DOESN'T crash on the spot
        //We would imagine that line 273 is where the exception is thrown. Its actually
        //thrown during the t.Wait() below
        try
        {
            t.Wait();
        }
        catch(AggregateException ex)
        {
            //Aggregate exceptions themselves are kind of weird
            //one task can have several faults - so they get thrown inside on AggregateException
            Console.WriteLine($"Caught: {ex.InnerException!.Message}");
        }
    }

    //Async / await - related to but not the same as a thread
    await AsyncDemo();

    static async Task AsyncDemo()
    {
        Console.WriteLine($"Before await on thread #{Environment.CurrentManagedThreadId}");
        await Task.Delay(50); //Non Blocking wait - thread isfreed
        Console.WriteLine($"After await on thread #{Environment.CurrentManagedThreadId}");
    }
}