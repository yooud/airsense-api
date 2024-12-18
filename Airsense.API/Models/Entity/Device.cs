namespace Airsense.API.Models.Entity;

public class Device
{
    public int Id { get; set; }

    public string SerialNumber { get; set; }

    public int? RoomId { get; set; }

    public DateTime ActiveAt { get; set; }
}