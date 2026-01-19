namespace BodWebAPI.DTOs
{
    public class ResetPasswordDto
    {
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}
