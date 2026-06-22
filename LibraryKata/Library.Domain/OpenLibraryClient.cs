using System.Text.Json; //Library for working with JSON - WRITTEN by Microsoft
using Serilog;

namespace LibraryKata.Domain;

public class OpenLibraryClient
{
    
    //We are going to create and use ONE HTTPClient for the entire process
    //If you use one per call, you are going to leak sockets - and evetually trigger a SocketException

    private static readonly HttpClient client = new();

    //We are going to write an async method. An async method is ANY method that calls async code
    //So, if you use something like .FindAsync() or you "await" a method call within a method body
    //The surrounding method MUST be declared async

    //A task in c# is like a promise in JS - it is a placeholder in memory telling the runtime
    //"I expect there to be a libraryItem (or whatever the Task is wrappinng with it's brackets)
    //-when this method resolves. I have no idea when that is, so for now - hold that place with a Task

    //We are also going account for the possibility of a null - becouse my HTTP call could fail for a number of reasons
    //I could be rate limitedd, asked for a bad ISBN, OpenLibrary might be down, etc

    public async Task<LibraryItem?> FetchByIsbnAsync(string isbn)
    {
        //Im going to create a string to hold the url im targeting
        //We will go much more in depth on HTTP, URLs/URIs, etc during API week
        string url = $"https://openlibrary.org/search.json?q=isbn:{isbn}&fields=title,author_name&limit=1";

        //This code could fail for a ton of reaons - i dont control OpenLibrary OR the internet between my laptop
        //and their servers

        try
        {
            //We are going try to get back a json formatted string from the API
            //Whenever we call upon an async method, we must await the call
            string jsonResponse = await client.GetStringAsync(url);

            //We are going to write our own parsing logic in a method called Parse()
            //Thankfully the returns from OpenLibrary are small. For an unmanegeable return
            //See pokeAPI

            return Parse(jsonResponse);

        }catch(HttpRequestException ex)
        {
            Log.Warning("Network fetch failed for {isbn}: {Message}", isbn, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            Log.Warning("FetchByIsbnAsync failed: {Message}", ex.Message);
            return null;
        }

        //We are going to write our own parsing logic
        //Mostrly as an excercise to work eith Json
        
    }

    public static LibraryItem? Parse(string json)
        {
            //The searchAPI within OpenLibrary returns a jsonObject, and inside that object, among other fields -
            //is a "docs" array. If we find the book we want based on it's isbn that search for, it's inside that 
            //We will use Jonathan's suggestion with a twist. Instead of Dictionary<string, string> it is <string,JsonElement>
            Dictionary<string, JsonElement>? resp = JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(json);

            //If we didnt find anything (becouse the isbn wasnt valid) book will be null OR the length of the docs array is 0
            //If book.TryGetValue return false, then we got something back, with no docs array somehow - so no book to return
            //if the docs array has a Length of 0, then we got the correct object shape back, but our isbn didnt match anything - no book to return
            if(resp is null || !resp.TryGetValue("docs", out JsonElement docs) || docs.GetArrayLength() == 0)
            {
                return null; //no docs array somehow, docs array is empty, or the json itself was empty - return a null
            }

            JsonElement foundBook = docs[0]; //if we get something back, we should only get onw thing. We search by isbn - they're unique

            //Now we can unpack thinkgs abot this foundBook
            //We are using the ?? null coallesing operator
            //If something is there: return the value resulting from the cde to the left of the ??
            //If something is not there, return whatever we assign as a "default" to the right of ??
            string title = foundBook.GetProperty("title").GetString() ?? "Untiltled"; 

            //Getting the author is a less straightforward becouse of the fact that books can have more than one author
            //So there us not a single value author property, its another array
            string author = "Unknow";

            //Checking to see if we have the author array, and if its there grab the first author
            if(foundBook.TryGetProperty("author_name", out JsonElement authors) && authors.GetArrayLength() > 0)
            {
                author = authors[0].GetString() ?? "Unknown";
            }

            return LibraryItemFactory.Create(ItemKind.Book, title, author);

        }



}