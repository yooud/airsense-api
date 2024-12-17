namespace Airsense.API.Models.Dto.Environment;

public class UpdateMemberRequestDto
{
    public MemberRole Role { get; set; }
    
    public enum MemberRole
    {
        Admin,
        Member
    }
}