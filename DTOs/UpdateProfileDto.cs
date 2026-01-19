using System.ComponentModel.DataAnnotations;

namespace BodWebAPI.DTOs
{
    public class UpdateProfileDto
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

    }
}
