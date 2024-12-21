namespace Airsense.API.Models.Dto.Sensor;

public class ParameterDto
{
    public string Name { get; set; }

    public double? Value { get; set; }

    public string Unit { get; set; }

    public double MinValue { get; set; }

    public double MaxValue { get; set; }
}