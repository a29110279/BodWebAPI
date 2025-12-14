using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BodWebAPI.Data;
using BodWebAPI.DTOs;
using BodWebAPI.Models;
using BCrypt.Net;

namespace BodWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;

        public AuthController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto) 
        {
            
            if (_appDbContext.Users.Any(u => u.PhoneNumber == dto.PhoneNumber))
            {
                return BadRequest("此電話號碼已被使用");
            }
            if (_appDbContext.Users.Any(u => u.Email == dto.Email))
            {
                return BadRequest("此電子郵件信箱已被使用");
            }
            if (_appDbContext.Users.Any(u => u.UserName == dto.UserName))
            {
                return BadRequest("此使用者名稱已被使用");
            }

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Birthday = dto.Birthday,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreateDate = DateTime.Now
            };

            _appDbContext.Users.Add(user);
            _appDbContext.SaveChanges();
            return Ok(new 
            {
                message = "會員資料新增完成"
            });

        }
    }
}
