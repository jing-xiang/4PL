namespace _4PL.Data.Authenthication
{
    public interface IAuthenticationService
    {
        bool IsAuthenticated { get; }
        void SetAuthenticated (bool isAuthenticated);
    }
}
