namespace _4PL.Data
{
    public class ShipmentCharge
    {
        public string Shipment_Job_No { get; set; }
        public string Charge_Code { get; set; }
        public string Charge_Name { get; set; }
        public string Creditor_Name { get; set; }
        public string OS_Charge_Currency { get; set; }
        public string Charge_Currency { get; set; }
        public double Charge_Ex_Rate { get; set; }
        public string VAT_Code { get; set; }
        public decimal Charge_Est_Cost_Net_OS_Amount { get; set; }
        public decimal Charge_Est_Cost_Net_Amount { get; set; }
        public string Lane_ID { get; set; }
        public string Remarks { get; set; }

        public ShipmentCharge()
        {
            this.Shipment_Job_No = "";
            this.Charge_Code = "";
            this.Charge_Name = "";
            this.Creditor_Name = "";
            this.OS_Charge_Currency = "";
            this.Charge_Ex_Rate = 1.00;
            this.Charge_Currency = "";
            this.VAT_Code = "FREESVC";
            this.Charge_Est_Cost_Net_OS_Amount = 0;
            this.Charge_Est_Cost_Net_Amount = 0;
            this.Lane_ID = "";
            this.Remarks = "FREETEXT";
        }
    }
}
