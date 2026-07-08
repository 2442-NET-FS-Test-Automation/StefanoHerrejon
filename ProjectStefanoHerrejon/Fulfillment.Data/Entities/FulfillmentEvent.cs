namespace Fulfillment.Data.Entities;

public class FulfillmentEvent
{
    public int Id{get;set;}
    
    public int OrderId{get;set;} //OrdersId -> 
    // Navigation Property
    public Order Order { get; set; } = default!;
    public string Type{get;set;} = default!;

    public string Message{get;set;} = default!;
    public DateTime FulfilledAutUtc{get;set;} = DateTime.UtcNow;
}