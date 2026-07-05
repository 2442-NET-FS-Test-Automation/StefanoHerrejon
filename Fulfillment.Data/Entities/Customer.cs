using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Fulfillment.Data.Entities;

//Indicating what table they belong
[Table("Customers")]
public class Customer
{
    public int Id {get;set;}

    [Required,MaxLength(100)]//Data anottation
    public string Name{get;set;} = default!;

    [Required]
    public string Email{get;set;} = default!;

    public List<Order> Orders{get;set;} = new();
}