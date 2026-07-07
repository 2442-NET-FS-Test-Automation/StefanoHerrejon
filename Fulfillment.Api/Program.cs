using Microsoft.EntityFrameworkCore;
using Fulfillment.Data;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Options;
using Serilog;
using Fulfillment.Data.Entities;
using Fulfillment.Api.Fulfillment;
using Fulfillment.Data.Enums;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddScoped<IFulfilmentService, FulfillmentService>(); //Fulfillment service
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

//GET Inventory/ticket by CustomerId
app.MapGet("/inventory/{id}",async (int id, FulfillmentDBContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Orders with {CustomerId} customerId",id);
    //Search via LINQ
    var tickets = db.Orders.GroupBy(o=>o.CustomerId).Select(g => new {Customer = g.Key, Orders = g.Count()});

    return tickets is null ? Results.NotFound() : Results.Ok(tickets); //Return
});


//Get inventory by value
app.MapGet("inventory/by-value", (FulfillmentDBContext db) =>
{
    return db.Inventory.Include(i => i.Ticket)
        .GroupBy(i => i.QuantityOnHand >=5 ? "Well Stocked":"low")
        .Select(g => new{tier = g.Key, count = g.Count(), units = g.Sum(i => i.QuantityOnHand)})
        .ToList();
});


//Quick Method to fulfill an order
app.MapPost("/orders", async (OrderPayLoad orderRequest,IDbContextFactory<FulfillmentDBContext> factory, 
        CancellationToken ct, IFulfilmentService fsvc) =>
{
    await using var db = await factory.CreateDbContextAsync(ct); 

    var newOrder = new Order
    {
        CustomerId = orderRequest.CustomerId,
        Priority = Priority.Normal,
        Lines = {new OrderLines{ProductId = orderRequest.ProductId, Quantity = orderRequest.Quantity}}
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
            var service = scope.ServiceProvider.GetRequiredService<IFulfilmentService>(); //Grab a fulfillment service
            await service.FulfillBurstAsync(ids, appStopping);
        }catch(Exception ex)
        {
            //The task failed
            Log.Error(ex, "Burst Fulfillment failed");
        }
    }, appStopping);
});

app.Run(); //API RUN
Log.CloseAndFlush(); //Close Loggs
public record OrderPayLoad(int ProductId, int Quantity, int CustomerId);

