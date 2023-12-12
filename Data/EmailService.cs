using Amazon.Runtime;
using Components.Account;
using System.Data;
using System.Net.Mail;

namespace _4PL.Data
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
                var resetLink = $"https://localhost:7144/Account/ResetPassword?token={resetToken}";
                Console.WriteLine("reset link generated");
                using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    Console.WriteLine("smtpclient created");
                    // smtpClient.EnableSsl = smtpProtocol.ToLower() == "smtp";

                    MailMessage message = new MailMessage
                    {
                        From = new MailAddress("svc-asiarpa@hellmann.com", "APAC_RPA"),
                        Subject = "Password Reset",
                        Body = $"Click here to reset your password: {resetLink}",
                        IsBodyHtml = true
                    };
                    Console.WriteLine("mail message created");

                    message.To.Add(email);
                    Console.WriteLine("email added");

                    smtpClient.Send(message);
                    Console.WriteLine("link sent");
                }
            } catch (Exception ex)
            {
                throw new Exception($"Error: {ex.Message}");
            }
        }
    }
}
