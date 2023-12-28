namespace _4PL.Data;

public class ChargeReference
{
    public string Charge_Code { get; set; } // "required" property or "= string.Empty" 
    public string Charge_Description { get; set; }

    public ChargeReference(string charge_Code, string charge_Description)
    {
        Charge_Code = charge_Code;
        Charge_Description = charge_Description;
    }
}
