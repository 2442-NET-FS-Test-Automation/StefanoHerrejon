namespace Fulfillment.Data.Enums;

public enum Status
{
    Pending, //Yet to be processed
    Fulfilled,//Sale is completed
    Backordered//Buy request when there is no inventory
}