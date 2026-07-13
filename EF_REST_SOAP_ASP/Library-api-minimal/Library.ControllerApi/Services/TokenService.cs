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

    public TokenService(IConfiguration config)
    {
        //we probably want to avoid hardcoding the basis of our key
        //we can always add it to appsetting. Development.json and treat ir as a secret
        //we probably want to then add that file to the .gitignore. Some logic aas a .env file
        _key = config["Jwt:Key"];
    }
}