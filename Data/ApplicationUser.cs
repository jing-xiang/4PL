using Microsoft.AspNetCore.Identity;

namespace _4PL.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string Email { get; set; }

        // Default values upon registration
        public string Password { get; set; } = "123123"; // for testing/development purposes now

        // Actual Implementation
        // public string Password { get; set; } = Guid.NewGuid().ToString();
        public bool IsLocked { get; set; } = false;
        public int FailedAttempts { get; set; } = 1;
        public DateTime LastReset { get; set; } = DateTime.Now;
        public bool IsNew { get; set; } = true;
        public string Hash { get; set; } = "";
        public byte[] Salt { get; set; } = new byte[10];
        public string Token { get; set; } = "";

        public ApplicationUser() { }

        public ApplicationUser(string name, string email, int failedAttempts, bool isLoocked, DateTime lastReset)
        {
            Name = name;
            Email = email;
            FailedAttempts = failedAttempts;
            IsLocked = isLoocked;
            LastReset = lastReset;
        }

        public override string ToString()
        {
            return $"name: {Name}, email: {Email}";
        }
    }
}