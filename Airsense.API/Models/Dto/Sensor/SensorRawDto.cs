namespace Airsense.API.Models.Dto.Sensor;

public class SensorRawDto
{
    public int Id { get; set; }

    public string SerialNumber { get; set; }

    public string Types { get; set; }
    
    public string ParamKey { get; set; }
    
    public double ParamValue { get; set; }
}