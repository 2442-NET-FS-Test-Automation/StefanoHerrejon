namespace LibraryKata.Domain;

public class ItemNotFoundException : LibraryException
{
    
    // We can hold the offending Id that triggered the exception
    // we will use this for logging later
    public int Id { get; }

    public ItemNotFoundException(int id) 
        : base($"No library item with id {id}")
    {
        Id = id;
    }
}