using Microsoft.EntityFrameworkCore;
using Library.Data;
using Microsoft.Extensions.Options;
using Library.Data.Entities;
using Serilog;
using Library.Api.Fulfillment;
using Library.Data.Enums;

//This is my API Program.cs
//No main, we can think of it as 2 sections
//Registering things with the builder
//And then configuring things on the app
//And at the very bottom that app object that represents our entire API calls its run method

//Builder area
var builder = WebApplication.CreateBuilder(args);

//The first thing that we need is to five our builder a connection string to our database
var conn_string ="Server=localhost,1433;Database=LibraryMinimalDb;User Id=sa; Password=LibraryPassword1; TrustServerCertificate=true";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()//Write to console, and write to a file - starting a new file each day
    .WriteTo.File("log/fullfillment-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); //Tell the builder to use Serilog for logging

//Tell the builder to use out LibraryDbContext with the connection string above
//By registering our DbContext class (or even classes, technically you use one per DataBase)
//we hand off the managing of creating and destroying these DBContext objects to ASP.NET's
//dependency injection container. Like spring beans if you are familiar

//ASP.NET has fwa different scocpe types
//transient - a new insance is created every time it's required
//Scoped - new instance for HTTP request
//Sungleton - A single instance for the entire runtime of the app
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseSqlServer(conn_string),
    ServiceLifetime.Scoped, ServiceLifetime.Singleton); //Scoped is the default, but we can be explicit - and allow for singletonScope
                                                        //When needed

//We know we will need more than one LibraryDbContext in one or more of these methods. But we dont know how many
//before runtome So we can use a DBContextFactory to create as many as we need at runtime. 
builder.Services.AddDbContextFactory<LibraryDbContext>(Options => Options.UseSqlServer(conn_string));

//Register our custom service with builder
builder.Services.AddScoped<IFulfillmentService, FulFillmentService>();

//Swagger stuff added to builder
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//App area
var app = builder.Build();

//Swagger stuff added to app
app.UseSwagger();
app.UseSwaggerUI();

//Endpoint area
app.MapGet("/", () => "Hello World!");

//Get all items from the inventory
app.MapGet("/inventory", async (LibraryDbContext db) =>
{
    //We should probably wait this - may not matter becouse we are local
    return await db.Inventory.ToListAsync();
});

//Lets use a LINQ - Language Integrated Query
// LINQ is a library that just lest us query collecttions
//The logic actually flows from SQL DQL - You can use method OR sql query syntax
//You can even save the queries themselves as c# objects if you want to

app.MapGet("/inventory/by-value", (LibraryDbContext db ) =>
{
    return db.Inventory.Include(i => i.Product)
        .GroupBy(i=> i.CurrentStock >= 5 ? "well-stocked":"low") //group by just like in sql
        .Select(g => new {tier = g.Key, count = g.Count(), units = g.Sum(i => i.CurrentStock)})
        .ToList();
});

//Any endpoints that start with "/peek*" are diagnostic/demo
//We are going to use them to expose things like EF Core change tracking and other
//underlying behavior for learning. A real app would have no reason to expose HTTP endpoints
//to outside users to make this stuff observable

app.MapGet("/peek/tracking", (LibraryDbContext db) =>
{
    //Lets see the underlying EF Core change tracker
    var unchanged = db.Products.First(); //Grab the first object. Read but not modified => Unchanged
    var modified = db.Products.Skip(1).First(); //queried....still Unchanged as of here

    modified.Price += 1;  //state => Modified

    //When we create a new object and call the dbset's .Add() method it's state is
    //"Added" - this has not actually hit the database yet. But is tracked to be added
    db.Products.Add(new Product{Sku = "BK-TMP", Name = "Tmp", Price=1m});

    //This bit of code is the non-production demo bit
    var states = db.ChangeTracker.Entries()
        .Select(e=> new{entity = e.Entity.GetType().Name, state = e.State.ToString()})
        .ToList();
    
    //Clearing the change tracker manually
    db.ChangeTracker.Clear();

    return states;
});

//Lets manually go out of our way to create a conflict - obviously dont do this on a real app
app.MapGet("/peek/conflict", (IServiceScopeFactory scopes) =>
{
    //Manually asking for sscopes. Noramlly each endpoint method call gets its onw scope tracked
    //by ASP.NET under the goood during runtime. We can, for various reasons good and bad do this manually
    using var scopeA = scopes.CreateScope();
    using var scopeB = scopes.CreateScope();

    //Now rememeber that a dbContext is generated per scope, so we have to do that too
    var firstDb = scopeA.ServiceProvider.GetRequiredService<LibraryDbContext>();
    var secondDb = scopeB.ServiceProvider.GetRequiredService<LibraryDbContext>();

    //Each dbContext reads from the same database BUT they track cahnges independently
    //remember we gave Inventory entities a RowVersion - not just a property named RowVersion
    //but an actual OnMOddelCreation FluentAPI config for a RowVersion
    //Both of these start with the same RowVersion value
    var firstInventory = firstDb.Inventory.First(i => i.Id == 1); //RowVersion = 1
    var secondInventory = secondDb.Inventory.First(i => i.Id ==1); //RowVersion = 1

    //Lets modify one AND save its change, while just modifying the other
    firstInventory.CurrentStock--; //decrement => Modified
    firstDb.SaveChanges(); //save changes is what persists any creted, deletied or modified objects
    //That row in the DB now has a RowVersion of 2, but secondInventory still has a RowVersion of 1

    //Calling SaveChanges() above modifies the RowVersion value

    //This object, that should represent the exact same row in the DB now has a stale RowVersion
    //before EF tries to persist any changes, it will check RowVersion. It won't match
    //and an exception will be thrown
    secondInventory.CurrentStock--;

    try
    {
        secondDb.SaveChanges(); //This should fail as row versions dont match
    }catch(DbUpdateConcurrencyException ex)
    {
        //In this case we want EF to retry the UPDATE
        //Asking for the actual ChangeTracker entre that threw the exception
        //this is EF Core specific
        var entry = ex.Entries.Single(); 

        //For the entry that threw the exception - grab it's current values from the DB
        //not the object, just the values
        var current = entry.GetDatabaseValues(); //Get the current values from the database

        //Every entre in the change tracker tracks two sets of values.
        //OriginalValues = the values of the object when it was loaded from the db
        //CurrentValues = the new modified values we changed on the object in our app
        //Here we manually set the OriginalValues to the values from the DB we JUST grabbed
        entry.OriginalValues.SetValues(current!);

        //Using the entry to grab the actual item - going somewhat backwards
        ((InventoryItem)entry.Entity).CurrentStock = 
            current!.GetValue<int>(nameof(InventoryItem.CurrentStock)) - 1; //Decrement the current value

        secondDb.SaveChanges();

    }

    //I can send back specific codes via methods like. .Ok() with messages inside
    //other include Problem(), NotFound(), etc
    return Results.Ok();

});

//Endpoint to reset the stock of the items in my catalog - useful for testing and demo
//might need to hit this endpoint while we work
app.MapGet("/inventory/rest", (LibraryDbContext db, ILogger<Program> logger) =>
{
    //We just ask for an ILogger like we do our dbcontext
    //thenuse it as normal
    logger.LogInformation("Start seeing database");

    //What I want to i is reset the items that I know I stuck into the db
    foreach(InventoryItem inv in db.Inventory)
    {
        
        //I onlu want to do something if the primary key is 1,2 or 3...
        switch(inv.Id)
        {
            case 1:
                inv.CurrentStock = 5;
                break;

            case 2:
                inv.CurrentStock = 3;
                break;

            case 3:
                inv.CurrentStock = 8;
                break;
            default:
                break;
        }
    }

    db.SaveChanges(); //Persisting to db
    logger.LogInformation("Stock restock");
    return Results.Ok("Stock reset");
});
//Fulfillment stuff for orders goes down here
//Im going to take in info from the front end (swager for now)
//I have a few options
//I can take in from the uri/query string
//I can also take in parameters from the body

//Quick method to fulfill one order
app.MapPost("/orders", async (OrderPayLoad orderRequest, IDbContextFactory<LibraryDbContext> factory,
    CancellationToken ct, IFulfillmentService fsvc) =>
{
    //Remember we create our order in our db
    //And then try to crete a successful fulfillment record against the db
    await using var db = await factory.CreateDbContextAsync(ct);//ask the db context to place order

    var newOrder = new Order
    {
        CustomerId = orderRequest.CustomerId,
        Priority = Priority.Normal,
        Lines = {new OrderLines { ProductId = orderRequest.ProductId, Quantity = orderRequest.Quantity}}
    };

    db.Orders.Add(newOrder); //add new order
    await db.SaveChangesAsync(ct); //save the order to db

    //Now that we have added the order - we tru to fulfill it
    var result = await fsvc.FulfillOneAsync(newOrder.Id, ct); //newOrder is now on the db, 
    return Results.Ok(new{orderId = newOrder.Id, result = result.ToString()});
});

//My file always ends with app.Run() - minimal API or Controller API
app.Run();
Log.CloseAndFlush();

public record OrderPayLoad(int ProductId, int Quantity, int CustomerId);

