namespace _4PL.Data
{
    public class Shipment
    {
        public string Job_No { get; set; }  
        public string Master_BL_No { get; set; } = string.Empty;
        public string Container_Mode { get; set; }
        public string Place_Of_Loading_ID { get; set; }
        public string Place_Of_Loading_Name { get; set; }
        public string Place_Of_Discharge_ID { get; set; }
        public string Place_Of_Discharge_Name { get; set; }
        public string Vessel_Name { get; set; }
        public string Voyage_No { get; set; }
        public DateTime ETD_Date { get; set; }
        public DateTime ETA_Date { get; set; }
        public string Carrier_Matchcode { get; set; }
        public string Carrier_Name { get; set; }
        public string Carrier_Contract_No { get; set; }
        public string Carrier_Booking_Reference_No { get; set; }
        public string Inco_Terms { get; set; }
        public string Controlling_Customer_Name { get; set; }
        public string Shipper_Name { get; set; }
        public string Consignee_Name { get; set; }
        public int Total_No_Of_Pieces { get; set; }
        public string Package_Type { get; set; }
        public double Total_No_Of_Volume_Weight_MTQ { get; set; }
        public double Total_No_Of_Gross_Weight_KGM { get; set; }
        public string Description { get; set; }
        public string Shipment_Note { get; set; }
        public List<Container> Container_List {  get; set; }

        public Shipment(
            string Job_No,
            string Master_Bl_No,
            string Container_Mode,
            string Place_Of_Loading_ID,
            string Place_Of_Loading_Name,
            string Place_Of_Discharge_ID,
            string Place_Of_Discharge_Name,
            string Vessel_Name,
            string Voyage_No,
            DateTime ETD_Date,
            DateTime ETA_Date,
            string Carrier_Matchcode,
            string Carrier_Name,
            string Carrier_Contract_No,
            string Carrier_Booking_Reference_No,
            string Inco_Terms,
            string Controlling_Customer_Name,
            string Shipper_Name,
            string Consignee_Name,
            int Total_No_Of_Pieces,
            string Package_Type,
            double Total_No_Of_Volume_Weight_MTQ,
            double Total_No_Of_Gross_Weight_KGM,
            string Description,
            string Shipment_Note,
            List<Container> Container_List
        )
        {
            this.Job_No = Job_No;
            this.Master_BL_No = Master_Bl_No;
            this.Container_Mode = Container_Mode;
            this.Place_Of_Loading_ID = Place_Of_Loading_ID;
            this.Place_Of_Loading_Name = Place_Of_Loading_Name;
            this.Place_Of_Discharge_ID = Place_Of_Discharge_ID;
            this.Place_Of_Discharge_Name = Place_Of_Discharge_Name;
            this.Vessel_Name = Vessel_Name;
            this.Voyage_No = Voyage_No;
            this.ETD_Date = ETD_Date;
            this.ETA_Date =ETA_Date;
            this.Carrier_Matchcode = Carrier_Matchcode;
            this.Carrier_Name = Carrier_Name;
            this.Carrier_Contract_No = Carrier_Contract_No;
            this.Carrier_Booking_Reference_No = Carrier_Booking_Reference_No;
            this.Inco_Terms = Inco_Terms;
            this.Controlling_Customer_Name = Controlling_Customer_Name;
            this.Shipper_Name = Shipper_Name;
            this.Consignee_Name = Consignee_Name;
            this.Total_No_Of_Pieces = Total_No_Of_Pieces;
            this.Package_Type = Package_Type;
            this.Total_No_Of_Volume_Weight_MTQ = Total_No_Of_Volume_Weight_MTQ;
            this.Total_No_Of_Gross_Weight_KGM = Total_No_Of_Gross_Weight_KGM;
            this.Description = Description;
            this.Shipment_Note = Shipment_Note;
            this.Container_List = Container_List;
        }

        public Shipment()
        {
            this.Job_No = "";
            this.Master_BL_No = "";
            this.Container_Mode = "";
            this.Place_Of_Loading_ID = "";
            this.Place_Of_Loading_Name = "";
            this.Place_Of_Discharge_ID = "";
            this.Place_Of_Discharge_Name = "";
            this.Vessel_Name = "";
            this.Voyage_No = "";
            this.ETD_Date = DateTime.Now;
            this.ETA_Date = DateTime.Now;
            this.Carrier_Matchcode = "";
            this.Carrier_Name = "";
            this.Carrier_Contract_No = "";
            this.Carrier_Booking_Reference_No = "";
            this.Inco_Terms = "";
            this.Controlling_Customer_Name = "";
            this.Shipper_Name = "";
            this.Consignee_Name = "";
            this.Total_No_Of_Pieces = 0;
            this.Package_Type = "";
            this.Total_No_Of_Volume_Weight_MTQ = 0.0;
            this.Total_No_Of_Gross_Weight_KGM = 0.0;
            this.Description = "";
            this.Shipment_Note = "";
            this.Container_List = new List<Container>();
        }
    }

}
