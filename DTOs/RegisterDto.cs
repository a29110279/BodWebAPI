using System.ComponentModel.DataAnnotations;

namespace BodWebAPI.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "請輸入名稱")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼")]
        [StringLength(100,MinimumLength = 8,ErrorMessage = "密碼長度不可小於8位數")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入電子郵件信箱")]
        [EmailAddress(ErrorMessage = "電子郵件信箱格式錯誤")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入生日")]
        [DataType(DataType.Date)]
        public DateTime Birthday { get; set; }

        [Required(ErrorMessage = "請輸入手機號碼")]
        [Phone(ErrorMessage = "手機格式錯誤")]
        public string PhoneNumber { get; set; } = string.Empty;

    }
}
