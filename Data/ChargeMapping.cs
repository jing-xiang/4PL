using _4PL.Components.Pages;
using Amazon.Auth.AccessControlPolicy;

namespace _4PL.Data;

public class ChargeMapping
{
    public string Other_Charge_Description_Name { get; set; }
    public string Source {  get; set; }
    public string Charge_Description { get; set; }

    public ChargeMapping(string other_Charge_Description_Name, string source, string charge_Description)
    {
        this.Other_Charge_Description_Name = other_Charge_Description_Name;
        this.Source = source;
        this.Charge_Description = charge_Description;
    }

    public ChargeMapping()
    {
        this.Other_Charge_Description_Name = "";
        this.Source = "";
        this.Charge_Description = "";
    }

    public ChargeMapping(ChargeMapping mapping)
    {
        this.Other_Charge_Description_Name = mapping.Other_Charge_Description_Name;
        this.Source = mapping.Source;
        this.Charge_Description = mapping.Charge_Description;
    }


}
