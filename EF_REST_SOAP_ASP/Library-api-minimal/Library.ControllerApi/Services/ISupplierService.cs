namespace Library.ControllerApi.Services;

public interface ISupplierclient
{
    
    Task<decimal?> GetListPriceAsync(string sku);
}