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
        private readonly IUserService _userService;

        public CustomAuthStateProvider(IHttpContextAccessor httpContextAccessor, IUserService userService)
        {
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
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
                var cookies = ctx?.Request?.Cookies;
                if (cookies != null && cookies.TryGetValue("RotaUserId", out var idVal))
                {
                    if (int.TryParse(idVal, out var id))
                    {
                        var user = _userService.GetById(id);
                        if (user != null && user.Active)
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
                            return Task.FromResult(new AuthenticationState(_current));
                        }
                    }
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
