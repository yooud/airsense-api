namespace Airsense.API.Models.Entity;

public class User
{
    public int Id { get; set; }
    
    public string Uid { get; set; }

    public string Name { get; set; }
    
    public string Email { get; set; }
    
    public string NotificationToken { get; set; }
    
    public DateTime CreatedAt { get; set; }
}