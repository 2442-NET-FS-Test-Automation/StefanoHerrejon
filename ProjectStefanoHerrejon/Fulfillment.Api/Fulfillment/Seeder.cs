using Fulfillment.Data;
using Fulfillment.Data.Entities;
using Fulfillment.Data.Enums;
using Microsoft.EntityFrameworkCore;

public interface ISeeder
{
    IReadOnlyList<int> SeeOrders(int n, bool expedited);

    IReadOnlyList<int> ResetAndCreateOrders(int n);
    
}

public class Seeder : ISeeder
{
    private static readonly string[] Skus = ["TKT-1001","TKT-1002","TKT-1003"];

    private readonly IDbContextFactory<FulfillmentDBContext> _factory;

    public Seeder(IDbContextFactory<FulfillmentDBContext> factory)
    {
        _factory = factory;
    }

    //Send = n -> Number of new orders, expedited -> if they are express
    //Create orders random, add them to db
    //Return = list of new order ids
    public IReadOnlyList<int> SeeOrders(int n, bool expedited)
    {
        using var db = _factory.CreateDbContext();//Factory set for function

        var pid = db.Tickets.ToDictionary(p=>p.Sku, p=>p.Id); //<Sku, TicketId>

        var ids = new List<int>(n);//List with order ids

        //Lets create n number of orders automatically
        for(int i = 0; i < n; i++)
        {
            var order = new Order
            {
                CustomerId = Random.Shared.Next(1,3), //Random number representing the customer
                Priority = expedited ? Priority.Expidited : Priority.Normal, //IS it expedited or normal order? Depende on argument send
                Lines = {new OrderLines{ProductId = pid[Skus[i%Skus.Length]], Quantity = Random.Shared.Next(1,5)}}
            };
            db.Orders.Add(order); //Add new Order to DB
            db.SaveChanges(); //Save changes to db
            ids.Add(order.Id);
        }
        return ids;
    }

    //n -> Number of new orders
    //Resets inventmry and creates n orders
    //List of new order ids
    public IReadOnlyList<int> ResetAndCreateOrders(int n)
    {
        using var db = _factory.CreateDbContext();//Db Context
        foreach(TicketItem inv in db.Inventory)//Reset inventory
        {
            switch(inv.TicketId)
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

        db.SaveChanges(); //Save reset to db
        var pid = db.Tickets.ToDictionary(p => p.Sku, p => p.Id); 

        var ids = new List<int>(n); 

        for(int i = 0; i < n; i++)
        {
            var order = new Order
            {
                CustomerId = Random.Shared.Next(1,3),
                Priority = i % 3 == 0 ? Priority.Expidited:Priority.Normal,
                Lines = {new OrderLines{ProductId = pid[new []{"TKT-1001","TKT-1002","TKT-1003"}[i%3]], Quantity = 1}}
            };
            db.Orders.Add(order);
            db.SaveChanges();
            ids.Add(order.Id);
        }
        return ids;
    }
}