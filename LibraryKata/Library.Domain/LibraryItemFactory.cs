namespace LibraryKata.Domain;


// This factory class is static. It can ONLY contain static members.
// It CANNOT be instantiated
// It CANNOT be inherited from 
public static class LibraryItemFactory
{
    // Our class is responsible for creating LibraryItems of any type
    // we will use that enum here to make sure users ONLY attempt to create
    // valid types
    public static LibraryItem Create(
        ItemKind kind,
        string title,
        string author,
        int copies = 1,
        string section = "General", 
        string publisher = "N/a")
    {
        // This method is going to use a switch to call the correct constructor
        switch (kind)
        {
            case ItemKind.Book:
                return new Book(title, author, copies);
            
            case ItemKind.ReferenceBook:
                return new ReferenceBook(title, author, section);

            case ItemKind.Magazine:
                return new Magazine(title, author, copies, publisher);

            default: // No idea how you'd get here.
                throw new LibraryException($"Unknown item kind: {kind}");
        }
    }

}