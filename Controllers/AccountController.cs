using Microsoft.AspNetCore.Mvc;
using Rota2.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Rota2.Models;

namespace Rota2.Controllers
{
    [ApiController]
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly CustomAuthStateProvider _authState;

        public AccountController(IUserService userService, CustomAuthStateProvider authState)
        {
            _userService = userService;
            _authState = authState;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = _userService.Authenticate(model.Email, model.Password);
            if (user == null) return Unauthorized();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("IsGlobalAdmin", user.IsGlobalAdmin.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Role", user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = System.DateTimeOffset.UtcNow.AddDays(7)
            });

            // Update the Blazor auth state for current circuit
            _authState.MarkUserAsAuthenticated(user);

            return Ok(new { user.Id, user.Name, user.Email, user.IsGlobalAdmin, Role = user.Role.ToString() });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _authState.MarkUserAsLoggedOut();
            return Ok();
        }
    }
}
