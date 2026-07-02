namespace Library.Data.Entities;

public class FulFillmentEvent
{
    public int Id{get; set;}
    public int OrderId{get;set;}

    //=default!; is something we are doing for EF Core. If we were to make his nullable, 
    //we would satisfy the compiler - but what if I DONT WANT THE DATABASE column to allow null?
    //default! lets me shove some default value (varies per type) into the property creation
    public string Type{get;set;} = default!;

    public DateTime FullFilledAutUtc{get;set;} = DateTime.UtcNow;
}