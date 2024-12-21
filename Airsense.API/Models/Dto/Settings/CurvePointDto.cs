using System.ComponentModel.DataAnnotations;

namespace Airsense.API.Models.Dto.Settings;

public class CurvePointDto
{
    public double Value { get; set; }
    
    [Range(0, 100)]
    public int FanSpeed { get; set; }
}