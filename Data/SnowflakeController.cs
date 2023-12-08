using _4PL.Components.Account.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Snowflake.Data.Client;
using System.Data;

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

        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser([FromBody] string email)
        {
            try
            {
                var user = await _dbContext.GetUser(email);
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
                _dbContext.ValidateLogin(user);
                if (isNew == "TRUE")
                {
                    return Ok(new { Message = "is new user." });
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("Validate")]
        public IActionResult Validate([FromBody] ApplicationUser user)
        {
            try
            {
                string password = _dbContext.GetFieldByEmail(user, "password");
                if (password != null && password.Equals(user.Password))
                {
                    return Ok(new { Message = "Login successful." });
                }
                else
                {
                    return Unauthorized(new { Message = "Invalid credentials." });
                }
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