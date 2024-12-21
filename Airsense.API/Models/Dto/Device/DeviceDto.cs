namespace Airsense.API.Models.Dto.Device;

public class DeviceDto
{
    public int Id { get; set; }

    public string SerialNumber { get; set; }

    public double? FanSpeed { get; set; }

    public long? ActiveAt { get; set; }
}