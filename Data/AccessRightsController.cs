using _4PL.Components.Account.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Snowflake.Data.Client;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace _4PL.Data
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccessRightsController : ControllerBase
    {
        private readonly SnowflakeDbContext _dbContext;
        public AccessRightsController(SnowflakeDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        [HttpPost("FetchAccessRights")]
        public async Task<ActionResult<bool[]>> FetchAccessRights([FromBody] string email)
        {
            try
            {
                //Access right array
                Console.WriteLine("request received");
                //TODO: get user from database and return access rights associated with user
                bool[] result = _dbContext.FetchAccessRights(email).Result;
                Console.WriteLine("request processed");
                Console.WriteLine(result);
                return result;
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

       
    }
}