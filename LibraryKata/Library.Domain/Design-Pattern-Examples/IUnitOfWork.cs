namespace LibraryKata.Domain;

public interface IUnitOfWork
{
    // This is not a method, this is a property
    ILibraryRepository Items { get; }

    void Stage(string change); // a method to allow us to stage changes - like "git add"

    int Commit(); // a method to actually commit those changes 
}