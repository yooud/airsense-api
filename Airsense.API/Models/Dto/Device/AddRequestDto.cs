using System.ComponentModel.DataAnnotations;

namespace Airsense.API.Models.Dto.Device;

public class AddRequestDto
{
    [StringLength(20)]
    public string SerialNumber { get; set; }
}