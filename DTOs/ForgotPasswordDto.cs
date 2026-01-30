using System.ComponentModel.DataAnnotations;

namespace BodWebAPI.DTOs
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress(ErrorMessage = "電子郵件格式錯誤")]
        public string? Email { get; set; }
    }
}