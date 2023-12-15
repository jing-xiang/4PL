using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Globalization;
using System.IO;
using System.Net;
using Excel = Microsoft.Office.Interop.Excel;

namespace _4PL.Data
{
    [Route("api/[controller]")]
    [ApiController]
    public class RateCardController : Controller
    {
        private readonly SnowflakeDbContext _dbContext;

        public RateCardController(SnowflakeDbContext dbContext)
        {
            _dbContext = dbContext;
        }      


        // Create
        [HttpPost("UploadExcel")]
        public async Task<ActionResult<IList<UploadResult>>> PostFile(
            [FromForm] IEnumerable<IFormFile> files
        )
        {
            var maxAllowedFiles = 1;
            long maxFileSize = 1024 * 1000;
            var filesProcessed = 0;
            var resourcePath = new Uri($"{Request.Scheme}://{Request.Host}/");
            List<UploadResult> uploadResults = new();

            foreach (var file in files)
            {
                var uploadResult = new UploadResult();
                string trustedFileNameForFileStorage;
                var untrustedFileName = file.FileName;
                uploadResult.FileName = untrustedFileName;
                var trustedFileNameForDisplay =
                    WebUtility.HtmlEncode(untrustedFileName);

                if (filesProcessed < maxAllowedFiles)
                {
                    if (file.Length == 0)
                    {
                        //logger.LogInformation("{FileName} length is 0 (Err: 1)",
                        //    trustedFileNameForDisplay);
                        //uploadResult.ErrorCode = 1;
                    }
                    else if (file.Length > maxFileSize)
                    {
                        //logger.LogInformation("{FileName} of {Length} bytes is " +
                        //    "larger than the limit of {Limit} bytes (Err: 2)",
                        //    trustedFileNameForDisplay, file.Length, maxFileSize);
                        //uploadResult.ErrorCode = 2;
                    }
                    else
                    {
                        try
                        {
                            trustedFileNameForFileStorage = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                            //trustedFileNameForFileStorage = "ratecard_excel";

                            //var path = Path.Combine(env.ContentRootPath,
                            //    env.EnvironmentName, "unsafe_uploads",
                            //    trustedFileNameForFileStorage);
                            var path = Path.Combine(Directory.GetCurrentDirectory(),
                                "FileUploads",
                                trustedFileNameForFileStorage + ".xlsx");

                            await using FileStream fs = new(path, FileMode.Create);
                            await file.CopyToAsync(fs);

                            //logger.LogInformation("{FileName} saved at {Path}",
                            //    trustedFileNameForDisplay, path);
                            uploadResult.Uploaded = true;
                            uploadResult.StoredFileName = trustedFileNameForFileStorage;
                        }
                        catch (IOException ex)
                        {
                            //logger.LogError("{FileName} error on upload (Err: 3): {Message}",
                            //    trustedFileNameForDisplay, ex.Message);
                            uploadResult.ErrorCode = 3;
                        }
                    }

                    filesProcessed++;
                }
                else
                {
                    //logger.LogInformation("{FileName} not uploaded because the " +
                    //    "request exceeded the allowed {Count} of files (Err: 4)",
                    //    trustedFileNameForDisplay, maxAllowedFiles);
                    uploadResult.ErrorCode = 4;
                }

                uploadResults.Add(uploadResult);
            }

            return new CreatedResult(resourcePath, uploadResults);
        }


        //Delete
        [HttpDelete("DeleteRatecard/{ratecardId}")]
        public ActionResult DeleteRatecard(string ratecardId)
        {
            int numDeleted = _dbContext.DeleteRatecard(ratecardId);

            return Ok(numDeleted);
        }

        [HttpDelete("DeleteCharge/{chargeId}")]
        public ActionResult DeleteCharge(string chargeId)
        {
            int numDeleted = _dbContext.DeleteCharge(chargeId);

            return Ok(numDeleted);
        }

        //Create
        [HttpPost("CreateRcTransaction")]
        public async Task<ActionResult<string>> CreateRcTransaction([FromBody] string fileNameWithoutExtension)
        {

            List<RateCard> ratecards = ReadRatecardExcel(fileNameWithoutExtension);

            //1.Create transaction
            string transactionId = await _dbContext.CreateRcTransaction(null);

            //For each rate card, ...
            foreach(RateCard rc in ratecards)
            {
                //2. Create ratecard (reference transactionId)
                string ratecardId = await _dbContext.CreateRatecard(rc, transactionId);

                //3. Create charges (reference transactionId and ratecardID)
                List<string> chargeIds = _dbContext.CreateCharges(rc.Charges, transactionId, ratecardId);

            }

            return Ok(transactionId);
        }

        //Read
        [HttpGet("GetTransactionDetails/{transactionId}")]
        [HttpGet("GetTransactionDetails/{transactionId}/{offset}")]
        public ActionResult getTransaction(string transactionId, long offset=0L)
        {
            List<RateCard> ratecards = _dbContext.GetRatecardsFromTransactionId(transactionId, offset, 50L);

            foreach(RateCard ratecard in ratecards) 
            {
                ratecard.Charges = _dbContext.GetChargesFromRatecardId(ratecard.Id.ToString(), offset, 50L);
            }

            return Ok(ratecards);
        }

        //Read
        [HttpGet("GetRateCard/{ratecardId}")]
        public ActionResult getRatecard(string ratecardId)
        {
            return Ok(_dbContext.GetRatecard(ratecardId));
        }

        /*
         * Helper methods
         * 
         */
        private List<RateCard> ReadRatecardExcel(string fileNameWithoutExtension)
        {
            /*
             * https://net-informations.com/csharp/excel/csharp-open-excel.htm
             */
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            object misValue = System.Reflection.Missing.Value;
            xlApp = new Excel.Application();

            //Requires full path
            //string fpath = Directory.GetCurrentDirectory() + "\\FileUploads\\" + fileNameWithoutExtension;
            string fpath = Path.Combine(Directory.GetCurrentDirectory(), "FileUploads", fileNameWithoutExtension);

            xlWorkBook = xlApp.Workbooks.Open(fpath, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            List<RateCard> ratecards = [];

            //Iterate row (max 1000 rows)
            //First row is for headers
            for (int r = 2; r < 1000; r++)
            {
                Excel.Range cell = xlWorkSheet.Cells[r, 1];

                //Check if first cell of line is empty
                if (cellIsEmpty(cell))
                {
                    break;
                }

                //Manually iterate through rows
                RateCard rc = new RateCard();

                rc.Lane_ID = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Controlling_Customer_Matchcode = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Controlling_Customer_Name = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Transport_Mode = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Function = cellIsEmpty(cell) ? "-" : cell.Value2;


                //Dates
                cell = nextCell(xlWorkSheet, cell);
                rc.Rate_Validity_From = cellIsEmpty(cell) ? DateTime.Now : DateTime.ParseExact(cell.Text, "M/d/yyyy", null);

                cell = nextCell(xlWorkSheet, cell);
                rc.Rate_Validity_To = cellIsEmpty(cell) ? DateTime.Now : DateTime.ParseExact(cell.Text, "M/d/yyyy", null);


                cell = nextCell(xlWorkSheet, cell);
                rc.POL_Name = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.POL_Country = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.POL_Port = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.POD_Name = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.POD_Country = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.POD_Port = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Creditor_Matchcode = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Creditor_Name = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Pickup_Address = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Delivery_Address = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Dangerous_Goods = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Temperature_Controlled = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Container_Mode = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Container_Type = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                rc.Local_Currency = cellIsEmpty(cell) ? "-" : cell.Value2;

                //Add charges
                cell = nextCell(xlWorkSheet, cell);
                while (!cellIsEmpty(cell))
                {
                    Charge charge = new Charge();
                    charge.Charge_Description = cellIsEmpty(cell) ? "-" : cell.Value2;

                    cell = nextCell(xlWorkSheet, cell);
                    charge.Calculation_Base = cellIsEmpty(cell) ? "-" : cell.Value2;

                    cell = nextCell(xlWorkSheet, cell);
                    charge.Min = cellIsEmpty(cell) ? 0M : (decimal)cell.Value2;

                    cell = nextCell(xlWorkSheet, cell);
                    charge.Unit_Price = cellIsEmpty(cell) ? 0M : (decimal)cell.Value2;

                    cell = nextCell(xlWorkSheet, cell);
                    charge.Currency = cellIsEmpty(cell) ? "-" : cell.Value2;

                    cell = nextCell(xlWorkSheet, cell);
                    charge.Per_Percent = cellIsEmpty(cell) ? 0M : (decimal)cell.Value2;

                    cell = nextCell(xlWorkSheet, cell);
                    charge.Charge_Code = cellIsEmpty(cell) ? "-" : cell.Value2;

                    rc.Charges.Add(charge);
                    cell = nextCell(xlWorkSheet, cell);
                }

                //Add rc to list
                ratecards.Add(rc);

            }

            xlWorkBook.Close(true, misValue, misValue);
            xlApp.Quit();


            System.IO.File.Delete(fpath + ".xlsx");

            return ratecards;
        }

        private static bool cellIsEmpty(Excel.Range cell)
        {
            return (cell == null || cell.Value2 == null || cell.Text == "");
        }

        private static Excel.Range nextCell(Excel.Worksheet xlWorkSheet, Excel.Range cell)
        {

            return xlWorkSheet.Cells[cell.Row, cell.Column + 1];
            //return cell;
        }
    }


}
