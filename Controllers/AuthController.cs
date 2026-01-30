using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BodWebAPI.Data;
using BodWebAPI.DTOs;
using BodWebAPI.Models;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Util.Store;

namespace BodWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public AuthController(AppDbContext appDbContext,IConfiguration configuration, IMemoryCache cache)
        {
            _appDbContext = appDbContext;
            _configuration = configuration;
            _cache = cache;
        }

        #region register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto dto) 
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
        #endregion

        #region Login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
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
                new Claim(ClaimTypes.Role, Enum.GetName(typeof(UserRole), user.Role)!),
                new Claim("Birthday", user.Birthday.ToString("yyyy-MM-dd")), 
                new Claim("PhoneNumber", user.PhoneNumber ?? "")
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
        #endregion

        #region forgotpassword
        [HttpPost("forgot_password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email))
            {
                return BadRequest("請輸入電子郵件");
            }

            var user = _appDbContext.Users.FirstOrDefault(u => u.Email == dto.Email);
            if (user == null)
                return BadRequest("該電子郵件未註冊");

            // 產生 6 位驗證碼
            var code = new Random().Next(100000, 999999).ToString();

            // 暫存 5 分鐘
            var cacheKey = $"reset_code_{user.Id}";
            _cache.Set(cacheKey, code, TimeSpan.FromMinutes(5));

            // 寄信（使用 MailKit + OAuth2）
            var subject = "BOD 網站 - 密碼重設驗證碼";
            var body = $"您的驗證碼是：{code}\n\n有效時間：5 分鐘\n若非本人操作，請忽略此信。";
            await SendEmailWithOAuth2Async(dto.Email, subject, body);

            return Ok(new { message = "驗證碼已寄至您的電子郵件" });
        }
        #endregion

        private async Task SendEmailWithOAuth2Async(string toEmail, string subject, string body)
        {
            var clientId = _configuration["Gmail:ClientId"];
            var clientSecret = _configuration["Gmail:ClientSecret"];
            var userEmail = "c29110279@gmail.com";
            
            var secrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            // OAuth2 授權流程
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                new[] { "https://mail.google.com/" },  // 完整權限
                userEmail,
                CancellationToken.None,
                new FileDataStore("token.json", true)  // 儲存 token 到檔案
            );

            var oauth2 = new SaslMechanismOAuth2(userEmail, credential.Token.AccessToken);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("BOD 網站", userEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await credential.RefreshTokenAsync(CancellationToken.None);
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(oauth2);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
