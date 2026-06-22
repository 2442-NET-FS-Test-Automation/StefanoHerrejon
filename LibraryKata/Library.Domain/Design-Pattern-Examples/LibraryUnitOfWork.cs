using Serilog;

namespace LibraryKata.Domain;

public class LibraryUnitOfWork : IUnitOfWork
{
    // This property is mandatory because its in my interface
    public ILibraryRepository Items { get;}

    // I want something to hold my list of staged changes 
    // we will represent those as strings, this is a shallow demo example
    private readonly List<string> _staged = new();

    // We need a constructor
    // We are technically using Dependency Injection here. We never instantiate the 
    // ILibraryRepository object, we ask for an existing one. 
    public LibraryUnitOfWork(ILibraryRepository items)
    {
        Items = items;
    }

    public int Commit()
    {
        // Shallow commit implementation
        // We will just log how many things were staged + commited
        int count = _staged.Count; // how many things are in staging at commit time?

        // Log the count via Serilog
        Log.Information("LibraryUnitOfWork commited {Count} staged change(s)", count);

        // Once you're done doing whatever work you needed to do, clear the staging area
        // same logic as git
        _staged.Clear();

        return count;
    }

    public void Stage(string change)
    {   
        _staged.Add(change); // staging a change
    }
}