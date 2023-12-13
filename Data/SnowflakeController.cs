using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Data;
using Components.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

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
            string token = Guid.NewGuid().ToString();
            user.Hash = hashedPassword;
            user.Salt = salt;
            user.Token = token;

            try
            {
                _dbContext.RegisterUser(user);
                var emailSettings = _dbContext.GetEmailSettings().Result;
                EmailService emailService = new EmailService(emailSettings);
                emailService.SendPasswordResetLinkAsync(user.Email, token);

                return Ok("User registered successfully. Check email for confirmation.");
            }
            catch (DuplicateNameException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("e={userEmail}")]
        public async Task<IActionResult> GetUserByEmailAsync(string userEmail)
        {
            try
            {
                var user = await _dbContext.GetUserByEmailAsync(userEmail);
                if (user == null)
                {
                    return NotFound("User does not exist.");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                return Conflict(ex);
            }
        }

        [HttpGet("t={token}")]
        public async Task<IActionResult> GetUserByTokenAsync(string token)
        {
            try
            {
                var user = await _dbContext.GetUserByTokenAsync(token);
                if (user == null)
                {
                    return NotFound("User does not exist.");
                }
                return Ok(user);
            } 
            catch (Exception ex)
            {
                return Conflict(ex);
            }
        }

        [HttpPost("ValidatePassword")]
        public async Task<IActionResult> ValidatePassword([FromBody] ApplicationUser user)
        {
            try
            {
                string storedHash = _dbContext.GetStringFieldByEmail(user.Email, "password").Result;
                byte[] storedSalt = Convert.FromBase64String(_dbContext.GetStringFieldByEmail(user.Email, "salt").Result);
                string inputHash = HashPassword(user.Password, storedSalt);

                if (storedHash == inputHash)
                {
                    return Ok("Password matches.");
                }
                else
                {
                    return Unauthorized("Password is incorrect.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPut("{userEmail}/ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ApplicationUser user)
        {
            byte[] salt = GenerateSalt();
            string hashedPassword = HashPassword(user.Password, salt);
            string newToken = Guid.NewGuid().ToString();
            user.Hash = hashedPassword;
            user.Salt = salt;
            user.Token = newToken;

            try
            {
                await _dbContext.ResetPassword(user);
                await _dbContext.UpdateToken(user);
                return Ok("Password successfully updated.");
            }
            catch (Exception ex)
            {
                return BadRequest($"{ex.Message}");
            }
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            try
            {
                ApplicationUser user = await _dbContext.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return NotFound("User does not exist.");
                }

                var emailSettings = await _dbContext.GetEmailSettings();
                EmailService emailService = new EmailService(emailSettings);
                emailService.SendPasswordResetLinkAsync(email, user.Token);
                return Ok("Password reset link sent.");
            }
            catch (AggregateException ex)
            {
                return NotFound(ex.InnerException.Message);
            }
        }

        [HttpPost("UpdateAttempts")]
        public async Task<IActionResult> UpdateAttempts([FromBody] ApplicationUser user)
        {
            try
            {
                string result = await _dbContext.UpdateAttempts(user);
                return Ok($"Invalid credentials. Number of attempts remaining: {result}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPost("ResetAttempts")]
        public async Task<IActionResult> ResetAttempts([FromBody] ApplicationUser user)
        {
            try
            {
                _dbContext.ResetAttempts(user);
                return Ok("Attempts have been reset.");
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



        /*
         * Ratecard
         */
        //[HttpPost("CreateRcTransaction")]
        //[HttpGet("CreateRcTransaction")]
        //public ActionResult createRcTransaction()
        //{
        //    //Fore ach rate card, ...
        //    //1.Create transaction
        //    string transactionId = _dbContext.CreateRcTransaction(null);

        //    //2. Create charges (reference transactionId)
        //    List<string> chargeIds = _dbContext.CreateCharges(new List<Charge>() { new Charge(), new Charge(), new Charge() }, transactionId);

        //    //3. Create ratecard (reference transactionId and chargeIds)
        //    //string ratecardId = _dbContext.CreateRatecard(new RateCard(), chargeIds, transactionId);

        //    //return Ok(ratecardId);
        //    return Ok(chargeIds);
        //}

        //[HttpGet("GetTransaction/{transactionId}")]
        //public ActionResult getTransaction(string transactionId)
        //{
        //    return Ok();
        //}
    }
}