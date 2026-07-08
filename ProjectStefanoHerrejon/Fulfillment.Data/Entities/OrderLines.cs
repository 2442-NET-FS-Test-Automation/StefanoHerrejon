namespace Fulfillment.Data.Entities;

public class OrderLines
{
    public int Id{get;set;}

    public int OrderId{get;set;}
    public Order Order{get;set;} = default!; //Navigation property

    public int TicketId{get;set;} //TicketsId -> ProductId
    public Ticket Ticket{get;set;} = default!;

    public int Quantity{get;set;}
}