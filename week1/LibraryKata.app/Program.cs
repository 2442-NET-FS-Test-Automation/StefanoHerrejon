namespace LibraryKata.App;  //Namespace is: bucket or logical container for different related code files.

public class Program
{
    //Now we are moving away from the python file stule top-level statements
    //We need a class to hold our main method. 
    //The previous style with no class or main - implicity main under the hood

    // public - accesible across the program
    // static - Main can be called with out intantiating the class. We can call it directly on the class itself.
    // void - there is no return 
    public static void Main()
    {
        //When I call dotnet run (Commando to run), it finds Main() and begin code execution at the first likne of Main()
        //I wrote my code, inside DataTypesAndOperator() method, so I need to call that method from Main() to execute it.
        Program.DataTypesAndOperator();

        Program.CotrolFlows();
    }

    // private - accesible only within the class
    // static - Belings to the class not an instance of the class. We can call it directly on the class itself.
    // void - returns nothing
    private static void DataTypesAndOperator() //if I had arguments/parameters/inputs for this methods, 
    {// they will go inside the parentheses.
        Console.WriteLine(" === Data Types and Operators ===");

        //C# is a strongly typed language, which means we need to declare the data type of our variables when we create them.
        //And cannot reassign a variable to a different data type later on.

        int copies = 3; ///whole numbers
        double lateFee = 1.5; //floating point numbers (Decimals)
        bool isMember = true; //True or false values
        char self = 'A'; //SIngle character
        string title = "Clean Code"; //text, sequence of characters. Reference type, not a value type.

        //Operators are symbols that perform operations on variables and values.
        string user = "John"; //Single = is the assignment operator, it assigns the value on the right to the variable on the left.
        int total = copies * 2; //Example of arithmatic operator, like +,-,*,/
        bool isEnough = total > 4; //Example of comparison operator, like >,<,==,!=, >=, <=. 
        // Is total grater than 4? This will evaluate to true or false and assign that value to isEnough.
        bool  exactlySix = total == 6; //Example of equality operator, it checks if total is equal to 6. 
        //Unlike JS there is no === in C#, == is used for both equality and comparison.
        bool lendable = isMember && isEnough; //Example of logical operator, 
        // like && (and, both), || (or, at least one), ! (not, *-1), ^ XOR (only one of the conditions can be true, but not both).

        Console.WriteLine(title + " has " + copies + " copies, late fee is " + lateFee + " User : "+ user + "self: "+self); //String concatenation - combines strings and variables into a single string using the + operator.
        //String interpolation - allows us to embed expressions inside string literals, using the $ symbol before the string and {} to enclose the expression.
        Console.WriteLine($"Hola como estan, late Fee : {lateFee}, total: {total}, is enough: {isEnough}, lendable: {lendable}");
        
        //C# has a lot of shorthands and little shortuts that you can find and use
        //tota = total + 1; or
        total += 1; //This is a shorthand for adding 1 to total. You can use it with other operators as well, like -=, *=, /=, etc.
    }

    
    private static void CotrolFlows()//Control flows, else and switch
    {
        Console.WriteLine("\n === Control Flows ===");

        // If - elsse if- else
        int copiesAvailable = 0; 
        bool isMember = true;


        if(copiesAvailable > 1) //if copiesAvailable is greater than 1 and isMember is true, then execute the code block inside the if statement.
            Console.WriteLine("Many available for checkout");
        else if(copiesAvailable == 1) //Brackets are not needed if there is only a single line
            Console.WriteLine("Last copy");
        else
            Console.WriteLine("Out of stock");

        
        //Switch statement - is a control statement that allows us to execute different code blocks based on the value of a variable 
        string genre = "Mystery";

        // Clasic switch - notice c# cares about intent a lot!, No fall through like in other languages
        switch(genre) //switch (variable)
        {
            case "Mystery": //Case for mystery
                Console.WriteLine("Checl section A!");
                break;
            case "Science-Fiction": //Case for mystery
                Console.WriteLine("Checl section F!");
                break;
            default: //Default case if none of the above cases match, default case is optional but good practice to have it,
                    //  it will execute if none of the above cases match.
                Console.WriteLine("Check the general section!");
                break;
            
        }

        //Different way to use switch, new in .NET S, Switch Expressions! Used in real world code, eg: 
        // In a switch expression we want a return value, a return value from the switch - we can then use it to print out a result

        string section = genre switch //switch (variable) switch expression
        { //This is my expression body, it will return a value based on the value of genre
            "Mystery" => "Section A", //Case for mystery, notice the use of => instead of : and no need for break statement
            "Science-Fiction" => "Section F", //Case for science fiction
            _=> "General Section" //Default case, _ is a wildcard that matches any value, it will execute if none of the above cases match.
        };
        Console.WriteLine($"Check out section {section}!");
    }

    private static void Loops()
    {
        //C# provides for loops as well, same as java and any other languages
        // For, while, do-while, etc

        for(int day = 1; day <= 3; day++)
        {
            Console.WriteLine($"Reminder day {day}: fee so far {CalculateLateeFee(day)}");
        }

        int onShelf = 3;

        while(onShelf > 0)
        {
            Console.WriteLine($"{onShelf} capies on the shelf!");
            onShelf--; //quick decrement shorthand 
        }
        Console.WriteLine("No copies on shelf!");

        string myString = "dog";

        myString = "cat"; //This is valid, we can reassign a string variable to a different value, 
        // but we cannot change the value of a string variable, because strings are immutable in C#. 
        // Under the hood the garbage collector will create a new string object with the new value and assign it to the variable, and discard the previous string object. 
        // This is different from mutable types, like lists or arrays, where we can change the value of the variable without creating a new object.
        
    }

    private static decimal CalculateLateeFee(int daysLate) => daysLate * 2; //Simple function

    //Stack : value types / primitives but strings are on the stack/ram, the value is stored there, 
    // for reference types the value is stored on the heap and a reference to that value is stored on the stack.
    //Heap : reference types, the value is stored on the heap and a reference to that value is stored on the stack.
    private static void ArraysWork()
    {
        //C# probiddes for arrays as well as lista and other collections - we'll get to those later. 
        // An array is a collection of items of the same data type, stored in contiguous memory locations, reference type
        string[] books = {"Dune", "Harry Potter", "Percy Jackson", "Lord of the Rings"};
         //This is an array of strings, it can hold multiple values of the same data type, in this case strings. 
         // Arrays are fixed in size, once you create an array you cannot change its size, you can change the values of the elements in the array but not the size of the array.

        Console.WriteLine(books[2]); //How to access an element in the array, using the index, which starts at 0. This will print "Percy Jackson"

        //C# allows for for-each loops, which are a simpler way to iterate through an array or any collection that implements IEnumerable.
        foreach(string book in books) //for-each loop, it will iterate through each element in the array and assign it to the variable book, and execute the code block for each element.
        {
            Console.WriteLine(book); //This will print each book in the array on a new line.
        }
    }


    
}