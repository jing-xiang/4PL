using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Data;
using Components.Account;
using Microsoft.AspNetCore.Identity;

namespace _4PL.Data
{
    [ApiController]
    [Route("api/[controller]")]
    public class SnowflakeController : ControllerBase
    {
        private readonly SnowflakeDbContext _dbContext;
        public SnowflakeController(SnowflakeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("RegisterUser")]
        public async Task<IActionResult> RegisterUser([FromBody] ApplicationUser user)
        {
            byte[] salt = GenerateSalt();
            string hashedPassword = HashPassword(user.Password, salt);

            try
            {
                _dbContext.RegisterUser(user, hashedPassword, Convert.ToBase64String(salt));
                var ResetToken = Guid.NewGuid().ToString();
                var emailSettings = _dbContext.GetEmailSettings().Result;
                IEmailService emailService = new EmailService(emailSettings);
                emailService.SendPasswordResetLinkAsync(user.Email, ResetToken);

                return Ok("User registered successfully. Check email for confirmation.");
            }
            catch (DuplicateNameException ex)
            {
                return Conflict($"{ex.Message}");
            }
            catch (Exception ex)
            {
                return Conflict($"{ex.Message}");
            }
        }

        [HttpPost("GetUser")]
        public async Task<IActionResult> GetUser([FromBody] string email)
        {
            try
            {
                ApplicationUser user = _dbContext.GetUser(email).Result;
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpPost("ValidateLogin")]
        public async Task<IActionResult> ValidateLogin([FromBody] ApplicationUser user)
        {
            try
            {
                string storedHash = _dbContext.GetStringField(user, "password").Result;
                byte[] storedSalt = Convert.FromBase64String(_dbContext.GetStringField(user, "salt").Result);
                string inputHash = HashPassword(user.Password, storedSalt);
                Console.WriteLine(storedHash);
                Console.WriteLine(storedSalt);

                if (storedHash == inputHash)
                {
                    return Ok("Login successful.");
                }
                else
                {
                    return Unauthorized("Login unsuccessful.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ApplicationUser user)
        {
            try
            {
                _dbContext.ResetPassword(user);
                return Ok("Password successfully updated.");
            }
            catch (Exception ex)
            {
                return BadRequest($"{ex.Message}");
            }
        }

        [HttpPost("UpdateAttempts")]
        public async Task<IActionResult> UpdateAttempts([FromBody] ApplicationUser user)
        {
            try
            {
                Console.WriteLine("updating user attempts");
                string result = await _dbContext.UpdateAttempts(user);
                return Ok($"Invalid credentials. Number of attempts remaining: {result}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private string HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash);
            }
        }
    }
}