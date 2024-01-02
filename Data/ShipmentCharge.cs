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
        public decimal Charge_Est_Cost_VAT_OS_Amount { get; set; }
        public decimal Charge_Est_Cost_VAT_Amount { get; set; }

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
            this.Remarks = "";
            this.Charge_Est_Cost_VAT_OS_Amount = 0;
            this.Charge_Est_Cost_VAT_Amount = 0;
        }

        public ShipmentCharge(ShipmentCharge sc)
        {
            this.Shipment_Job_No = sc.Shipment_Job_No;
            this.Charge_Code = sc.Charge_Code;
            this.Charge_Name = sc.Charge_Name;
            this.Creditor_Name = sc.Creditor_Name;
            this.OS_Charge_Currency = sc.OS_Charge_Currency;
            this.Charge_Ex_Rate = sc.Charge_Ex_Rate;
            this.Charge_Currency = sc.Charge_Currency;
            this.VAT_Code = sc.VAT_Code;
            this.Charge_Est_Cost_Net_OS_Amount = sc.Charge_Est_Cost_Net_OS_Amount;
            this.Charge_Est_Cost_Net_Amount = sc.Charge_Est_Cost_Net_Amount;
            this.Lane_ID = sc.Lane_ID;
            this.Remarks = sc.Remarks;
            this.Charge_Est_Cost_VAT_OS_Amount = sc.Charge_Est_Cost_VAT_OS_Amount;
            this.Charge_Est_Cost_VAT_Amount = sc.Charge_Est_Cost_VAT_Amount;
        }
    }

}
