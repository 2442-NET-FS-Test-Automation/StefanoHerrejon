using Library.Data;
using Library.Data.Entities;
using Microsoft.AspNetCore.Identity; //Not the fuññ framework - we just need the PasswordHasher
using Microsoft.EntityFrameworkCore;

namespace Library.ControllerApi.Services;

public class UserService : IUserService
{
    private readonly LibraryDbContext _db;

    //Comes from ASP.NET Identity. User per-password salt to abfuscate/hash passwords
    //we will hash THEN store. And always verify agains that hash. Never store plaintext passwords.
    //generally: don't invent your own hashing
    private readonly IPasswordHasher<User> _hasher;

    public UserService(LibraryDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }
    public async Task<string?> RegisterAsyn(string username, string password)
    {
        //First trim the string
        string name = username.Trim();

        //Check if the username is already taken
        if(await _db.Users.AnyAsync(u => u.UserName == name))
            return "username is taken";

        User newUser = new User {UserName = name, Role = "consumer"}; //Never trust client on the role

        //Hashing + salting password - uses the newUser object + password
        newUser.PasswordHash = _hasher.HashPassword(newUser, password);

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();
        return null; //If all goes well - we return null
    }

    public async Task<User?> ValidateAsync(string username, string password)
    {
        User? foundUser = await _db.Users.SingleOrDefaultAsync(u => u.UserName == username);

        if (foundUser is null) return null; //Unknown username and wrong pass look IDENTICAL
        //probably not the best imoplementation - you guys can do more checks later

        var result = _hasher.VerifyHashedPassword(foundUser, foundUser.PasswordHash, password);

        return result == PasswordVerificationResult.Failed? null : foundUser;
    }
}