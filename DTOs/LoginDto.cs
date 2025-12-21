using System.ComponentModel.DataAnnotations;

namespace BodWebAPI.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "請輸入帳號或Email或電話")]
        public string Account { get; set; } = string.Empty;
        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; } = string.Empty;
    }
}
