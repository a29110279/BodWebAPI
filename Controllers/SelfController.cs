using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens.Experimental;
using System.Security.Claims;

namespace BodWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SelfController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult Public()
        {
            return Ok("This is public");
        }

        [Authorize]
        [HttpGet("profile")]
        public IActionResult Private()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                UserId = userId,
                UserName = User.Identity!.Name,
                UserEmail = userEmail,
                UserRole = userRole
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("secret")]
        public IActionResult Secret()
        {
            return Ok(new { message = "你是Admin！"});
        }
    }
}
