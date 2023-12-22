namespace _4PL.Data
{
    public class ActualShipmentCharge
    {
        public string Shipment_Job_No { get; set; }
        public string Charge_Code { get; set; }
        public string Charge_Name { get; set; }
        public string Creditor_Name { get; set; }
        public string Charge_Currency { get; set; }
        public double Charge_Ex_Rate { get; set; }
        public string VAT_Code { get; set; }
        public int AP_Invoice_No {  get; set; }
        public DateTime AP_Invoice_Date { get; set; }
        public DateTime AP_Invoice_Due_Date { get; set; }
        public string AP_Charge_Currency { get; set; }
        public double AP_Charge_Ex_Rate { get; set; }
        public decimal Charge_Act_Cost_VAT_OS_Amount { get; set; }
        public decimal Charge_Act_Cost_Net_OS_Amount { get; set; }
        public decimal Charge_Act_Cost_Gross_OS_Amount { get; set; }
        public decimal Charge_Act_Cost_VAT_Amount { get; set; }
        public decimal Charge_Act_Cost_Net_Amount { get; set; }
        public decimal Charge_Act_Cost_Gross_Amount { get; set; }
        public decimal AP_Invoice_Net_Total_Amount { get; set; }
        public decimal AP_Invoice_VAT_Total_Amount { get; set; }
        public decimal AP_Invoice_Gross_Total_Amount { get; set; }
        public bool AP_Invoice_Audit_Status { get; set; }
        public DateTime AP_Invoice_Audit_Date { get; set; }

        public ActualShipmentCharge()
        {
            Shipment_Job_No = "";
            Charge_Code = "";
            Charge_Name = "";
            Charge_Ex_Rate = 1.00;
            Charge_Currency = "";
            VAT_Code = "FREESVC";
            AP_Invoice_No = 0;
            AP_Invoice_Date = new();
            AP_Invoice_Due_Date = new();
            AP_Charge_Currency = "";
            AP_Charge_Ex_Rate = 1.00;
            Charge_Act_Cost_VAT_OS_Amount = 0;
            Charge_Act_Cost_Net_OS_Amount = 0;
            Charge_Act_Cost_Gross_OS_Amount = 0;
            Charge_Act_Cost_VAT_Amount = 0;
            Charge_Act_Cost_Net_Amount = 0;
            Charge_Act_Cost_Gross_Amount = 0;
            AP_Invoice_Net_Total_Amount = 0;
            AP_Invoice_VAT_Total_Amount = 0;
            AP_Invoice_Gross_Total_Amount = 0;
            AP_Invoice_Audit_Status = false;
            AP_Invoice_Audit_Date = new();
        }
    }
}
