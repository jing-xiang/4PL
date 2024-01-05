using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace _4PL.Data;

[Route("api/[controller]")]
[ApiController]
public class ContainerTypeMappingController : Controller
{
    private readonly SnowflakeDbContext _dbContext;

    public ContainerTypeMappingController(SnowflakeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /**
     * APIs 
     * Create a container type mapping (after submitting in form) 
     * Delete a container type mapping
     * Get all container type mappings (Search) 
     * TBC: Update container type mapping
    **/

    [HttpPost("CreateContainerTypeMapping")]
    public async Task<ActionResult<string>> CreateContainerTypeMapping([FromBody] ContainerTypeMappingReference mapping) // TBC: FromBody
    {
        try
        {
            string result = await _dbContext.CreateContainerTypeMapping(mapping.Other_Container_Type_Name, mapping.Source, mapping.Container_Type);
            return Ok(result);

        } catch (Exception ex) {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }

    }

    [HttpDelete("DeleteContainerTypeMapping/{otherContainerTypeName}/{source}")]
    public async Task<ActionResult<bool>> DeleteContainerTypeMapping(string otherContainerTypeName, string source) // Not from body
    {
        try
        {
            int numDeleted = await _dbContext.DeleteContainerTypeMapping(otherContainerTypeName, source);
            return Ok(numDeleted > 0 ? true : false);
        } catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
        
    }

    [HttpGet("FetchContainerTypesList")]
    public async Task<ActionResult<List<ContainerTypeReference>>> FetchContainerTypesList() // GET request should not have body 
    {
        try
        {
            List<ContainerTypeReference> containerTypes = await _dbContext.FetchAllContainerTypes();
            Debug.WriteLine($"Logging: {containerTypes}");
            return Ok(containerTypes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }


    [HttpGet("FetchContainerTypeMappings/{otherContainerTypeName}/{source}/{containerType}")]
    public async Task<ActionResult<List<ContainerTypeMappingReference>>> FetchContainerTypeMappings(string otherContainerTypeName, string source, string containerType) // GET request should not have body 
    {
        Debug.WriteLine($"Server side Parameters: otherContainerTypeName={otherContainerTypeName}, source={source}, containerType={containerType}");
        try
        {
            List<ContainerTypeMappingReference> containerTypeMappings = await _dbContext.FetchContainerTypeMappings(otherContainerTypeName.Trim(), source.Trim(), containerType.Trim());
            Debug.WriteLine($"Logging: {containerTypeMappings}");
            return Ok(containerTypeMappings);
        } catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }


}
