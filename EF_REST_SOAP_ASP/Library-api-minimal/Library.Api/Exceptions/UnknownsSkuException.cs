namespace Library.Api.Fulfillment;

public sealed class UnKnownSkuException:Exception
{
    public string Sku{get;}
    public UnKnownSkuException(string sku):base ($"Unknown SKU: {sku}")
    {
        Sku = sku;
    }
}