using Microsoft.AspNetCore.Identity;

namespace _4PL.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string Email { get; set; }

        // Default values upon registration
        public string Password { get; set; } = "Password123!";
        public bool IsLocked { get; set; } = false;
        public int FailedAttempts { get; set; } = 0;
        public DateTime LastReset { get; set; } = DateTime.Now;
        public bool IsNew { get; set; } = true;

        public void lockOut()
        {
            IsLocked = true;
        }

        public void unlockOut()
        {
            IsLocked = false;
        }

        public bool getLockedStatus()
        {
            return IsLocked;
        }

        public override string ToString()
        {
            return $"name: {Name}, email: {Email}";
        }
    }
}