namespace Airsense.API.Models.Entity;

public class Sensor
{
    public int Id { get; set; }

    public string SerialNumber { get; set; }
    
    public int? RoomId { get; set; }

    public int TypeId { get; set; }

    public string Secret { get; set; }
}