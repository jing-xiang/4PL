using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace _4PL.Data;

[Route("api/[controller]")]
[ApiController]
public class ChargeController : Controller
{
    private readonly SnowflakeDbContext _dbContext;

    public ChargeController(SnowflakeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /**
     * APIs 
     * Create a charge (after submitting in form) 
     * Delete a charge 
     * Get all charges (Search) 
     * New: Edit existing charge 
    **/

    [HttpPost("CreateCharge")]
    public async Task<ActionResult<string>> CreateCharge([FromBody] ChargeReference charge) // TBC: FromBody
    {
        try
        {
            string result = await _dbContext.CreateCharge(charge.Charge_Code, charge.Charge_Description);
            return Ok(result);

        } catch (Exception ex) {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }

    }

    [HttpDelete("DeleteCharge/{chargeDescription}")]
    public async Task<ActionResult<bool>> DeleteCharge(string chargeDescription) // Not from body
    {
        try
        {
            int numDeleted = await _dbContext.DeleteChargeFromDescription(chargeDescription);
            return Ok(numDeleted > 0 ? true : false);
        } catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
        
    }

    [HttpGet("FetchAllCharges")]
    public async Task<ActionResult<List<ChargeReference>>> FetchAllCharges() // GET request should not have body 
    {
        try
        {
            List<ChargeReference> chargeReferences = await _dbContext.FetchAllCharges();
            Debug.WriteLine($"Logging: {chargeReferences}");
            return Ok(chargeReferences);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }


    [HttpGet("FetchChargesByDescription/{chargeDescription}")]
    public async Task<ActionResult<List<ChargeReference>>> FetchChargesByDescription(string chargeDescription) // GET request should not have body 
    {
        try
        {
            List<ChargeReference> chargeReferences = await _dbContext.FetchChargesByDescription(chargeDescription);
            Debug.WriteLine($"Logging: {chargeReferences}");
            return Ok(chargeReferences);
        } catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    [HttpGet("FetchChargesByCode/{chargeCode}")]
    public async Task<ActionResult<List<ChargeReference>>> FetchChargesByCode(string chargeCode) // GET request should not have body 
    {
        try
        {
            List<ChargeReference> chargeReferences = await _dbContext.FetchChargesByCode(chargeCode);
            Debug.WriteLine($"Logging: {chargeReferences}");
            return Ok(chargeReferences);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    [HttpGet("FetchChargesByBoth/{chargeCode}/{chargeDescription}")]
    public async Task<ActionResult<List<ChargeReference>>> FetchChargesByBoth(string chargeCode, string chargeDescription) // GET request should not have body 
    {
        try
        {
            List<ChargeReference> chargeReferences = await _dbContext.FetchChargesByBoth(chargeCode, chargeDescription);
            Debug.WriteLine($"Logging: {chargeReferences}");
            return Ok(chargeReferences);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    [HttpPost("UpdateChargeCode")]
    public async Task<ActionResult<string>> UpdateChargeCode([FromBody] ChargeReference updatedCharge) // Cannot have more than one attribute in body
    {
        try
        {
            string updatedChargeCode = await _dbContext.UpdateChargeCode(updatedCharge.Charge_Description, updatedCharge.Charge_Code);
            return Ok(updatedChargeCode);

        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }

    }

    [HttpGet("FetchChargeDescriptionsInMappings")]
    public async Task<ActionResult<List<string>>> FetchChargeDescriptionsInMappings() // GET request should not have body 
    {
        try
        {
            List<string> mappingChargeDescriptions = await _dbContext.FetchChargeDescriptionsInMappings();
            Debug.WriteLine($"Logging: {mappingChargeDescriptions}");
            return Ok(mappingChargeDescriptions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"{ex.GetType().Name}: {ex.Message}");
        }
    }


}
