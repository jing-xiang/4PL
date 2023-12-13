//using _4PL.Data;
using System.ComponentModel.DataAnnotations;

namespace _4PL.Data
{
    public class RateCard
    {
        // To replace with UUID from database once connected
        [Key]
        public Guid Id { get; set; }
        public string Lane_ID { get; set; }
        public string Controlling_Customer_Matchcode { get; set; }
        public string Controlling_Customer_Name { get; set; }
        public string Transport_Mode { get; set; }
        public string Function { get; set; }
        public DateTime Rate_Validity_From { get; set; }
        public DateTime Rate_Validity_To { get; set; }
        public string POL_Name { get; set; }
        public string POL_Country { get; set; }
        public string POL_Port { get; set; }
        public string POD_Name { get; set; }
        public string POD_Country { get; set; }
        public string POD_Port { get; set; }
        public string Creditor_Matchcode { get; set; }
        public string Creditor_Name { get; set; }
        public string Pickup_Address { get; set; }
        public string Delivery_Address { get; set; }
        public string Dangerous_Goods { get; set; }
        public string Temperature_Controlled {  get; set; }
        public string Container_Mode { get; set; }
        public string Container_Type { get; set;}
        public string Local_Currency { get; set;}
        public List<Charge> Charges { get; set; }

        public RateCard(string Id, string lane_ID, string controlling_Customer_Matchcode, string controlling_Customer_Name, string transport_Mode, string function, DateTime rate_Validity_From, DateTime rate_Validity_To, string pOL_Name, string pOL_Country, string pOL_Port, string pOD_Name, string pOD_Country, string pOD_Port, string creditor_Matchcode, string creditor_Name, string pickup_Address, string delivery_Address, string dangerous_Goods, string temperature_Controlled, string container_Mode, string container_Type, string local_Currency, List<Charge> charges)
        {
            this.Id = Guid.Parse(Id);
            this.Lane_ID = lane_ID;
            this.Controlling_Customer_Matchcode = controlling_Customer_Matchcode;
            this.Controlling_Customer_Name = controlling_Customer_Name;
            this.Transport_Mode = transport_Mode;
            this.Function = function;
            this.Rate_Validity_From = rate_Validity_From;
            this.Rate_Validity_To = rate_Validity_To;
            this.POL_Name = pOL_Name;
            this.POL_Country = pOL_Country;
            this.POL_Port = pOL_Port;
            this.POD_Name = pOD_Name;
            this.POD_Country = pOD_Country;
            this.POD_Port = pOD_Port;
            this.Creditor_Matchcode = creditor_Matchcode;
            this.Creditor_Name = creditor_Name;
            this.Pickup_Address = pickup_Address;
            this.Delivery_Address = delivery_Address;
            this.Dangerous_Goods = dangerous_Goods;
            this.Temperature_Controlled = temperature_Controlled;
            this.Container_Mode = container_Mode;
            this.Container_Type = container_Type;
            this.Local_Currency = local_Currency;
            this.Charges = charges;
        }

        public RateCard()
        {
            Id = Guid.NewGuid();
            this.Lane_ID = "empty";
            this.Controlling_Customer_Matchcode = "empty";
            this.Controlling_Customer_Name = "empty";
            this.Transport_Mode = "empty";
            this.Function = "empty";
            this.Rate_Validity_From = DateTime.Now;
            this.Rate_Validity_To = DateTime.Now;
            this.POL_Name = "empty";
            this.POL_Country = "empty";
            this.POL_Port = "empty";
            this.POD_Name = "empty";
            this.POD_Country = "empty";
            this.POD_Port = "empty";
            this.Creditor_Matchcode = "empty";
            this.Creditor_Name = "empty";
            this.Pickup_Address = "empty";
            this.Delivery_Address = "empty";
            this.Dangerous_Goods = "empty";
            this.Temperature_Controlled = "empty";
            this.Container_Mode = "empty";
            this.Container_Type = "empty";
            this.Local_Currency = "empty";
            this.Charges = new List<Charge>();
        }
    }
}
