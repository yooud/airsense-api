namespace Airsense.API.Models.Dto.Sensor;

public class SensorDto
{
    public int Id { get; set; }

    public string SerialNumber { get; set; }

    public ICollection<string> Types { get; set; }
    
    public Dictionary<string, double> Params { get; set; }
}