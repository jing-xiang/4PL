using Microsoft.AspNetCore.Mvc;
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
    }
}

