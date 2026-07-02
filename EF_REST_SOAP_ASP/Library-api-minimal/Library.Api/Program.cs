using Microsoft.EntityFrameworkCore;
using Library.Data;
using Microsoft.Extensions.Options;
using Library.Data.Entities;

//This is my API Program.cs
//No main, we can think of it as 2 sections
//Registering things with the builder
//And then configuring things on the app
//And at the very bottom that app object that represents our entire API calls its run method

//Builder area
var builder = WebApplication.CreateBuilder(args);

//The first thing that we need is to five our builder a connection string to our database
var conn_string ="Server=localhost,1433;Database=LibraryMinimalDb;User Id=sa; Password=LibraryPassword1; TrustServerCertificate=true";

//Tell the builder to use out LibraryDbContext with the connection string above
//By registering our DbContext class (or even classes, technically you use one per DataBase)
//we hand off the managing of creating and destroying these DBContext objects to ASP.NET's
//dependency injection container. Like spring beans if you are familiar
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseSqlServer(conn_string));

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


//My file always ends with app.Run() - minimal API or Controller API
app.Run();
