using FirebaseAdmin.Messaging;

namespace Airsense.API.Services;

public class FirebaseNotificationService() : INotificationService
{
    public async Task<bool> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
    {
        var messaging = FirebaseMessaging.DefaultInstance; 
        var result = await messaging.SendAsync(new Message
        {
            Token = deviceToken,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = data?.ToDictionary()
        });
        return !string.IsNullOrEmpty(result);
    }
    
    public async Task SendNotificationAsync(ICollection<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        var messaging = FirebaseMessaging.DefaultInstance; 
        await messaging.SendEachForMulticastAsync(new MulticastMessage
        {
            Tokens = deviceTokens as IReadOnlyList<string>,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = data?.ToDictionary()
        });
    }
    
}