using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace _4PL.Data.Authenthication
{
    public class SnowflakeAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _user;

        public void MarkUserAsAuthenticated(ApplicationUser newUser)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, newUser.Email),
                new Claim(ClaimTypes.Name, newUser.Name),
            }, "custom");

            _user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
        }

        public void MarkUserAsLoggedOut()
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var authState = _user != null
                ? new AuthenticationState(_user)
                : new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            return Task.FromResult(authState);
        }
    }
}
