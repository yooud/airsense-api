namespace Airsense.API.Models.Dto.Sensor;

public class SensorRawDto
{
    public int Id { get; set; }

    public string SerialNumber { get; set; }
    
    public string TypeName { get; set; }

    public string SensorParameter { get; set; }
    
    public string? ParamKey { get; set; }
    
    public double? ParamValue { get; set; }

    public string? ParamUnit { get; set; }

    public double? ParamMinValue { get; set; }

    public double? ParamMaxValue { get; set; }
}