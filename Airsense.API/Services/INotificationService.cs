namespace Airsense.API.Services;

public interface INotificationService
{
    public Task<bool> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
    
    public Task SendNotificationAsync(ICollection<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null);
}