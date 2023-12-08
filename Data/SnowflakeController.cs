using _4PL.Components.Account.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                _dbContext.RegisterUser(user);
                return Ok("User registered successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Registration failed: {ex.Message}");
            }
        }

        [HttpGet("CheckIsNewUser")]
        public IActionResult CheckIsNewUser([FromBody] ApplicationUser user)
        {
            try
            {
                string isNew = _dbContext.GetFieldByEmail(user, "is_new_user");
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

        [HttpGet("GetFailedAttempts")]
        public IActionResult GetFailedAttempts([FromBody] ApplicationUser user)
        {
            try
            {
                string attempts = _dbContext.GetFieldByEmail(user, "failed_attempts");
                if (attempts != null)
                {
                    return Ok(new { failed_attempts = attempts });
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

        [HttpGet("CheckIsLocked")]
        public IActionResult CheckIsLocked([FromBody] ApplicationUser user)
        {
            try
            {
                string isLocked = _dbContext.GetFieldByEmail(user, "is_locked");
                if (isLocked == "TRUE")
                {
                    return Ok(new { Message = "Account is locked." });
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

        [HttpGet("ValidateLogin")]
        public IActionResult ValidateLogin([FromBody] ApplicationUser user)
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
    }
}