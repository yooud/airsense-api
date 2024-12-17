namespace Airsense.API.Models.Dto.Room;

public class RoomDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public Dictionary<string, double> Params { get; set; }

    public double DeviceSpeed { get; set; }
}