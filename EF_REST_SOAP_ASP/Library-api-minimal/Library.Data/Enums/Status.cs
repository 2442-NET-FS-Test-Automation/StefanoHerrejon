namespace Library.Data.Enums;

public enum Status
{
    //In my application if an order is yet to be processed it is pending,
    //Fullfilled means the sale completed
    //Backorder happens when someone places a uy request we dont have stock for
    Pending,
    Fullfilled,
    Backordered
}