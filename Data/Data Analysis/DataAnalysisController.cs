using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

        [HttpGet("FetchReports")]
        public async Task<ActionResult<List<DataReport>>> FetchDataReports()
        {
            try
            {
                List<DataReport> result = _dbContext.FetchDataReports();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound($"{ex.Message}");
            }
        }

        [HttpPost("AddReport")]
        public async Task<IActionResult> AddNewReport([FromBody] DataReport newReport)
        {
            try
            {
                _dbContext.AddNewReport(newReport);
                return Ok($"{newReport.Title} successfully added.");
            }
            catch (DuplicateNameException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("UpdateReport")]
        public async Task<IActionResult> UpdateReport([FromBody] DataReport updatedReport)
        {
            try
            {
                _dbContext.UpdateReport(updatedReport);
                return Ok("Report successfully updated.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete{reportTitle}")]
        public async Task<IActionResult> DeleteReport(string reportTitle)
        {
            try
            {
                _dbContext.DeleteReport(reportTitle);
                return Ok("Report successfully deleted.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}