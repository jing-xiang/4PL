﻿using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace _4PL.Data
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfileLayoutController : Controller
    {
        private readonly SnowflakeDbContext _dbcontext;

        public UserProfileLayoutController(SnowflakeDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpPost("GetUserLayouts")]
        public IActionResult Get([FromBody] Tuple<string, string> tpl)
        {
            var output = _dbcontext.fetchUserLayouts(tpl.Item1, tpl.Item2);
            return Ok(output);
        }

        [HttpPost("CreateLayout")]
        public IActionResult CreateLayout([FromBody] List<UserProfileLayout> userProfileLayouts)
        {
            var response = _dbcontext.InsertUserLayout(userProfileLayouts);

            var returnOutput = "";
            if (response.ContainsKey("noUser"))
            {
                returnOutput += "Error Updating Layout. \n";
                returnOutput += $"User not found: {string.Join(", ", response["noUser"])}";
            }
            else if (response.ContainsKey("layoutExists"))
            {
                returnOutput += "Error Updating Layout. \n";
                returnOutput += $"Layout name exists for user: {string.Join(", ", response["layoutExists"])}";
            }
            else
            {
                returnOutput += "Success";
            }
            return Ok(returnOutput);
        }

        [HttpPost("UpdateLayout")]
        public IActionResult UpdateLayout([FromBody] UserProfileLayout upl)
        {
            _dbcontext.UpdateDefaultLayout(upl);
            return Ok();
        }
    }
}

