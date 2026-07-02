namespace Library.Data.Enums;

public enum Priority
{
    //Be default enums are backed by ordinals (0,1,2...)
    //We can give them values explicitily if we are going to do math or sort based enums
    Normal = 0,
    Expedited = 1
}