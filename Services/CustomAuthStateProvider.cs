using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Rota2.Models;

namespace Rota2.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _anon = new ClaimsPrincipal(new ClaimsIdentity());
        private ClaimsPrincipal _current = null!;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_current == null)
            {
                return Task.FromResult(new AuthenticationState(_anon));
            }
            return Task.FromResult(new AuthenticationState(_current));
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
