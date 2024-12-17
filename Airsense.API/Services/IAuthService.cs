namespace Airsense.API.Services;

public interface IAuthService
{
    public Task<bool> SetIdAsync(string uid, int id);
}