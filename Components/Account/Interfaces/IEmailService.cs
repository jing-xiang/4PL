namespace Components.Account
{
    public interface IEmailService
    {
        void SendPasswordResetLinkAsync(string email, string resetToken);
    }
}
