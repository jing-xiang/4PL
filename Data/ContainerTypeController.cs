using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace _4PL.Data;

[Route("api/[controller]")]
[ApiController]
public class ContainerTypeController : Controller
{
    private readonly SnowflakeDbContext _dbContext;

    public ContainerTypeController(SnowflakeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /**
     * APIs 
     * Create a container type (after submitting in form) 
     * Delete a container type 
     * Get all container types (Search) 
    **/

    [HttpPost("CreateContainerType/{containerType}")]
    public async Task<ActionResult<string>> CreateContainerType([FromBody] string containerType) // TBC: FromBody
    {
        try
        {
            string result = await _dbContext.CreateContainerType(containerType);
            return Ok(result);

        } catch (Exception ex) {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }

    }

    [HttpDelete("DeleteContainerType/{containerType}")]
    public async Task<ActionResult<bool>> DeleteContainerType(string containerType) // Not from body
    {
        try
        {
            int numDeleted = await _dbContext.DeleteContainerType(containerType);
            return Ok(numDeleted > 0 ? true : false);
        } catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
        
    }

    [HttpGet("FetchAllContainerTypes")]
    public async Task<ActionResult<List<ContainerTypeReference>>> FetchAllContainerTypes() // GET request should not have body 
    {
        try
        {
            List<ContainerTypeReference> containerTypeReferences = await _dbContext.FetchAllContainerTypes();
            Debug.WriteLine($"Logging: {containerTypeReferences}");
            return Ok(containerTypeReferences);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }


    [HttpGet("FetchContainerTypes/{containerType}")]
    public async Task<ActionResult<List<ContainerTypeReference>>> FetchContainerTypes(string containerType) // GET request should not have body 
    {
        try
        {
            List<ContainerTypeReference> containerTypeReferences = await _dbContext.FetchContainerTypes(containerType);
            Debug.WriteLine($"Logging: {containerTypeReferences}");
            return Ok(containerTypeReferences);
        } catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }


}
