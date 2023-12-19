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

        public class AccessRightsModel
        {
            public string Heading { get; set; }
            public bool IsAccessible { get; set; }

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

        [HttpPost("FetchAccessRightsHeadings")]
        public async Task<ActionResult<string[]>> FetchAccessRightsHeadings([FromBody] string email)
        {
            try
            {
                //Access right array
                Console.WriteLine("request received");
                //TODO: get user from database and return access rights associated with user
                string[] result = _dbContext.FetchAccessRightsHeadings(email).Result;
                Console.WriteLine("request processed");
                Console.WriteLine(result);
                return result;
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpPost("CopyAccessRights")]
        public async Task<ActionResult<string[]>> CopyAccessRights([FromBody] List<string> emailList)
        {
            try
            {
                //Access right array
                Console.WriteLine("request received");
                string[] access_type = await _dbContext.FetchAccessRightsHeadings(emailList[0]);
                bool[] is_accessible = await _dbContext.FetchAccessRights(emailList[0]);
                //TODO: get user from database and return access rights associated with user
                var result = _dbContext.CopyAccessRights(emailList[1], access_type, is_accessible);
                Console.WriteLine("request processed");
                Console.WriteLine(emailList);
                Console.WriteLine(result);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }


        [HttpGet("FetchAvailableAccounts")]
        public async Task<ActionResult<string[]>> FetchAvailableAccounts()
        {
            try
            {
                //Access right array
                Console.WriteLine("request received");
                //TODO: get user from database and return access rights associated with user
                string[] result = _dbContext.FetchAvailableAccounts().Result;
                Console.WriteLine("request processed");
                Console.WriteLine(result);
                return result;
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpPost("DeleteAccessRights")]
        public async Task<ActionResult<string[]>> DeleteAccessRights([FromBody] List<string>parameterList)
        {
            try
            {
                Console.WriteLine("request received");
                var result = _dbContext.DeleteAccessRights(parameterList[0], parameterList[1]);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpPost("SaveAccessRights")]
        public async Task<ActionResult<string[]>> SaveAccessRights([FromBody] List<string> parameterList)
        {
            try
            {
                string[] access_type = await _dbContext.FetchAccessRightsHeadings(parameterList[0]);
                Console.WriteLine("request received");
                var result = _dbContext.SaveAccessRights(parameterList, access_type);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpPost("AddAccessRights")]
        public async Task<ActionResult<string[]>> AddAccessRights([FromBody] List<string> parameterList)
        {
            try
            {
                Console.WriteLine("request received");
                var result = _dbContext.AddAccessRights(parameterList[0], parameterList[1]);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }
    }
}