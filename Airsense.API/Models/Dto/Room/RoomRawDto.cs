namespace Airsense.API.Models.Dto.Room;

public class RoomRawDto
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public double? DeviceSpeed { get; set; }
    
    public string? ParamKey { get; set; }
    
    public double? ParamValue { get; set; }
    
    public string? ParamUnit { get; set; }

    public double? ParamMinValue { get; set; }

    public double? ParamMaxValue { get; set; }
}
