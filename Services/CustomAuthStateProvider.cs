using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Rota2.Models;

namespace Rota2.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _anon = new ClaimsPrincipal(new ClaimsIdentity());
        private ClaimsPrincipal _current = null!;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomAuthStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // if already set in memory return it
            if (_current != null)
            {
                return Task.FromResult(new AuthenticationState(_current));
            }

            try
            {
                var ctx = _httpContextAccessor.HttpContext;
                if (ctx?.User?.Identity?.IsAuthenticated == true)
                {
                    // Use the authenticated principal supplied by the ASP.NET cookie middleware
                    _current = ctx.User;
                    return Task.FromResult(new AuthenticationState(_current));
                }
            }
            catch
            {
                // ignore and fall back to anonymous
            }

            return Task.FromResult(new AuthenticationState(_anon));
        }

        public void MarkUserAsAuthenticated(User user)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("IsGlobalAdmin", user.IsGlobalAdmin.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Role", user.Role.ToString())
            }, "apiauth");
            _current = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_current)));
        }

        public void MarkUserAsLoggedOut()
        {
            _current = _anon;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anon)));
        }
    }
}
