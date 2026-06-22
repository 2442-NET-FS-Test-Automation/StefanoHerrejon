namespace LibraryKata.Domain;

public class ItemNotAvailableException : LibraryException
{
    public ItemNotAvailableException(string title)
        : base($"{title} has no copies available to borrow.") { }
}