using System.Collections;
namespace LibraryKata.Domain;

//Second half of my class
//I don't have to mirror the interface implementation or any inheritance across both class files
//However, I can still only inherit from one parent
public partial class Catalog : IEnumerable<LibraryItem>
{

    //This is the one that we actually want to provide logic for, the one that uses a generic
    public IEnumerator<LibraryItem> GetEnumerator()
    {
        foreach (LibraryItem item in _items)
        {
            //we want to lazily return items one at a time, we dont want to return a second list
            //or anything lik that. We will use "yield" with out return
            yield return item;
        }
    }

    //This version (non - generic version) is OLD - kept in IEnumerable for backwards compatibility reasons
    //what we are doing is simply routing it to the IEnumerator<LibraryItem> GetEnumerator() method
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    //Lets make a method to return only Lendable items /Things that implement ILendable
    public IEnumerable<LibraryItem> Lendable()
    {

        foreach(LibraryItem item in _items)
        {
            if(item is ILendable)
            {//Checking the type via "is". InstanceOf
                yield return item;
            }    
        }
    }

    //Search function for the catalog
    //We are going to use Predicate to pass a delegate to our function
    //A delegate is justa  reference to method in an argument list
    //Predicate<LibraryItem> match represents a function that takes a libraryItem, and return a boolean

    //when we call this FInd() method, we will combine it with a lambda expression. 
    // Lambda's are the c# implementation of annonymous or arrow function. Just a quicl definition that we dont bother staring a reference to
    // authorItem ) Fund(item => item.Author == "Frank Herbert"); - "Find every item whos author is frank herbert
    public List<LibraryItem> Find(Predicate<LibraryItem> match)
    {
        //match is a method, not an object or a value
        //its a pointer to some method that gets passed in when we call Find()
        List<LibraryItem> foundItems = new();

        foreach(LibraryItem item in _items)
        {
            if(match(item))
            {
                foundItems.Add(item);
            }
        }

        return foundItems;
    }
}