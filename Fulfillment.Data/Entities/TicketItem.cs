namespace Fulfillment.Data.Entities;

public class TicketItem
{
    public int Id {get; set;} //PK
    public int TicketId {get;set;} //FK
    public Ticket Ticket{get;set;} = default!; //Default value via EF, to default to class type
    public int QuantityOnHand{get;set;} //Stock

    //Concurrency tracker
    public byte[] RowVersion{get;set;} = default!;

}