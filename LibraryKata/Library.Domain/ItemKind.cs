// This is going to be an Enum 
// Its a custom value type, where we basically enumerate possible values ahead of time
namespace LibraryKata.Domain;

public enum ItemKind
{
    // My enum definition contains possible values for an instance of this enum.
    // An ItemKind enum can ONLY ever be one of these 3 things. I can come back and add more later.
    Book,
    ReferenceBook,
    Magazine
}