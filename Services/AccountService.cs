using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using UserFactory.Models;

namespace UserFactory.Services
{
    public class AccountService
    {
        private readonly UserService _userService;

        public AccountService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<(ClaimsIdentity Claims, User User)> AuthenticateAsync(LoginViewModel model)
        {
            var user = await _userService.AuthenticateUserAsync(model);
            if (user == null)
                return (null, null);

            var claims = new List<Claim>
        {
            new Claim("Guid", user.Id.ToString()),
            new Claim("Login", user.Login),
            new Claim(ClaimTypes.Role, user.Admin ? "Admin" : "User")
        };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            return (identity, user);
        }

        public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal)
        {
            var loginClaim = principal.FindFirst("Login").Value;

            return _userService.GetUserByLogin(loginClaim);
        }
    }
}
