using System.ComponentModel.DataAnnotations;

namespace Library.Data.Entities;

//In production - we would likely hanlde this off to something like
//ASP.NET Identity - we will make our own user class

public class User
{
    public int Id{get;set;}
    
    [MaxLength(64)]
    public string UserName {get; set;} = "";
    public string PasswordHash {get;set;} = "";//Never stored password in plain text
    public string Role {get;set;} = "consumer"; //consumer || admin
    

}