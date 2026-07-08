namespace Fulfillment.Data.Entities;

public class OrderLines
{
    public int Id{get;set;}

    public int OrderId{get;set;}

    public int ProductId{get;set;} //TicketId

    public int Quantity{get;set;}
}