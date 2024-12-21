namespace Airsense.API.Models.Dto.Room;

public class HistoryRawDto
{
    public int Id { get; set; }
    
    public string TypeName { get; set; }
    
    public string SerialNumber { get; set; }

    public double? Value { get; set; }

    public long? Timestamp { get; set; }
}