using Microsoft.EntityFrameworkCore;
using Fulfillment.Data;
using Serilog;
using Fulfillment.Data.Entities;
using Fulfillment.Api.Fulfillment;
using Fulfillment.Data.Enums;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Fulfillment.Api.Exceptions;
//API
//Register things with the builder

//Configurings things on the app

//API Calls and RUN method - builder area
var builder = WebApplication.CreateBuilder(args);

//1) Give our builder a connection string to our database
var conn_string ="Server=localhost,1433;Database=SHFullFillment;User Id=sa; Password=LibraryPassword1; TrustServerCertificate=true";

//Serilog Area
var logFile = $"log/fulfillment-{DateTime.Now:yyyyMMdd-HHmmss}.txt"; //For 1 log per execution
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() //Write to console
    .WriteTo.File(logFile)//Write to file, new log file every execution, name based on time it started execution
    .CreateLogger();
builder.Host.UseSerilog();//Builder is told we are using serilog for logging

//Builder => use FulfillmentDBContext and string above to connecto to the db
//Managing of creating, destroying is handed off 
//We use Design Pattern Singleton
builder.Services.AddDbContext<FulfillmentDBContext>(options => options.UseSqlServer(conn_string),
    ServiceLifetime.Scoped, ServiceLifetime.Singleton);

//We can use more than one LibraryDBContext for methods, we create as many as we needed at runtime
builder.Services.AddDbContextFactory<FulfillmentDBContext>(Options => Options.UseSqlServer(conn_string));

//Register the customer service with builder 
builder.Services.AddScoped<IFulfillmentService, FulfillmentService>(); //Fulfillment service
builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddScoped<BurstPlanner>();
builder.Services.AddScoped<IOrderService,OrderService>();

//Swagger stuff
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//App Area ---------------------------------------------------------
var app = builder.Build();

//Swagger Stuff added to the app
app.UseSwagger();
app.UseSwaggerUI();

//Hello world
app.MapGet("/", () => 
{
    return "Welcome to Stefano's DEMO!!!";
});

//Get all items
app.MapGet("/inventory", async (FulfillmentDBContext db, ILogger<Program> logger) =>
{
    return await db.Inventory.ToListAsync();
});

//Reset inventory endpoint
app.MapGet("/inventory/reset", (FulfillmentDBContext db) =>
{
    //Reset default items/Seeding
    foreach(TicketItem inv in db.Inventory)
    {
        switch(inv.Id)
        {
            case 1:
                inv.QuantityOnHand = 15;
                break;
            case 2:
                inv.QuantityOnHand = 5;
                break;
            case 3:
                inv.QuantityOnHand = 3;
                break;
            default:
                break;
        }
    }

    db.SaveChanges(); //Save to DB
    Log.Logger.Information("Stock restock to default");
    return Results.Ok("Stock reset");
});

//Get number of fulfilled orders and backordered all time
app.MapGet("/orders-all-time", async (IOrderService orderService, CancellationToken ct) =>
{
    var report = await orderService.OrdersAllTime(ct);
    return Results.Ok(report);
});

//Get number of fulfilled orders and backordered all time
app.MapGet("/orders-today", async (IOrderService orderService, CancellationToken ct) =>
{
    var report = await orderService.OrdersToday(ct);
    return Results.Ok(report);
});

//Orders by client, count, all orders ? Both
//Clients history, total Count, fulfilled, backorderd, 
app.MapGet("/orders-by-client", async (IOrderService orderService, CancellationToken ct) =>
{
    var report = await orderService.OrdersByClient(ct);
    return Results.Ok(report);
});

//Orders by specific client
app.MapGet("/orders-history-client", async (int clientNumber, IOrderService orderService, CancellationToken ct) =>
{
     List<Fulfillment.Api.Fulfillment.OrdersSingleClient>report = [];
    try
    {
        report = await orderService.OrderHistory(clientNumber, ct);
    }catch(ClientNotFoundException ex)
    {
        Log.Logger.Error("No client with clientId {clientId}, message: {message}.", ex.ClientId, ex.Message);
    }catch(Exception ex)
    {
        Log.Logger.Error("No client with clientId {clientId}, message: {message}.", clientNumber, ex.Message);
    }
    
    return report;
});

//Get Pending orders
app.MapGet("orders/pending", (FulfillmentDBContext db, CancellationToken ct) =>
{
    int count = 0;
    foreach(var inv in db.Orders)
    {
        if(inv.Status == Status.Pending)
        {
            count+=1;
        }
    }

    return count;
});

//Get Pending orders
app.MapGet("orders/deletePending", async (FulfillmentDBContext db, CancellationToken ct) =>
{
    int count = 0;
    foreach(var inv in db.Orders)
    {
        if(inv.Status == Status.Pending)
        {
            db.Orders.Remove(inv);
        }
    }
    await db.SaveChangesAsync();
    Log.Logger.Information("Pending Orders deleted");

    return count;
});

//Quick Method to fulfill an order
app.MapPost("/orders", async (OrderPayLoad orderRequest,IDbContextFactory<FulfillmentDBContext> factory, 
        CancellationToken ct, IFulfillmentService fsvc) =>
{
    await using var db = await factory.CreateDbContextAsync(ct); 

    var newOrder = new Order
    {
        CustomerId = orderRequest.CustomerId,
        Priority = Priority.Normal,
        Lines = {new OrderLines{TicketId = orderRequest.ProductId, Quantity = orderRequest.Quantity}}
    };

    db.Orders.Add(newOrder); //Add new Order
    await db.SaveChangesAsync(ct); //Save the order to the db

    //Now we try to fulfill the order
    var result = await fsvc.FulfillOneAsync(newOrder.Id, ct);
    return Results.Ok(new{orderId = newOrder.Id, result = result.ToString()});
});

//Burst EndPoint
app.MapGet("/orders/burst", (int n, bool expedited, ISeeder seeder, IServiceScopeFactory scopes, IHostApplicationLifetime lifetime) =>
{
    var ids = seeder.SeeOrders(n, expedited); //Create orders via seeder
    var appStopping = lifetime.ApplicationStopping; //Cancelation token

    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = scopes.CreateScope(); //Fresh scope
            var service = scope.ServiceProvider.GetRequiredService<IFulfillmentService>(); //Grab a fulfillment service
            await service.FulfillBurstAsync(ids, appStopping);
        }catch(Exception ex)
        {
            //The task failed
            Log.Error(ex, "Burst Fulfillment failed");
        }
    }, appStopping);
});

//BenchMark - to check the differences between normal order after order and burst
app.MapGet("/benchmark", async (int n, IFulfillmentService fs, ISeeder seeder, CancellationToken ct) =>
{
    //Create orders sequantial
    var ordersSequential = seeder.SeeOrders(n, false);//Orders ids
    //Stop watch to time sequantial
    Stopwatch sw1 = new Stopwatch();
    //Start watch
    sw1.Start();
    //Start orders 

    foreach(var order in ordersSequential)
    {
        await fs.FulfillOneAsync(order, ct);
    }
    //Stop watch
    sw1.Stop();

    //Create orders burst
    var ordersBurst = seeder.ResetAndCreateOrders(n);
    //Stop watch to time burst
    Stopwatch sw2 = new Stopwatch();
    sw2.Start();
    await fs.FulfillBurstAsync(ordersBurst, ct);
    sw2.Stop();

    double speedupA = (double)sw1.ElapsedMilliseconds/sw2.ElapsedMilliseconds;

    Log.Information("Total orders {n}, Sequential time : {st}, Burst time {bt}", n, sw1.ElapsedMilliseconds,sw2.ElapsedMilliseconds);
    //Return results
    //= 1.0	No improvement. Both implementations took the same time.
    //> 1.0	The parallel version is faster.
    //< 1.0	The parallel version is slower than the sequential version.
    return new
    {
        sequential = sw1.ElapsedMilliseconds,
        burst = sw2.ElapsedMilliseconds,
        speedup = speedupA
    };
    
});

//BenchMark for PriorityQueue
app.MapGet("/benchmark-PQ", async (int n, FulfillmentDBContext db,IFulfillmentService fs, ISeeder seeder, CancellationToken ct) =>
{
    Log.Information("benchmark-PQ started");

    var ordersBurst = seeder.ResetAndCreateOrders(n);

    await fs.FulfillBurstAsync(ordersBurst, ct);

    var report = await db.Orders
        .Where(o => ordersBurst.Contains(o.Id)).OrderByDescending(o => o.Priority)
        .Select(o => new {Id = o.Id,Priority = o.Priority.ToString(), CompletedAt = o.CompletedUtc })
        .ToListAsync(ct);

    Log.Information("Total orders for benchmark-pq {n}", n);
    
    return report;//Return results
    
});


//Top Selling products from a new query
app.MapGet("/TopSellingProducts-NewOrders", async (int n, FulfillmentDBContext db, IFulfillmentService fs, ISeeder seeder,CancellationToken ct) =>
{
    //Check the most sold tickets
    //Binary Search for rank

    Log.Information("benchmark-Report started");
    //Reset inventory & make Orders
    var ordersBurst = seeder.ResetAndCreateOrders(n);

    //Complete orders
    await fs.FulfillBurstAsync(ordersBurst, ct);

    //Agruparlas por tickedId, solo aquellas que estan en ordersBurst;


    var data = await db.OrderLines
        .Where(ol => ordersBurst.Contains(ol.OrderId) && ol.Order.Status == Status.Fulfilled)
        .GroupBy(o => o.TicketId)
        .Select(g => new {
            TicketId = g.Key,
            TotalSold = g.Sum(x => x.Quantity)})
        .ToListAsync(ct);

    var report = data.Select((x, index)=> new TopProducts(x.TicketId, x.TotalSold, index+=1)).OrderByDescending(x => x.TotalSold).ToList();

    return report;
});

//Top Selling products from all time
app.MapGet("/TopSellingProducts", async (FulfillmentDBContext db, IFulfillmentService fs, ISeeder seeder,CancellationToken ct) =>
{
    //Check the most sold tickets
    //Binary Search for rank

    Log.Information("benchmark-Report started");
    //Reset inventory & make Orders


    //Agruparlas por tickedId, solo aquellas que estan en ordersBurst;


    var data = await db.OrderLines
        .Where(ol => db.Orders
            .Any(o => o.Id == ol.OrderId && o.Status == Status.Fulfilled))
        .GroupBy(ol => ol.TicketId)
        .Select(g => new
        {
            TicketId = g.Key,
            TotalSold = g.Sum(x => x.Quantity)
        })
        .OrderByDescending(x => x.TotalSold)
        .ToListAsync(ct);

    var report = data
        .Select((x, index) => new TopProducts(
            x.TicketId,
            x.TotalSold,
            index + 1))
        .ToList();

    return report;
});

//BenchMark for p7, Reports of most popular products from last orders
app.MapGet("/benchmark-Reports", async (int topRank, FulfillmentDBContext db, IFulfillmentService fs, ISeeder seeder, CancellationToken ct) =>
{
    //Check the most sold tickets
    //Binary Search for rank

    Log.Information("benchmark-Report-Top");

    var totals = await db.OrderLines
        .Join(
        db.Orders.Where(o => o.Status == Status.Fulfilled),
        ol => ol.OrderId,
        o => o.Id,
        (ol, o) => ol)
        .GroupBy(ol => ol.TicketId)
        .Select(g => new
        {
            TicketId = g.Key,
            TotalSold = g.Sum(x => x.Quantity)
        })
    .OrderByDescending(x => x.TotalSold)
    .ToListAsync(ct);

    var ranked = totals //rankThem based on totalSold and order from previous totals
    .Select((x, index) => new TopProducts(
        x.TicketId,
        x.TotalSold,
        index + 1))
        .ToList();

    var searchable = ranked
        .OrderBy(x => x.TicketID)
        .ToList();

    int index = searchable.BinarySearch(
    new TopProducts(topRank, 0, 0),
        Comparer<TopProducts>.Create(
        (a, b) => a.TicketID.CompareTo(b.TicketID)));

    if (index >= 0)
    {
        var product = searchable[index];

        return($"Ticket {product.TicketID} is ranked #{product.Rank} with totalsold : {product.TotalSold}");
    }else return $"No ticket with rank : {topRank}";

});

//Safe exit
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application is stopping...");
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("Application stopped.");
    Log.CloseAndFlush();
});


app.Run(); //API RUN
Log.CloseAndFlush(); //Close Loggs
public record OrderPayLoad(int ProductId, int Quantity, int CustomerId);
public record TopProducts(
    int TicketID,
    int TotalSold,
    int Rank);

