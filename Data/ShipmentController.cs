using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

using System;
using System.Globalization;
using System.IO;
using System.Net;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Excel;
using Org.BouncyCastle.Utilities;
using Microsoft.EntityFrameworkCore;
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

        //Create and Update Shipment
        [HttpPost("CreateShipment")]
        public IActionResult insertFormtoSnowflake([FromBody] Shipment shipment)
        {
            string uploadMessage = _dbcontext.InsertShipment(shipment);
            return Ok(uploadMessage);
        }

        [HttpPost("CreateShipments")]
        public IActionResult insertExceltoSnowflake ([FromBody] string fileNameWithoutExtension)
        {
            Dictionary<string, Shipment> dict = ReadExcelFile(fileNameWithoutExtension);
            var response = _dbcontext.InsertShipments(dict.Values.ToList());
               
            var returnOutput = "";
            if (response.Any())
            {
                returnOutput += "Error uploading file. \n";
                returnOutput += $"Existing Shipments: {string.Join(", ", response)}";
            } else
            {
                returnOutput +=  dict.ToList().Count + " new shipment(s) uploaded.";
            }
            return Ok(returnOutput);
        }

        [HttpPost("UpdateShipment")]
        public IActionResult updateFormtoSnowflake([FromBody] Shipment shipment)
        {
            _dbcontext.UpdateShipment(shipment);
            return Ok();
        }

        // Create and Update Container
        [HttpPost("CreateContainer")]
        public IActionResult insertFormtoSnowflake([FromBody] Container container)
        {
            _dbcontext.InsertContainer(container);
            return Ok();
        }

        // Create
        [HttpPost("UploadExcel")]
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
                        uploadResult.ErrorCode = 1;
                    }
                    else if (file.Length > maxFileSize)
                    {
                        uploadResult.ErrorCode = 2;
                    }
                    else
                    {
                        try
                        {
                            trustedFileNameForFileStorage = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

                            var path = Path.Combine(Directory.GetCurrentDirectory(),
                                "FileUploads",
                                trustedFileNameForFileStorage + ".xlsx");

                            await using FileStream fs = new(path, FileMode.Create);
                            await file.CopyToAsync(fs);

                            uploadResult.Uploaded = true;
                            uploadResult.StoredFileName = trustedFileNameForFileStorage;
                        }
                        catch (IOException ex)
                        {
                            uploadResult.ErrorCode = 3;
                        }
                    }

                    filesProcessed++;
                }
                else
                {
                    uploadResult.ErrorCode = 4;
                }

                uploadResults.Add(uploadResult);
            }

            return new CreatedResult(resourcePath, uploadResults);
        }

        //Delete
        [HttpDelete("DeleteShipment/{Shipment_Job_No}")]
        public ActionResult DeleteShipment(string Shipment_Job_No)
        {
            System.Console.WriteLine($"Deleting shipment: {Shipment_Job_No}");
            int numDeleted = _dbcontext.DeleteShipment(Shipment_Job_No);

            return Ok(numDeleted > 0 ? true : false);
        }

        [HttpDelete("DeleteContainer/{Shipment_Job_No}/{Container_No}")]
        public ActionResult DeleteContainer(string Shipment_Job_No, string Container_No)
        {
            int numDeleted = _dbcontext.DeleteContainer(Shipment_Job_No, Container_No);

            return Ok(numDeleted > 0 ? true : false);
        }

        [HttpGet("Search")]
        public IActionResult Search(string Job_No, string Master_BL_No, string Place_Of_Loading_Name, string Place_Of_Discharge_Name, string Vessel_Name, string Voyage_No, string Container_No, string Container_Type, 
            DateTime ETD_Date_From, DateTime ETD_Date_To, DateTime ETA_Date_From, DateTime ETA_Date_To)
        {
            List<Shipment> shipments = _dbcontext.fetchShipments(Job_No, Master_BL_No, Place_Of_Loading_Name, Place_Of_Discharge_Name, Vessel_Name, Voyage_No, Container_No, Container_Type, 
                ETD_Date_From, ETD_Date_To, ETA_Date_From, ETA_Date_To);
            return Ok(shipments);
        }


        [HttpGet("ShipmentData/{Shipment_Job_No}")]
        public IActionResult fetchShipmentData(string Shipment_Job_No)
        {
            Shipment shipments = _dbcontext.fetchShipment(Shipment_Job_No);
            return Ok(shipments);
        }

        [HttpGet("ContainerData")]
        [HttpGet("ContainerData/{Shipment_Job_No}")]
        public IActionResult fetchContainerData(string Shipment_Job_No)
        {
            List<Container> containers = new();
            if (Shipment_Job_No != null)
            {
                containers = _dbcontext.fetchContainers(Shipment_Job_No);
            }
            return Ok(containers);
        }

        [HttpPost("ShipmentCharges")]
        public IActionResult fetchShipmentCharges([FromBody] Shipment s) 
        {
            //Container Type : # of Containers with the same container type
            Dictionary<string, int> containerTypeCounterMap = new Dictionary<string, int>();
            List<Container> containers = _dbcontext.fetchContainers(s.Job_No); 
            foreach (Container c in containers)
            {
                if (containerTypeCounterMap.ContainsKey(c.Container_Type))
                {
                    containerTypeCounterMap[c.Container_Type]++;
                } else
                {
                    containerTypeCounterMap[c.Container_Type] = 1;
                }
            }

            //Charge description : Total cost
            Dictionary<string, ShipmentCharge> chargeCostMap = new Dictionary<string, ShipmentCharge>();
            List<RateCard> ratecards = _dbcontext.GetShipmentRatecards(s);
            Dictionary<string, int> containerTypeNoRatecards = containerTypeCounterMap;
            Console.WriteLine("count ratecards found: " + ratecards.Count);
            //for loop the ratecard ids, multiply by # of containers for each charge, add to dictionary accordingly
            foreach (RateCard r in ratecards)
            {
                if (containerTypeCounterMap.ContainsKey(r.Container_Type))
                {
                    int containerCount = containerTypeCounterMap[r.Container_Type]; 
                    containerTypeNoRatecards.Remove(r.Container_Type);
                    List<Charge> charges = _dbcontext.GetChargesFromRatecardId(r.Id.ToString(), 0, 100);
                    foreach (Charge c in charges)
                    {
                        if (chargeCostMap.ContainsKey(c.Charge_Description))
                        {
                            ShipmentCharge temp = chargeCostMap[c.Charge_Description];
                            
                            if (string.Equals(c.Calculation_Base, "Per Container", StringComparison.CurrentCultureIgnoreCase))
                            {
                                temp.Charge_Est_Cost_Net_OS_Amount += c.OS_Unit_Price * containerCount;
                                temp.Charge_Est_Cost_Net_Amount += c.Unit_Price * containerCount;
                                temp.Remarks += $", {containerCount} {r.Container_Type} @ {c.OS_Unit_Price} {c.OS_Currency}/Container";
                            }
                            else
                            {
                                temp.Charge_Est_Cost_Net_OS_Amount += c.OS_Unit_Price;
                                temp.Charge_Est_Cost_Net_Amount += c.Unit_Price;
                                temp.Remarks += $", {containerCount} {r.Container_Type} @ {c.OS_Unit_Price} {c.OS_Currency}";
                            }
                            chargeCostMap[c.Charge_Description] = temp;
                        }
                        else
                        {
                            ShipmentCharge newCharge = new();
                            newCharge.Charge_Name = c.Charge_Description;
                            newCharge.Charge_Code = c.Charge_Code;
                            newCharge.OS_Charge_Currency = c.OS_Currency;
                            newCharge.Charge_Currency = c.Currency;
                            newCharge.Creditor_Name = r.Creditor_Name;
                            newCharge.Lane_ID = r.Lane_ID;
                          
                            if (string.Equals(c.Calculation_Base, "Per Container", StringComparison.CurrentCultureIgnoreCase))
                            {
                                newCharge.Charge_Est_Cost_Net_OS_Amount += c.OS_Unit_Price * containerCount;
                                newCharge.Charge_Est_Cost_Net_Amount += c.Unit_Price * containerCount;
                                newCharge.Remarks = $"{c.Charge_Code}: {containerCount} {r.Container_Type} @ {c.OS_Unit_Price} {c.OS_Currency}/Container";
                            }
                            else
                            {
                                newCharge.Charge_Est_Cost_Net_OS_Amount += c.OS_Unit_Price;
                                newCharge.Charge_Est_Cost_Net_Amount += c.Unit_Price;
                                newCharge.Remarks = $"{c.Charge_Code}: {containerCount} {r.Container_Type} @ {c.OS_Unit_Price} {c.OS_Currency}";
                            }
                            chargeCostMap[c.Charge_Description] = newCharge;
                        }
                    }
                }
            }

            List<ShipmentCharge> shipmentCharges = new();
            foreach (var kvp in chargeCostMap)
            {
                shipmentCharges.Add(kvp.Value);
            }

            List<string> containerTypeNoRatecardslist = containerTypeNoRatecards.Keys.ToList();
            Tuple<List<ShipmentCharge>, List<string>> result = new Tuple<List<ShipmentCharge>, List<string>>(shipmentCharges, containerTypeNoRatecardslist);
            return Ok(result);
        }

        [HttpGet("FetchActualCharges/{Shipment_Job_No}")]
        public IActionResult fetchActualCharges(string Shipment_Job_No)
        {
            List<ActualShipmentCharge> actualCharges = new();
            actualCharges = _dbcontext.fetchActualCharges(Shipment_Job_No);
            return Ok(actualCharges);
        }

        [HttpPost("CreateShipmentCharges")]
        public IActionResult CreateShipmentCharges([FromBody] List<ShipmentCharge> shipmentcharges)
        { 
            foreach (ShipmentCharge sc in shipmentcharges)
            {
                _dbcontext.InsertShipmentCharge(sc);
            }
            return Ok();
        }

        [HttpGet("ShipmentChargesData/{Shipment_Job_No}")]
        public IActionResult GetInitialShipmentCharges(string Shipment_Job_No)
        {
            List<ShipmentCharge> shipmentcharges = new();
            if (Shipment_Job_No != null)
            {
                shipmentcharges = _dbcontext.fetchShipmentCharges(Shipment_Job_No);
            }
            return Ok(shipmentcharges);
        }

        [HttpDelete("DeleteShipmentCharge/{Shipment_Job_No}/{encodedChargeName}")]
        public ActionResult DeleteShipmentCharge(string Shipment_Job_No, string encodedChargeName)
        {
            string Charge_Name = Uri.UnescapeDataString(encodedChargeName);
            System.Console.WriteLine($"chargename decoded {Charge_Name}");
            int numDeleted = _dbcontext.DeleteShipmentCharge(Shipment_Job_No, Charge_Name);

            return Ok(numDeleted > 0 ? true : false);
        }

        private Dictionary<string, Shipment> ReadExcelFile(string fileNameWithoutExtension)
        {
        Dictionary<string, Shipment> shipmentData = new Dictionary<string, Shipment>();

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
            string jobNo = cellIsEmpty(cell) ? "" : cell.Value2;
            s.Job_No = jobNo;

            cell = nextCell(xlWorkSheet, cell);
            s.Master_BL_No = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Container_Mode = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Place_Of_Loading_ID = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Place_Of_Loading_Name = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Place_Of_Discharge_ID = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Place_Of_Discharge_Name = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Vessel_Name = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Voyage_No = cellIsEmpty(cell) ? "" : cell.Value2;
           
            cell = nextCell(xlWorkSheet, cell);
            s.ETD_Date = cellIsEmpty(cell) ? DateTime.Now : DateTime.ParseExact(cell.Text, "d-MMM-yy", null);

            cell = nextCell(xlWorkSheet, cell);
            s.ETA_Date = cellIsEmpty(cell) ? DateTime.Now : DateTime.ParseExact(cell.Text, "d-MMM-yy", null);

            cell = nextCell(xlWorkSheet, cell);
            s.Carrier_Matchcode = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Carrier_Name = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Carrier_Contract_No = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Carrier_Booking_Reference_No = cellIsEmpty(cell) ? "" : cell.Value2;
                
            cell = nextCell(xlWorkSheet, cell);
            s.Inco_Terms = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Controlling_Customer_Name = cellIsEmpty(cell) ? "" : cell.Value2; 

            cell = nextCell(xlWorkSheet, cell);
            s.Shipper_Name = cellIsEmpty(cell) ? "" : cell.Value2; 

            cell = nextCell(xlWorkSheet, cell);
            s.Consignee_Name = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Total_No_Of_Pieces = cellIsEmpty(cell) ? 0 : (int)cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Package_Type = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Total_No_Of_Volume_Weight_MTQ = cellIsEmpty(cell) ? 0.0 : (double)cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Total_No_Of_Gross_Weight_KGM = cellIsEmpty(cell) ? 0.0 : (double)cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Description = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            s.Shipment_Note = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            c.Container_No = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            c.Container_Type = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            c.Seal_No_1 = cellIsEmpty(cell) ? "" : cell.Value2;

            cell = nextCell(xlWorkSheet, cell);
            c.Seal_No_2 = cellIsEmpty(cell) ? "" : cell.Value2;

            c.Shipment_Job_No = jobNo;

            if (!shipmentData.ContainsKey(jobNo))
            {
                shipmentData[jobNo] = s;
            }

            shipmentData[jobNo].Container_List.Add(c);
        }

        xlWorkBook.Close(true, misValue, misValue);
        xlApp.Quit();
        System.IO.File.Delete(fpath + ".xlsx");
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

    }
}