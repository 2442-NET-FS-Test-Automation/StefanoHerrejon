using System.Resources;
using Microsoft.Identity.Client;

namespace Library.ControllerApi.Services;

public class SuplierClient : ISupplierclient
{
    //This class will call can outside api using for client

    private readonly HttpClient _http; //Comes from APS.M
    private record SupplierProduct(int Id, string Title, decimal Price);


    public SuplierClient(HttpClient http)
    {
        _http = http;
    }

    //This method sends a GET to a training APIAa called dummyjson
    //GET : https://dummmyjson.com/products/ñ{id}
    public async Task<decimal?> GetListPriceAsync(string sku)
    {
        //Lest pretend we are grabbing the "wholesale price" of our products from the supplier
        var digits = new string(sku.Where(char.IsDigit).ToArray()); //"BK-001" -> 001

        //Check to make suer we dont have a nulll in data
        if(!int.TryParse(digits, out var id)) return null;//If our data string was empty, just return nul

        var product = await _http.GetFromJsonAsync<SupplierProduct>($"producrs/{id}");

        return product?.Price;
    }
}