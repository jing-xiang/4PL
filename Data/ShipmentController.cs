using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

using System;
using System.Globalization;
using System.IO;
using System.Net;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Excel;
namespace _4PL.Data
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipmentController : Controller
    {
        private readonly SnowflakeDbContext _dbcontext;

        public ShipmentController(SnowflakeDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet("")]
        public ActionResult Get()
        {
            Dictionary<string, Tuple<Shipment, List<Container>>> shipmentData = new Dictionary<string, Tuple<Shipment, List<Container>>>();
            List<Container> containerList = new List<Container>();
            containerList.Add(new Container());
            shipmentData["test"] = new Tuple<Shipment, List<Container>>(new Shipment(), containerList);
            return Ok(shipmentData);
        }

        // Read
        [HttpGet("List/{fileNameWithoutExtension}")]
        public IActionResult insertExceltoSnowflake (string fileNameWithoutExtension)
        {
            Dictionary<string, Tuple<Shipment, List<Container>>> dict = ReadExcelFile(fileNameWithoutExtension);
         
            foreach(var kvp in dict)
            {
                Shipment shipment = kvp.Value.Item1;
                _dbcontext.CallStoredProcedureForShipment(shipment);

                List<Container> containers = kvp.Value.Item2;
                foreach (var container in containers)
                {
                    _dbcontext.CallStoredProcedureForContainer(container);
                }
            }
            return Ok(dict);
        }

        // Read
        [HttpGet("ShipmentData")]
        public IActionResult fetchShipmentData()
        {
            List<Shipment> shipments = _dbcontext.fetchShipment();
            return Ok(shipments);
        }

        [HttpGet("ContainerData")]
        public IActionResult fetchContainerData()
        {
            List<Container> containers = _dbcontext.fetchContainer();
            return Ok(containers);
        }

        private Dictionary<string, Tuple<Shipment , List<Container>>> ReadExcelFile(string fileNameWithoutExtension)
        {
            Dictionary<string, Tuple<Shipment, List<Container>>> shipmentData = new Dictionary<string, Tuple<Shipment, List<Container>>>();

            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            object misValue = System.Reflection.Missing.Value;
            xlApp = new Excel.Application();

            string fpath = Directory.GetCurrentDirectory() + "\\FileUploads\\" + fileNameWithoutExtension;
            xlWorkBook = xlApp.Workbooks.Open(fpath, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            int lastUsedRow = 0;
            // Find the last real row
            lastUsedRow = xlWorkSheet.Cells.Find("*", System.Reflection.Missing.Value,
                                           System.Reflection.Missing.Value, System.Reflection.Missing.Value,
                                           Excel.XlSearchOrder.xlByRows, Excel.XlSearchDirection.xlPrevious,
                                           false, System.Reflection.Missing.Value, System.Reflection.Missing.Value).Row;

            for (int r = 2; r <= lastUsedRow; r++)
            {
                Excel.Range cell = xlWorkSheet.Cells[r, 1];
                Shipment s = new Shipment();
                Container c = new Container();

                //ways to automatically assign this, given the different data type
                string jobNo = cellIsEmpty(cell) ? "-" : cell.Value2;
                s.Job_No = jobNo;

                cell = nextCell(xlWorkSheet, cell);
                s.Master_BL_No = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Container_Mode = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Place_Of_Loading_ID = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Place_Of_Loading_Name = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Place_Of_Discharge_ID = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Place_Of_Discharge_Name = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Vessel_Name = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Voyage_No = cellIsEmpty(cell) ? "-" : cell.Value2;
           
                cell = nextCell(xlWorkSheet, cell);
                s.ETD_Date = DateTime.Now;

                cell = nextCell(xlWorkSheet, cell);
                //16-Jan-2023
                //01/16/2023 --> MM/dd/yyyy
                //find the cell.Text
                //s.ETA_Date = cellIsEmpty(cell) ? DateTime.Now : DateTime.ParseExact(cell.Text, "d-mmm-yy", null);
                s.ETA_Date = DateTime.Now;

                cell = nextCell(xlWorkSheet, cell);
                s.Carrier_Matchcode = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Carrier_Name = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Carrier_Contract_No = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Carrier_Booking_Reference_No = cellIsEmpty(cell) ? "-" : cell.Value2;
                
                cell = nextCell(xlWorkSheet, cell);
                s.Inco_Terms = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Controlling_Customer_Name = cellIsEmpty(cell) ? "-" : cell.Value2; 

                cell = nextCell(xlWorkSheet, cell);
                s.Shipper_Name = cellIsEmpty(cell) ? "-" : cell.Value2; 

                cell = nextCell(xlWorkSheet, cell);
                s.Consignee_Name = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Total_No_Of_Pieces = cellIsEmpty(cell) ? 0 : (int)cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Package_Type = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Total_No_Of_Volume_Weight_MTQ = cellIsEmpty(cell) ? 0.0 : (double)cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Total_No_Of_Gross_Weight_KGM = cellIsEmpty(cell) ? 0.0 : (double)cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Description = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                s.Shipment_Note = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                c.Container_No = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                c.Container_Type = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                c.Seal_No_1 = cellIsEmpty(cell) ? "-" : cell.Value2;

                cell = nextCell(xlWorkSheet, cell);
                c.Seal_No_2 = cellIsEmpty(cell) ? "-" : cell.Value2;

                c.Shipment_Job_No = jobNo;

                if (!shipmentData.ContainsKey(jobNo))
                {
                    shipmentData[jobNo] = new Tuple<Shipment, List<Container>>(s, new List<Container>());
                }

                shipmentData[jobNo].Item2.Add(c);
            }

            xlWorkBook.Close(true, misValue, misValue);
            xlApp.Quit();
            //System.IO.File.Delete(fpath + ".xlsx");
            return shipmentData;
        }
        private static bool cellIsEmpty(Excel.Range cell)
        {
            return (cell == null || cell.Value2 == null || cell.Text == "");
        }

        private static Excel.Range nextCell(Excel.Worksheet xlWorkSheet, Excel.Range cell)
        {
            return xlWorkSheet.Cells[cell.Row, cell.Column + 1];
        }

        // Create
        [HttpPost]
        public async Task<ActionResult<IList<UploadResult>>> PostFile(
        [FromForm] IEnumerable<IFormFile> files)
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
                            //trustedFileNameForFileStorage = "shipment_excel";

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

    }
}
