using _4PL.Components.Pages;
using Amazon.Auth.AccessControlPolicy;

namespace _4PL.Data;

public class ChargeMappingReference
{
    public Guid Id { get; set; }
    public string Other_Charge_Description_Name { get; set; }
    public string Source {  get; set; }
    public string Charge_Description { get; set; }

    public ChargeMappingReference(string id, string other_Charge_Description_Name, string source, string charge_Description)
    {
        this.Id = Guid.Parse(id);
        this.Other_Charge_Description_Name = other_Charge_Description_Name;
        this.Source = source;
        this.Charge_Description = charge_Description;
    }

    public ChargeMappingReference()
    {
        this.Id = Guid.NewGuid(); 
        this.Other_Charge_Description_Name = "";
        this.Source = "";
        this.Charge_Description = "";
    }

    public ChargeMappingReference(ChargeMappingReference mapping)
    {
        this.Id = mapping.Id; 
        this.Other_Charge_Description_Name = mapping.Other_Charge_Description_Name;
        this.Source = mapping.Source;
        this.Charge_Description = mapping.Charge_Description;
    }


}
