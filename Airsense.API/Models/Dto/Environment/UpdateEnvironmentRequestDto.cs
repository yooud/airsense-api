using System.ComponentModel.DataAnnotations;

namespace Airsense.API.Models.Dto.Environment;

public class UpdateEnvironmentRequestDto
{
    [Length(3,20)]
    public string Name { get; set; }
}