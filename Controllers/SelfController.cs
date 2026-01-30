using BodWebAPI.Data;
using BodWebAPI.DTOs;
using BodWebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens.Experimental;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Net.Mail;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace BodWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SelfController : ControllerBase
    {

        private readonly AppDbContext _appDbContext;
        private readonly IConfiguration _configuration;
        public SelfController(AppDbContext appDbContext, IConfiguration configuration)
        {
            _appDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));
            _configuration = configuration;
        }

        #region 秀出個人資料
        [Authorize]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [HttpGet("profile")]
        public IActionResult Private()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userBirthday = User.FindFirstValue("Birthday");
            var userPhone = User.FindFirstValue("PhoneNumber");

            return Ok(new
            {
                UserId = userId,
                UserName = User.Identity!.Name,
                UserEmail = userEmail,
                UserRole = userRole,
                UserBirthday = userBirthday,
                UserPhone = userPhone
            });
        }
        #endregion 

        #region 更新個人資料
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfie([FromBody] UpdateProfileDto dto) 
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("無法識別使用者");
            }
            var user = await _appDbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("使用者不存在");
            }

            if (!string.IsNullOrWhiteSpace(dto.UserName))
                user.UserName = dto.UserName;
            else
                return BadRequest("使用者名稱不可留空");
                
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var emailPattern = @"^[\w\.-]+@[\w\.-]+\.\w{2,}$";  // 簡單且實用的 Email 正則
                if (!Regex.IsMatch(dto.Email, emailPattern))
                {
                    return BadRequest("電子郵件格式錯誤");
                }
                user.Email = dto.Email;
            }
            else
                return BadRequest("電子郵件不可留空");

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var phonePattern = @"^09[0-9]{8}$";  // 嚴格：09 開頭 + 8 碼數字
                if (!Regex.IsMatch(dto.PhoneNumber, phonePattern))
                {
                    return BadRequest("手機號碼格式錯誤");
                }
                user.PhoneNumber = dto.PhoneNumber;
            }
            else
                return BadRequest("電話號碼不可留空");


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? "Unknown"),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, Enum.GetName(typeof(UserRole), user.Role)!),
                new Claim("Birthday", user.Birthday.ToString("yyyy-MM-dd")),
                new Claim("PhoneNumber", user.PhoneNumber ?? "")
                // 加其他你想包含的資料
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT Key 未設定")));
            var newToken = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(int.Parse(_configuration["JWT:ExpireMinutes"]!)),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(newToken);


            _appDbContext.Users.Update(user);
            await _appDbContext.SaveChangesAsync();

            return Ok(new { message = "個人資料更新成功",token = tokenString});

        }
        #endregion

        [Authorize]
        [HttpPut("Reset-Password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto) 
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("無法識別使用者");
            }
            var user = await _appDbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("使用者不存在");
            }
            if (string.IsNullOrEmpty(dto.OldPassword)) 
            {
                return BadRequest("舊密碼未輸入");
            }
            else if (string.IsNullOrEmpty(dto.NewPassword))
            {
                return BadRequest("新密碼未輸入");
            }
            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            {
                return BadRequest("舊密碼錯誤");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);



            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? "Unknown"),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, Enum.GetName(typeof(UserRole), user.Role)!),
                new Claim("Birthday", user.Birthday.ToString("yyyy-MM-dd")),
                new Claim("PhoneNumber", user.PhoneNumber ?? "")
                // 加其他你想包含的資料
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT Key 未設定")));
            var newToken = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(int.Parse(_configuration["JWT:ExpireMinutes"]!)),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(newToken);

            _appDbContext.Users.Update(user);
            await _appDbContext.SaveChangesAsync();

            return Ok(new { message = "密碼修改成功", token = tokenString });
        }
    }
}
