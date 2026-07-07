using Microsoft.EntityFrameworkCore;
namespace Fulfillment.Data.Entities;

public class Ticket
{

    public int Id{get;set;}
    public string Sku{get;set;}
    public string Name{get;set;}

    [Precision(10,2)] //Data annotation for constraint -> 10 digits, 2 after decimal
    public decimal Price{get;set;}

    //Relationship
    //An inventory item is associated 1:1 to a product/Ticket
    public TicketItem? Inventory{get;set;} 

}