using Microsoft.AspNetCore.Authentication;

namespace _4PL.Data.Authenthication
{
    public class AuthenticationService : IAuthenticationService
    {
        public bool IsAuthenticated { get; private set; }
        public void SetAuthenticated (bool isAuthenticated)
        {
            IsAuthenticated = isAuthenticated;
        }
    }
}
