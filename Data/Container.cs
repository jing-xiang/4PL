namespace _4PL.Data
{
    public class Container
    {
        public string Id { get; set; }
        public string Shipment_Job_No { get; set; }
        public string Container_No { get; set; }
        public string Container_Type { get; set; }
        public string Seal_No_1 { get; set; }
        public string Seal_No_2 { get; set; }

        public Container(string Id, string Shipment_Job_No, string Container_No, string Container_Type, string Seal_No_1, string Seal_No_2)
        {
            this.Id = Id;
            this.Shipment_Job_No = Shipment_Job_No;
            this.Container_No = Container_No;
            this.Container_Type = Container_Type;
            this.Seal_No_1 = Seal_No_1;
            this.Seal_No_2 = Seal_No_2;
        }

        public Container()
        {
            this.Id = "";
            this.Shipment_Job_No = "";
            this.Container_No = "";
            this.Container_Type = "";
            this.Seal_No_1 = "";
            this.Seal_No_2 = "";
        }

        public Container(Container cont)
        {
            this.Id = cont.Id;
            this.Shipment_Job_No = cont.Shipment_Job_No;
            this.Container_No = cont.Container_No;
            this.Container_Type = cont.Container_Type;
            this.Seal_No_1 = cont.Seal_No_1;
            this.Seal_No_2 = cont.Seal_No_2;
        }

    }
}
