using System.Reflection.Metadata.Ecma335;
using Library.Data;
using Library.Data.Entities;
using Library.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Fulfillment;

//In "production" our orders would come from users, these APIs run locally
//So we could either - create a post for a single order and run a shell script or something
//or we create a seeding endpoint from here to generate some orders for us

public interface ISeeder
{
    IReadOnlyList<int> SeedOrders(int n, bool expedited);
    IReadOnlyList<int> ResetAndCreateOrders(int n);
}

public class Seeder : ISeeder
{
    //Going ahead and hardcoding some item SKUs (barcode numbers essentiall in a list)

    private static readonly string[] Skus = ["BK-001", "BK-002", "BK-003"];//BK-001
    private readonly IDbContextFactory<LibraryDbContext> _factory;

    public Seeder(IDbContextFactory<LibraryDbContext> factory)
    {
        _factory = factory;
    }
    public IReadOnlyList<int> SeedOrders(int n, bool expedited)
    {
        //Ask for a db context
        using var db = _factory.CreateDbContext();

        //Create a dictionary based on our product table the IDs in the db and skus
        var pid = db.Products.ToDictionary(p => p.Sku, p => p.Id); //Sku key - productId value

        //New list of ids
        var ids = new List<int>(n);

        //Based on n (number of orders the user want to seed)
        //LEts use a for loop to create those orders automatically

        for(int i = 0; i < n; i++)
        {
            var order = new Order
            {
                CustomerId = Random.Shared.Next(1,3), //random numberm 
                Priority = expedited ? Priority.Expedited : Priority.Normal,
                Lines = {new OrderLines {ProductId = pid[Skus[i % Skus.Length]], Quantity = 1}}
            };

            db.Orders.Add(order); //Add - stage changes in EF Core change tracker
            db.SaveChanges(); //peresist the changes
            ids.Add(order.Id); //add the created order's ID to the id list
        }

        return ids;
    }

    public IReadOnlyList<int> ResetAndCreateOrders(int n)
    {
        using var db = _factory.CreateDbContext();

        foreach(InventoryItem inv in db.Inventory)
        {
            switch(inv.ProductId)
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
        db.SaveChanges(); //Saving changes after reset
        //Similar logic to the burst - just creating mixed orders this time
        var pid = db.Products.ToDictionary(p=>p.Sku, p => p.Id);

        //n is user defined - how many orders in total do we want to make
        var ids = new List<int>(n);

        for(var i = 0; i < n; i++)
        {
            var order = new Order
            {
                CustomerId = Random.Shared.Next(1,3),
                Priority = i % 3 == 0 ? Priority.Expedited:Priority.Normal,
                //You can seed this however you like - for demo we are foinf modulo checks
                Lines = {new OrderLines{ProductId = pid[new [] {"BK-001", "BK-002", "BK-003"}[i%3]], Quantity = 1}}
            };

            db.Orders.Add(order);
            db.SaveChanges();
            ids.Add(order.Id);
        }

        return ids;
    }
}