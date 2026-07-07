using Library.Data.Entities;
using Library.Data.Enums;

namespace Library.Api.Fulfillment;

public class OrderFactory
{
    public readonly IFulfillmentService _fs;

    public OrderFactory(IFulfillmentService fulfillment)
    {
        _fs = fulfillment;
    }

    public Order CreateOrder(string kind, int customerId, IEnumerable<(string sku, int qty)>lines)
    {
        switch(kind)
        {
            case "normal":
                return BuildOrder(Priority.Normal, customerId, lines);
            case "expedited":
                return BuildOrder(Priority.Expedited, customerId, lines);
            default:
                throw new ArgumentException($"Unknown order kind:{kind}");
        }
    }

    private Order BuildOrder(Priority priority, int customerId, IEnumerable<(string sku, int qty)> lines)
    {
        return new Order
        {
            CustomerId = customerId,
            Priority = priority,
            Status = Status.Pending,
            Lines = lines.Select(l=> new OrderLines
            {
                ProductId = _fs.ResolverProductId(l.sku), //Unknow SKU -> UnknownSkuException
                Quantity = l.qty
            }).ToList(),

        };
    }
}