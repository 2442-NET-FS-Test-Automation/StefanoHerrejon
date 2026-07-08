namespace Fulfillment.Data.Entities;

public class FUlfillmentEvent
{
    public int Id{get;set;}
    
    public int OrderId{get;set;}
    public string Type{get;set;} = default!;

    public string Message{get;set;} = default!;
    public DateTime FulfilledAutUtc{get;set;} = DateTime.UtcNow;
}