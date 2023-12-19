using Microsoft.AspNetCore.Identity.UI.Services;
using System.Data;
using System.Net.Mail;

namespace Components.Account
{
    public class EmailService : IEmailService
    {
        private readonly string smtpProtocol;
        private readonly string smtpServer;
        private readonly int smtpPort;
        private readonly string senderName;

        public EmailService(IDataReader dataReader)
        {
            string[] res = new string[4];
            int i = 0;
            while (dataReader.Read())
            {
                res[i] = dataReader.GetString(0);
                i++;
            }
            smtpProtocol = res[0];
            smtpServer = res[1];
            smtpPort = Convert.ToInt32(res[2]);
            senderName = res[3];
        }

        public void SendPasswordResetLinkAsync(string email, string resetToken)
        {
            try
            {
                var resetLink = $"https://localhost:7144/Account/ResetPassword/{resetToken}";
                using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    // smtpClient.EnableSsl = smtpProtocol.ToLower() == "smtp";

                    MailMessage message = new MailMessage
                    {
                        From = new MailAddress("svc-asiarpa@hellmann.com", senderName),
                        Subject = "Password Reset",
                        Body = $"Click here to reset your password: {resetLink}",
                        IsBodyHtml = true
                    };

                    message.To.Add(email);
                    smtpClient.Send(message);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error: {ex.Message}");
            }
        }
    }
}
