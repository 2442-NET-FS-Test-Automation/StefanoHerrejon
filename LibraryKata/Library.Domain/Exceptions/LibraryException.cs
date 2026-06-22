namespace LibraryKata.Domain;

// An exception is any class that inherits from the base Exception class
public class LibraryException : Exception
{
    // The base class just contains a message.
    public LibraryException(string message) : base(message) { }

}