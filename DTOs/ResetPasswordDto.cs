using System.ComponentModel.DataAnnotations;

namespace BodWebAPI.DTOs
{
    public class ResetPasswordDto
    {
        public string? OldPassword { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "新密碼長度不可小於8位數")]
        public string? NewPassword { get; set; }
    }
}
