using System.Data;
using Fulfillment.Data;
using Fulfillment.Data.Entities;
using Fulfillment.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Fulfillment.Api.Fulfillment;

public interface IFulfilmentService //Interface for fulfillment service
{
    public Task<FulfillmentResult> FulfillmentAsync(int orderId, CancellationToken ct);

}

//Request can have only 2 results. Fulfilled => order is fulfilled, Backordered => order is not filfilled
public enum FulfillmentResult{Fulfilled, Backordered}

//Class for fulfillment service aka post orders, and fulfillment
public class FulfillmentService : IFulfilmentService
{
    private readonly IDbContextFactory<FulfillmentDBContext> _factory;

    public FulfillmentService(IDbContextFactory<FulfillmentDBContext> factory)
    {
        _factory = factory;
    }

    //Method in charge of fulfillment
    //We gran the order from the dbContext via the parameter orderId
    //We create a dictionary to fill from OrderLines the productId = Quantity

    public async Task<FulfillmentResult> FulfillmentAsync(int orderId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct); //Db context through Constructor/properties

        var order = await db.Orders.Include(a => a.Lines).FirstAsync(a => a.Id == orderId, ct); //Grab the order from the dbContext vai orderId

        var requested = order.Lines.ToDictionary(l => l.ProductId, l => l.Quantity);

        bool canFulfill = true; //Flag for "can I continue fulfillinf the order"

        foreach(OrderLines line in order.Lines)
        {
            //We grab the current inventory from the ticket we want to buy
            TicketItem inv = await db.Inventory.FirstAsync(i => i.TicketId == line.ProductId, ct);

            //We check if we can fulfilled the order due to number of tickets
            if(inv.QuantityOnHand < line.Quantity)
            {
                canFulfill = false; //We have less tickets that they want to buy
                break;
            }

            inv.QuantityOnHand -= line.Quantity; //If we do have the inventory, we "sell", Guarded via RowVersion, later we try to save changes

        }

        //If we can not fulfilled the order
        if(!canFulfill)
        {
            //We set the order Status as Backordered
            order.Status = Status.Backordered; 
            //We create a fail/backordered fulfillment event
            db.FUlfillmentEvents.Add(new FUlfillmentEvent{OrderId = orderId, Type = "Backordered", Message="Not enough inventory"});
            //We make the changes (Add Order update & FulfillmentEvent new add) to the db via db>_factory
            await db.SaveChangesAsync(ct);
            //Log the success
            Log.Warning("BackOrdered {orderId}: insufficient stock",orderId); //Not enough stock warning
            //Return task enum
            return FulfillmentResult.Backordered; //Return backordered

        }

        //If we can fulfilled the order based only on current stock and Quantity to sell
        order.Status = Status.Fulfilled; //Order Status = Fulfilled
        order.CompletedUtc = DateTime.UtcNow; //Fill order.CompletedUtc as now
        //We add to the fulfillmentEvents table a new record, indicating the success
        db.FUlfillmentEvents.Add(new FUlfillmentEvent{OrderId = orderId, Type = "Fulfilled", Message ="Enough inventory"});

        //Lets try to save the changes to the db
        if(!await SaveWithRetryAsync(db, requested, ct)) //If we get a false from the function, meaning impossible to save changes
        {
            db.ChangeTracker.Clear();
            Order staleOrder = await db.Orders.FirstAsync(a => a.Id == orderId, ct);
            staleOrder.Status = Status.Backordered;
            Log.Warning("Backordered order:{orderid} after concurrency retry", orderId); //Log unsucessfull save to db
            return FulfillmentResult.Backordered; //Return baackordered
        }

        Log.Information("Fulfilled order:{orderId}, {LineCount} lines", orderId,order.Lines.Count); //Logging success
        return FulfillmentResult.Fulfilled; //Fulfilled order

    }

    //Retry saved logic
    private static async Task<bool> SaveWithRetryAsync(FulfillmentDBContext db, IReadOnlyDictionary<int,int> RequestedByProduct,
        CancellationToken ct)
    {
        //While true we will try to save the changes on the db, exit via other logic
        while(true)
        {
            try
            {   //try to save the changes on inventory.ticket.QuantityOnHand, new Fulfillment event, update order.status&completedUtc
                //Checks row version 
                await db.SaveChangesAsync(ct); 
                return true; //This break the while, happy ending
            }
            catch(DbUpdateConcurrencyException ex)
            {
                //Retry logic
                //Entry is EF Core Change tracker
                foreach(var entry in ex.Entries)
                {
                    var current = await entry.GetDatabaseValuesAsync();//Grab the current db value
                    if(current is null) return false; //If another user delete the entry we cant save 

                    entry.OriginalValues.SetValues(current);//Set the OriginalValues bucket on the entry to what they currently are
                    
                    if(entry.Entity is TicketItem inv)
                    {
                        int freshValue = current.GetValue<int>(nameof(TicketItem.QuantityOnHand));//Update Quantity on Hand 
                        int desireAmount = RequestedByProduct[inv.TicketId]; //Desire amount for sell

                        if(freshValue < desireAmount) return false; //Updated quantity is not enough for how much they want to buy
                        inv.QuantityOnHand -= desireAmount; //We can sell
                    }

                }
            }
        }
    }
}