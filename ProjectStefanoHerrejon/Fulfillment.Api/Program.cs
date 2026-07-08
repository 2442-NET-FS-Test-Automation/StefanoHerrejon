using Microsoft.EntityFrameworkCore;
using Fulfillment.Data;
using Serilog;
using Fulfillment.Data.Entities;
using Fulfillment.Api.Fulfillment;
using Fulfillment.Data.Enums;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
//API
//Register things with the builder

//Configurings things on the app

//API Calls and RUN method - builder area
var builder = WebApplication.CreateBuilder(args);

//1) Give our builder a connection string to our database
var conn_string ="Server=localhost,1433;Database=SHFullFillment;User Id=sa; Password=LibraryPassword1; TrustServerCertificate=true";

//Serilog Area
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() //Write to console
    .WriteTo.File("log/fulfillment-log.txt", rollingInterval:RollingInterval.Day)//Write to file, new log file every day
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

//Swagger stuff
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//App Area ---------------------------------------------------------
var app = builder.Build();

//Swagger Stuff added to the app
app.UseSwagger();
app.UseSwaggerUI();

//Hello world
app.MapGet("/", () => "Hello World!");

//Get all items
app.MapGet("/inventory", async (FulfillmentDBContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Endpoint Inventory");
    return await db.Inventory.ToListAsync();
});

//Reset inventory endpoint
app.MapGet("/inventory/reset", (FulfillmentDBContext db, ILogger<Program> logger) =>
{
    //Serilog
    logger.LogInformation("Start of reseting DB");

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
    logger.LogInformation("Stock restock to default");
    return Results.Ok("Stock reset");
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
    return new
    {
        sequential = sw1.ElapsedMilliseconds,
        burst = sw2.ElapsedMilliseconds,
        speedup = speedupA
    };
    
});

app.Run(); //API RUN
Log.CloseAndFlush(); //Close Loggs
public record OrderPayLoad(int ProductId, int Quantity, int CustomerId);

