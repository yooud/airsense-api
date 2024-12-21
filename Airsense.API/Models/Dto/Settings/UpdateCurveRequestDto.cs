using System.ComponentModel.DataAnnotations;

namespace Airsense.API.Models.Dto.Settings;

public class UpdateCurveRequestDto
{
    public double? CriticalValue { get; set; }
    
    [MinLength(2)]
    public ICollection<CurvePointDto> Points { get; set; }
}