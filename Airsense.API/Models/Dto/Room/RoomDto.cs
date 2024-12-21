using Airsense.API.Models.Dto.Sensor;

namespace Airsense.API.Models.Dto.Room;

public class RoomDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public ICollection<ParameterDto>? Parameters { get; set; }

    public double? DeviceSpeed { get; set; }
}