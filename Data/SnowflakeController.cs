using _4PL.Components.Account.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Snowflake.Data.Client;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

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
        public IActionResult RegisterUser([FromBody] ApplicationUser user)
        {
            byte[] salt = GenerateSalt();
            string hashedPassword = HashPassword(user.Password, salt);

            try
            {
                _dbContext.RegisterUser(user, hashedPassword, Convert.ToBase64String(salt));
                return Ok("User registered successfully");
            }
            catch (DuplicateNameException ex)
            {
                return Conflict($"{ex.Message}");
            }
        }

        [HttpPost("GetUser")]
        public async Task<IActionResult> GetUser([FromBody] string email)
        {
            try
            {
                Console.WriteLine("request received");
                ApplicationUser user = _dbContext.GetUser(email).Result;
                Console.WriteLine("request processed");
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpGet("ValidateLogin")]
        public IActionResult ValidateLogin([FromBody] ApplicationUser user)
        {
            try
            {
                string storedHash = _dbContext.RetrieveHash(user).Result;
                byte[] storedSalt = Convert.FromBase64String(_dbContext.RetrieveSalt(user).Result);
                string inputHash = HashPassword(user.Password, storedSalt);

                if (storedHash == inputHash)
                {
                    return Ok("Login successful.");
                }
                else
                {
                    return Unauthorized("Invalid credentials.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPost("UpdateAttempts")]
        public IActionResult Validate([FromBody] ApplicationUser user)
        {
            try
            {
                _dbContext.UpdateAttempts(user);
                return Ok("Failed attempts updated.");
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