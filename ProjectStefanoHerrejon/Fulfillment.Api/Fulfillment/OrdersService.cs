using Fulfillment.Data.Entities;
using Fulfillment.Data;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Data.Enums;
using Serilog;
using Microsoft.IdentityModel.Tokens;
using Fulfillment.Api.Exceptions;

namespace Fulfillment.Api.Fulfillment;

public interface IOrderService
{
    Task<List<OrdersRecord>> OrdersAllTime(CancellationToken ct);
    Task<List<OrdersRecord>> OrdersToday(CancellationToken ct);
    Task<List<OrdersClient>> OrdersByClient(CancellationToken ct);
    Task<List<OrdersSingleClient>> OrderHistory(int clientNumber, CancellationToken ct);
}

public record OrdersRecord(string Status, int Count);

public record OrdersClient(int Id, string Name, int TotalOrders, int FulfilledOrders, int BackOrderedOrders, int Pending);

public record OrdersSingleClient(int Id, Priority Priority, Status Status, DateTime CreatedAt, DateTime? CompletedAt);
public class OrderService: IOrderService
{
    private readonly IDbContextFactory<FulfillmentDBContext> _factory;

    //Record for OrdersAllTime
    public OrderService(IDbContextFactory<FulfillmentDBContext> factory)
    {
        _factory = factory;
    }

    //Return a list of OrdersRecord that contains status and int total count of all orders
    public async Task<List<OrdersRecord>> OrdersAllTime(CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var report = await db.Orders.GroupBy(o => o.Status).Select(g => new OrdersRecord(g.Key.ToString(), g.Count())).ToListAsync(ct);
        Log.Information("Generated all time order report with {Count} status groups.",report.Count);
        return report;
    }

    //Returns a list of OrdersRecord that contains status and total count of orders for today
    public async Task<List<OrdersRecord>> OrdersToday(CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var report = await db.Orders.Where(o => (o.CompletedUtc.HasValue && o.CompletedUtc.Value.Date == DateTime.UtcNow.Date)
            || (o.CreatedUtc.Date == DateTime.UtcNow.Date && o.Status == Status.Pending))
            .GroupBy(o => o.Status)
            .Select(g => new OrdersRecord(g.Key.ToString(), g.Count())).ToListAsync(ct);
        
        /*
        foreach(var list in report)
        {
            switch (list.Status)
            {
                case "Backordered":
                    Log.Information("Orders for today : {today}, Backordered: {x}", DateTime.Now.Date,list.Count);
                    break;
                case "Fulfilled":
                    Log.Information("Orders for today : {today}, Fulfilled: {x}", DateTime.Now.Date,list.Count);
                    break;
                case "Pending":
                    Log.Information("Orders for today : {today}, Pending: {x}", DateTime.Now.Date,list.Count);
                    break;
                default:
                    break;
            }
        }
        */

        
    
        return report;
    }

    //Get orders by client, all clients and info as how many per status
    public async Task<List<OrdersClient>> OrdersByClient(CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        
        var report = await db.Orders
            .GroupBy(o => o.CustomerId).
            Select(g => new OrdersClient(
                g.Key,
                g.First().Customer.Name,
                g.Count(),
                g.Sum(o => o.Status == Status.Fulfilled ? 1 : 0),
                g.Sum(o => o.Status == Status.Backordered ? 1 : 0),
                g.Sum(o => o.Status == Status.Pending ? 1 : 0)  
            )).
            ToListAsync(ct);
        
        foreach(var list in report)
        {
            Log.Information("Orders for Client : {ClientId}, {ClientName}: Fulfilled: {x}, Backordered : {y}, Pending : {z} "
            , list.Id, list.Name, list.FulfilledOrders, list.BackOrderedOrders,list.Pending );
        }

        return report;
    }

    //Get all orders from a single client
    public async Task<List<OrdersSingleClient>> OrderHistory(int clientNumber, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var report = await db.Orders.Where(o => o.CustomerId == clientNumber)
            .OrderByDescending(o => o.CompletedUtc)
            .Select(g => new OrdersSingleClient(g.Id, g.Priority, g.Status, g.CreatedUtc, g.CompletedUtc)).
        ToListAsync();

        if(report.IsNullOrEmpty())
            throw new ClientNotFoundException(clientNumber);
        return report;

    }
}