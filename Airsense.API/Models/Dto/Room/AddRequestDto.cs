using System.ComponentModel.DataAnnotations;

namespace Airsense.API.Models.Dto.Room;

public class AddRequestDto
{
    [Length(20,20)]
    public string SerialNumber { get; set; }
}