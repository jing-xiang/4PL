using Microsoft.AspNetCore.Mvc;

namespace _4PL.Data
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataAnalysisController : ControllerBase
    {
        private readonly DataAnalysisDbContext _dbContext;
        public DataAnalysisController(DataAnalysisDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("FetchAccessRights={userEmail}")]
        public async Task<ActionResult<List<string>>> FetchAccessRights(string userEmail)
        {
            try
            {
                List<string> result = await _dbContext.FetchAccessRights(userEmail);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpGet("CheckIsValidType={accessType}")]
        public async Task<ActionResult<bool>> CheckIsValidType(string accessType)
        {
            try
            {
                bool result = _dbContext.CheckIsValidType(accessType);
                return Ok(!result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpGet("FetchAccessTypes")]
        public async Task<ActionResult<List<AccessRight>>> FetchAccessTypes()
        {
            try
            {
                List<AccessRight> result = await _dbContext.FetchAccessTypes();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpPost("CopyAccessRights")]
        public async Task<IActionResult> CopyAccessRights([FromBody] List<string> copyPair)
        {
            try
            {
                List<string> rightsToCopy = await _dbContext.FetchAccessRights(copyPair[0]);
                _dbContext.CopyAccessRights(copyPair[1], rightsToCopy);
                return Ok($"Successfully copied rights from {copyPair[0]} to {copyPair[1]}");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpPost("AddNewAccessRight")]
        public async Task<IActionResult> AddNewAccessRight([FromBody] AccessRight newRight)
        {
            try
            {
                _dbContext.AddNewAccessRight(newRight);
                return Ok($"Successfully added new access right {newRight.AccessType}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPut("UpdateAccessRight")]
        public async Task<IActionResult> UpdateAccessRight([FromBody] AccessRight updatedRight)
        {
            try
            {
                _dbContext.UpdateAccessRight(updatedRight);
                return Ok($"Successfully updated access right {updatedRight.AccessType}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpDelete("DeleteAccessRight={accessType}")]
        public async Task<IActionResult> DeleteAccessRight(string accessType)
        {
            try
            {
                _dbContext.DeleteAccessRight(accessType);
                return Ok($"Successfully deleted access right {accessType}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPost("SaveAccessRights")]
        public async Task<IActionResult> SaveAccessRights([FromBody] ApplicationUser user)
        {
            try
            {
                _dbContext.SaveAccessRights(user);
                return Ok($"Access rights for {user.Name} updated.");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound($"{ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
    }
}