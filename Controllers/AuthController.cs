using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BodWebAPI.Data;
using BodWebAPI.DTOs;
using BodWebAPI.Models;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace BodWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext appDbContext,IConfiguration configuration)
        {
            _appDbContext = appDbContext;
            _configuration = configuration;
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
                CreateDate = DateTime.Now,
                Role = UserRole.User
            };

            _appDbContext.Users.Add(user);
            _appDbContext.SaveChanges();
            return Ok(new 
            {
                message = "會員資料新增完成"
            });

        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            var account = dto.Account.Trim();

            var user = _appDbContext.Users
                .FirstOrDefault(u => u.Email == account || u.PhoneNumber == account || u.UserName == account);

            if (string.IsNullOrWhiteSpace(account))
            {
                return BadRequest("請輸入帳號或Email或電話");
            }
            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("請輸入密碼");
            }

            if (user == null)
            {
                return BadRequest("查無帳號或密碼錯誤");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return BadRequest("查無帳號或密碼錯誤");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? "Unkonwn"),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, Enum.GetName(typeof(UserRole), user.Role)!)
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT Key 未設定")));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    int.Parse(_configuration["JWT:ExpireMinutes"]!)
                    ),
                signingCredentials: new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                    )
                );

            return Ok(new {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });

        }
    }
}
