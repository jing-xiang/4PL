namespace _4PL.Data;

public class ChargeReference
{
    public string Charge_Code { get; set; } // "required" property or "= string.Empty" 
    public string Charge_Description { get; set; }

    public ChargeReference(string charge_Code, string charge_Description)
    {
        this.Charge_Code = charge_Code;
        this.Charge_Description = charge_Description;
    }

    // Default constructor without parameters
    public ChargeReference()
    {
        this.Charge_Code = "";
        this.Charge_Description = "";
    }

    public ChargeReference(ChargeReference charge)
    {
        this.Charge_Code = charge.Charge_Code;
        this.Charge_Description = charge.Charge_Description; 
    }
}
