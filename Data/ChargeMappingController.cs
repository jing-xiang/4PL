using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace _4PL.Data;

[Route("api/[controller]")]
[ApiController]
public class ChargeMappingController : Controller
{
    private readonly SnowflakeDbContext _dbContext;

    public ChargeMappingController(SnowflakeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /**
     * APIs 
     * Create a charge mapping (after submitting in form) 
     * Delete a charge mapping
     * Get all charge mappings (Search) 
     * Update charge mapping
    **/

    [HttpPost("CreateChargeMapping")]
    public async Task<ActionResult<string>> CreateChargeMapping([FromBody] ChargeMappingReference mapping) // TBC: FromBody
    {
        try
        {
            string result = await _dbContext.CreateChargeMapping(mapping.Other_Charge_Description_Name, mapping.Source, mapping.Charge_Description);
            return Ok(result);

        } catch (Exception ex) {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }

    }

    [HttpDelete("DeleteChargeMapping/{otherChargeDescriptionName}/{source}")]
    public async Task<ActionResult<bool>> DeleteChargeMapping(string otherChargeDescriptionName, string source) // Not from body
    {
        try
        {
            int numDeleted = await _dbContext.DeleteChargeMapping(otherChargeDescriptionName, source);
            return Ok(numDeleted > 0 ? true : false);
        } catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
        
    }

    [HttpGet("FetchChargeDescriptionsList")]
    public async Task<ActionResult<List<string>>> FetchChargeDescriptionsList() // GET request should not have body 
    {
        try
        {
            List<string> chargeDescriptionsList = await _dbContext.FetchChargeDescriptionsList(); // TO EDIT 
            Debug.WriteLine($"Logging: {chargeDescriptionsList}");
            return Ok(chargeDescriptionsList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }


    [HttpPost("FetchChargeMappings")]
    public async Task<ActionResult<List<ChargeMappingReference>>> FetchChargeMappings(ChargeMappingReference searchChargeMapping) 
    {
        try
        {
            List<ChargeMappingReference> chargeMappings = await _dbContext.FetchChargeMappings(searchChargeMapping.Other_Charge_Description_Name, searchChargeMapping.Source, searchChargeMapping.Charge_Description);
            Debug.WriteLine($"Logging: {chargeMappings}");
            return Ok(chargeMappings);
        } catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    [HttpPost("UpdateChargeMapping")]
    public async Task<ActionResult<string>> UpdateChargeMapping(ChargeMappingReference chargeMapping)
    {
        try
        {
            string resultId = await _dbContext.UpdateChargeMapping(chargeMapping.Id.ToString(), chargeMapping.Other_Charge_Description_Name, chargeMapping.Source, chargeMapping.Charge_Description);
            Debug.WriteLine($"Logging: {resultId}");
            return Ok(resultId);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }




}
