using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("TestSnowflake")]
        public IActionResult RegisterUser([FromBody] ApplicationUser user)
        {
            try
            {
                Console.WriteLine("http request received");
                _dbContext.ExecuteStoredProc("ADD_NEW_USER", user);
                Console.WriteLine("store proc successfully executed.");
                return Ok("User registered successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Registration failed: {ex.Message}");
            }
        }
    }
}