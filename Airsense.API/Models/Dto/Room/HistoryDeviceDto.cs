namespace Airsense.API.Models.Dto.Room;

public class HistoryDeviceDto
{
    public int Id { get; set; }
    
    public string TypeName { get; set; }
    
    public string SerialNumber { get; set; }

    public ICollection<HistoryDeviceDataDto> History { get; set; }
}