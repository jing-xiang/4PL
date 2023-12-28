using System.ComponentModel.DataAnnotations;

namespace _4PL.Data;

public class ContainerTypeReference

{
    [Key] 
    public string Container_Type { get; set; }

    public ContainerTypeReference(string container_Type)
    {
        this.Container_Type = container_Type;
    }

    // Default constructor without parameters
    public ContainerTypeReference()
    {
        this.Container_Type = ""; 
    }


}
