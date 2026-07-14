namespace Library.ControllerApi.Services;

public interface ITokenService
{
    string Issue(string user, string role);
    
}