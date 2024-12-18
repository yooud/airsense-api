namespace Airsense.API.Models.Dto.Settings;

public class UpdateCurveRequestDto
{
    public double CriticalValue { get; set; }
    
    public ICollection<CurvePointDto> Points { get; set; }
}