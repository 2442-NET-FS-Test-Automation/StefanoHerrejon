using Library.ControllerApi.DTOs;
using Library.Data;
using Library.Data.Entities;
namespace Library.ControllerApi.Services;

public class InventoryService : IInventoryService
{
    //Our inventory service, is what will call repo layer methods, so it
    //gets that dependency, not the controller layer

    private readonly IInventoryRepository _repo;

    public InventoryService(IInventoryRepository repo)
    {
        _repo = repo;
    }

    //When you first start writing your API, and you want to make sure DB access
    //is working, and het the skeleton/structure up - your methods will be very "lean"
    //that is ok

    public Task<IReadOnlyList<InventoryItem>> AllAsync()
    {
        //That is the method for now
        return _repo.GetAllAsync();
    }

    public Task<InventoryItem?> BySkuAsync(string sku)
    {
        return _repo.GetInventoryItemBySkuAsync(sku);
    }

    public Task<InventoryItem> AddAsync(InventoryCreateDTO dto)
    {
        //This is going to need a DTO - we will return to this
        return _repo.AddInventoryItemAsync(dto.Sku, dto.Name, dto.Price, dto.CurrentStock);
    }

    public Task<bool> RemoveAsync(string sku)
    {
        return _repo.RemoveBySkuAsync(sku);
    }
}