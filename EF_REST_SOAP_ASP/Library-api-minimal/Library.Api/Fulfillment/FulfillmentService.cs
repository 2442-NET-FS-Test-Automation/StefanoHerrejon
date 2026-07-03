//This class will hold the business logic/db retry logic fulfilling transactions
using System.Data;
using Library.Data;
using Library.Data.Entities;
using Library.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Library.Api.Fulfillment;

//ASP.NET's builder (DI container) NEEDS us to provide 2 things when we register a service
//An interface and a concrete implemetnation. These can both go in the same file

public interface IFulfillmentService
{
    public Task<FulFillmentResult> FulfillOneAsync(int orderId, CancellationToken ct);
}

//I am going to stick everything about order fulfillment in this file
//Request are wither Fulfilled or Backordered - no other result possible
public enum FulFillmentResult{ Fulfilled, Backordered}

//Also going to make a record for the result of a Burst (many orders at the same time)
//records are lightweight custom types that allow for comparison with ==
public record BurstResult(int Fulfilled, int BackOrdered);

public class FulFillmentService : IFulfillmentService
{
    // ASP.NET manages the creation (and destruction) of all our dependencies across our app
    //if we need a DBContext or Logger or any other dependency
    //we DO NOT instantiate one here, we ask for one via the Constructor
    private readonly IDbContextFactory<LibraryDbContext> _factory;

    //The factory in the constructor argyments list comes from the ASP.NET DI COntainer
    public FulFillmentService(IDbContextFactory<LibraryDbContext> factory)
    {
        _factory = factory;
    }

    //This method is going to handle fulfillment - its fonna be a bit long which is why we didnt 
    //just write all of if
    public async Task<FulFillmentResult> FulFillOneAsync(int orderId, CancellationToken ct)
    {
        //First - we need a db contect
        await using var db = await _factory.CreateDbContextAsync(ct);

        //lets grab our order from the database
        //flow for this - a costumer places an order. It hits the order table - we are now fulfilling that order
        var order = await db.Orders.Include(a => a.Lines).FirstAsync(a => a.Id == orderId, ct); //LINQ with async

        //lest create that dictionary with the productId and the OrderId value
        //Yay for LINQ/COllections namespace
        var requested = order.Lines.ToDictionary(l => l.ProductId, l => l.OrderId);


        //Creatint a flag for "can I continue fulfilling this order"
        bool canFulfill = true;

        foreach(OrderLines line in order.Lines)
        {
            //First - grab the current inventory from the db for the product
            InventoryItem inv = await db.Inventory.FirstAsync(i => i.ProductId == line.ProductId, ct); 

            //Next - check if we can meet the order 
            if(inv.CurrentStock < line.Quantity)
            {
                canFulfill = false;
                break; //this breaks us out
            }

            inv.CurrentStock -= line.Quantity; //This write to the InventoryItem table is guarded by RowVersio
        }

        //assuming that we broke out of the foreach and can not fullfill the order
        if(!canFulfill) //checking for canFulfill == false
        {
            //We cant fulfill this order, ts now Backordered
            order.Status = Status.Backordered;

            //Create a new fulfillment event record for this transaction, setting it to be backorder
            db.FulFillmentEvents.Add(new FulFillmentEvent {OrderId = orderId, Type = "Backorder"});
            await db.SaveChangesAsync(ct);

            //Log the transaction, using the Serilog structured Logging syntax
            Log.Warning("Backordered {orderId}: insufficient stock", orderId);
            
            return FulFillmentResult.Backordered;
        }

        //If we make it here, we CAN fulfill that order
        order.Status = Status.Fullfilled;
        order.CompletedUtc = DateTime.UtcNow;
        db.FulFillmentEvents.Add(new FulFillmentEvent{OrderId = orderId, Type = "Fulfilled"});



        //Adding our retry save method
        if(!await SaveWithRetryAsync(db,requested, ct)) //IF we enter this if - we lost enough times that stock dropped
        //and this order was backordered
        {
            db.ChangeTracker.Clear(); //Clear change tracker
            Order staleOrder = await db.Orders.FirstAsync(a => a.Id == orderId, ct); //grab stale order from the db
            staleOrder.Status = Status.Backordered; //Set its status to backordered
            Log.Warning("Backordered order {OrderId} after concurrency retry", orderId);
            return FulFillmentResult.Backordered;
        }
        Log.Information("Fulfilled order: {orderId}, {LineCount} lines", orderId, order.Lines.Count);
        return FulFillmentResult.Fulfilled;
    }

    //Lets breack the logic for saving with retry (via RowVersion) into its onw method
    //jus tto help keep things straight. IReadOlyDictionary just makes any dict we pass in readonly
    private static async Task<bool> SaveWithRetryAsync(
        LibraryDbContext db, IReadOnlyDictionary<int,int> requesterdByProductId, CancellationToken ct)
    {
        //This is that RowVersion Change Tracker entry retry from yesterday
        //Lest set max retries to 3 - by wrapping everythin in a loop
        for(int attempt = 0; ; attempt++)
        {
            //Our loop as written never exits - it does increment attempt for us
            //if we retry and fail x amount of times - we will throw an exception manually
            try
            {
                //The DBContext inside this method came from FulFillOneAsync - if it as changes
                //stages to it - we can save them here. Its the same object
                await db.SaveChangesAsync(ct);
                return true;   
            }//We can tell our try catch how many timpes to handle this exception for us
            catch(DbUpdateConcurrencyException ex) when (attempt < 3)
            {
                //Retry loggic - remember that change tracker stuff?
                //entry is an EF Core Change tracker entry
                foreach(var entry in ex.Entries)
                {
                    var current = await entry.GetDatabaseValuesAsync(); //grab the curent database values

                    //If some other user deleted the entry out from under us... we can't save
                    //Return false
                    if(current is null) return false;

                    //Set the OriginalValues bucket on the entry to what they currently are
                    entry.OriginalValues.SetValues(current);

                    if(entry.Entity is InventoryItem inv)
                    {
                        //Grab the current total for that item's stock
                        int freshValue = current.GetValue<int>(nameof(InventoryItem.CurrentStock));
                        //Dictionary lookup against the dict we passed in
                        int desiredAmount = requesterdByProductId[inv.ProductId];

                        //Re-check on the fresh stock - don't blindly trust it
                        if(freshValue < desiredAmount) return false;
                        inv.CurrentStock = freshValue - desiredAmount;
                    }
                }
            }
        }
    }

    public Task<FulFillmentResult> FulfillOneAsync(int orderId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}