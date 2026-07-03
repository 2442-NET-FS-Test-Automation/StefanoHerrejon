namespace Library.Data.Entities;
using Library.Data.Enums;
public class Order
{
    public int Id{get; set;}

    public int CustomerId{get; set;}

    public Customer Customer{get; set; } = default!;

    public Priority Priority{get; set;}

    public Status Status{get; set;}

    public DateTime CreatedUtc { get; set;} = DateTime.UtcNow;

    public DateTime? CompletedUtc{get;set;}

    //Every Order has one or more OrderLines
    //OrderLines are the actual product and quantity of a something on the order
    public List<OrderLines> Lines {get; set; } = new();
}
