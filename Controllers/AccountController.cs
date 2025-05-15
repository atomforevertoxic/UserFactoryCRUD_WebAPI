using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using UserFactory.Models;
using UserFactory.Services;

namespace UserFactory.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserService _userService;

        public AccountController( UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            var user = await _userService.AuthenticateUserAsync(model);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid login or password" });
            }

            var claims = new List<Claim>
            {
                new Claim("Guid", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, user.Admin?"Admin": "User")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = false, // No "remember me"
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) 
                });

            return Ok(new { message = "Login successful", user});
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logout successful" });
        }

        [HttpGet("current-user")]
        [Authorize]
        public async  Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst("Guid");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User ID not found");
            }

            var user = await _userService.GetByGuidAsync(userId);

            return Ok(user);
        }
    }
}