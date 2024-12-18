namespace Airsense.API.Models.Dto.Settings;

public class CurveDto
{
    public double CriticalValue { get; set; }
    
    public ICollection<CurvePointDto> Points { get; set; }
}