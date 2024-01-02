namespace _4PL.Data
{
    public class ShipmentSearchModel
    {
        public string Job_No { get; set; }
        public string Master_BL_No { get; set; }
        public string Place_Of_Loading_Name { get; set; }
        public string Place_Of_Discharge_Name { get; set; }
        public string Vessel_Name { get; set; }
        public string Voyage_No { get; set; }
        public DateTime ETD_Date_From { get; set; }
        public DateTime ETD_Date_To { get; set; }
        public DateTime ETA_Date_From { get; set; }
        public DateTime ETA_Date_To { get; set; }


        public ShipmentSearchModel()
        {
            this.Job_No = "";
            this.Master_BL_No = "";
            this.Place_Of_Loading_Name = "";
            this.Place_Of_Discharge_Name = "";
            this.Vessel_Name = "";
            this.Voyage_No = "";
            this.ETD_Date_From = new DateTime(DateTime.Now.Year, 1, 1);
            this.ETD_Date_To = DateTime.Now;
            this.ETA_Date_From = new DateTime(DateTime.Now.Year, 1, 1);
            this.ETA_Date_To = DateTime.Now;
        }
    }
}
