namespace Fulfillment.Data;

using Fulfillment.Data.Entities;
using Fulfillment.Data.Enums;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public Priority Priority { get; set; }
    public Status Status { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedUtc { get; set; }

    // One Order -> Many OrderLines
    public List<OrderLines> Lines { get; set; } = new();

    // One Order -> One FulfillmentEvent
    public FulfillmentEvent? FulfillmentEvent { get; set; }
}
