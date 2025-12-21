using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens.Experimental;
using System.Security.Claims;

namespace BodWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult Public()
        {
            return Ok("This is public");
        }

        [Authorize]
        [HttpGet("private")]
        public IActionResult Private()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            return Ok(new
            {
                UserId = userId,
                UserName = User.Identity!.Name,
                UserEmail = userEmail
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
