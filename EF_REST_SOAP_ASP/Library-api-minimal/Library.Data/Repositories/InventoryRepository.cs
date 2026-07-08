using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Data;

//This class will hold my db access logic. It is concerned with looking into the db

public class InventoryRepository : IInventoryRepository
{
    //Our repo class jeeds a dn context we can ask for a dbcontext from ASP.NET DI container
    //Same pattters we have been using since day 1 of thi minimal API
    private readonly IDbContextFactory<LibraryDbContext> _factory;

    public InventoryRepository(IDbContextFactory<LibraryDbContext> factory)
    {
        _factory = factory;
    }

    //Lets make some CRUD
    //Actually pretty simple to do - becouse we dont have to concern ourself with business logic checks, etc

    //Lets write some Read Methods
    //Get all inventory items
    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync()
    {
        //Ask for db context
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Inventory.Include(i => i.Product).ToListAsync();
    }

    //Get item by its sku
    public async Task<InventoryItem?> GetInventoryItemBySkuAsync(string sku)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Inventory.Include(i=>i.Product).FirstAsync(i => i.Product.Sku ==sku);
    }

    //Lets do a simple add
    //Best practice when add is to return the added item
    public async Task<InventoryItem> AddInventoryItemAsync(string sku, string name, decimal price, int quantity)
    {
        await using var db = await _factory.CreateDbContextAsync();

        //Creatinour new item - and Product
        InventoryItem newItem = new InventoryItem
        {
            Product = new Product{Sku = sku, Name = name, Price = price},
            CurrentStock = quantity
        };

        db.Inventory.Add(newItem);
        await db.SaveChangesAsync();

        return newItem; //Because newItem is an object tracked by EF Core - EF will grab the pk for us
    }

    //Lets do a remove
    public async Task<bool> RemoveBySkuAsync(string sku)
    {
        await using var db = await _factory.CreateDbContextAsync();

        //First find the thing we want out of the datbase - grab it
        InventoryItem? itemToRemove = await db.Inventory.Include(i => i.Product)
                                    .FirstOrDefaultAsync(i => i.Product.Sku == sku);
        
        //Dont assume the search criteria prodiced a result - check for a null
        //if it is null we failed to remove it - becouse it didnt exist

        if(itemToRemove is null)
        {
            return false;
        }

        //Telling EF we want to remove this object from the db
        db.Products.Remove(itemToRemove.Product);
        await db.SaveChangesAsync();
        return true;
    }


}