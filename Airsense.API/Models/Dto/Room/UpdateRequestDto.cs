using System.ComponentModel.DataAnnotations;

namespace Airsense.API.Models.Dto.Room;

public class UpdateRequestDto
{
    [Length(3, 20)]
    public string Name { get; set; }
}