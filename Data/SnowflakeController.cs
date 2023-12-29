using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Data;
using Components.Account;

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
                await _dbContext.RegisterUser(user);
                var emailSettings = await _dbContext.GetEmailSettings();
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

        [HttpGet("CheckIsValidUser={email}")]
        public async Task<ActionResult<bool>> CheckIsValidUser(string email)
        {
            try
            {
                Console.WriteLine("check is valid request received");
                bool result = _dbContext.CheckIsValidUser(email);
                Console.WriteLine("check is valid request fulfilled");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("e={userEmail}")]
        public async Task<IActionResult> VerifyUserExist(string userEmail)
        {
            try
            {
                var user = await _dbContext.VerifyUserExist(userEmail);
                if (user == null)
                {
                    return NotFound("User does not exist.");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
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
                return BadRequest(ex);
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
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{userEmail}/UpdateEmail")]
        public async Task<IActionResult> UpdateEmail([FromBody] ApplicationUser emailModel)
        {
            try
            {
                await _dbContext.UpdateEmail(emailModel);
                return Ok("Email successfully changed.");
            }
            catch (DuplicateNameException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{userEmail}/Lock")]
        public IActionResult LockUser([FromBody] ApplicationUser user)
        {
            try
            {
                _dbContext.LockUser(user);
                return Ok("User locked.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{userEmail}/Unlock")]
        public async Task<IActionResult> UnlockUserAsync([FromBody] ApplicationUser user)
        {
            try
            {
                _dbContext.UnlockUser(user);
                var emailSettings = await _dbContext.GetEmailSettings();
                EmailService emailService = new EmailService(emailSettings);
                emailService.SendPasswordResetLinkAsync(user.Email, user.Token);
                return Ok("User unlocked.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            try
            {
                ApplicationUser user = await _dbContext.VerifyUserExist(email);
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
                return Ok(result);
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

        [HttpGet("f={field}/v={value}")]
        public async Task<ActionResult<List<ApplicationUser>>> GetUsersByFieldAsync(string field, string value)
        {
            try
            {
                List<ApplicationUser> users = await _dbContext.GetUsersByFieldAsync(field, value);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest($"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("n={name}/e={email}")]
        public async Task<ActionResult<List<ApplicationUser>>> GetUsersByBothAsync(string name, string email)
        {
            try
            {
                List<ApplicationUser> users = await _dbContext.GetUsersByBothAsync(name, email);
                if (users == null)
                {
                    return NotFound(users);
                }
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("GetAllUsers")]
        public async Task<ActionResult<List<ApplicationUser>>> GetAllUsers()
        {
            try
            {
                List<ApplicationUser> users = await _dbContext.GetAllUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("GetSystemSettings")]
        public async Task<ActionResult<List<ApplicationSetting>>> GetSystemSettings()
        {
            try
            {
                List<ApplicationSetting> settings = await _dbContext.GetSystemSettings();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return BadRequest($"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPut("{settingType}/UpdateSetting")]
        public IActionResult UpdateSetting([FromBody] ApplicationSetting setting)
        {
            try
            {
                if (setting.SettingType == "MAX FAILED ATTEMPTS" || setting.SettingType == "MAX DAYS BEFORE LOCKED" || setting.SettingType == "EMAIL PORT")
                {
                    if (!int.TryParse(setting.Value, out int result))
                    {
                        return BadRequest($"{setting.SettingType} must be a number.");
                    }
                }
                _dbContext.UpdateSetting(setting);
                return Ok($"{setting.SettingType} updated.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpDelete("d={userEmail}")]
        public async Task<IActionResult> DeleteUser(string userEmail)
        {
            try
            {
                _dbContext.DeleteUser(userEmail);
                Console.WriteLine("request fulfilled");
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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