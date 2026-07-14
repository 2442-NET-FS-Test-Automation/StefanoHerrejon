using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Library.ControllerApi.Services;

//Logic for token issuance lives here - any service controller or another service
//That needs to JST calls this code

public class TokenService : ITokenService
{
    private readonly string _key;

    //This is a temporary stand-in that will BE REMOVED - its going to stand in for seeding admin accounts.
    //We will add a user table with some admin accouts tomorrow - for true auth. This is just for AuthZ demo

    private static readonly Dictionary<string, string> Roles = 
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ada"] = "admin" //This string username gets admin
            //everyone else falls throught as "consumer" or whatever other role we default to
        };

    public TokenService(IConfiguration config)
    {
        //we probably want to avoid hardcoding the basis of our key
        //we can always add it to appsetting. Development.json and treat ir as a secret
        //we probably want to then add that file to the .gitignore. Some logic aas a .env file
        _key = config["Jwt:Key"];
    }

    //Method for token issuance. Validation lives in Program.cs
    //This token, once the front end has it (i.e. User has logged in), gets appended to every
    //http request. For some endpoints, we will validate this token, and if the user isnt authorized to di
    //a given action we sent back 401 unauthorized
    public string Issue(string user, string role)
    {
        //Sign the token with asymetric key (HMAC-SHA256) - the
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)), SecurityAlgorithms.HmacSha256);
        
        //If user = "ada" they get admin, otherwise they get "consumer"
        //temp code
        //var role = Roles.GetValueOrDefault(user, "consumer");
        
        //Once we have creds (That jey we can sign with) we can register claims
        //things like user role. We can also give the key anexpiration date/time

        var token = new JwtSecurityToken("library-fulfillment", "library-fulfillment-clients", 
            new[] {new Claim(ClaimTypes.Name, user), new Claim(ClaimTypes.Role, role)},
            expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}