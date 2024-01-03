using _4PL.Components.Pages;
using Amazon.Auth.AccessControlPolicy;

namespace _4PL.Data;

public class ContainerTypeMapping
{
    public string Other_Container_Type_Name { get; set; }
    public string Source {  get; set; }
    public string Container_Type {  get; set; }

    public ContainerTypeMapping(string other_Container_Type_Name, string source, string container_Type)
    {
        this.Other_Container_Type_Name = other_Container_Type_Name;
        this.Source = source;
        this.Container_Type = container_Type;
    }

    public ContainerTypeMapping()
    {
        this.Other_Container_Type_Name = "";
        this.Source = "";
        this.Container_Type = "";
    }

    public ContainerTypeMapping(ContainerTypeMapping mapping)
    {
        this.Other_Container_Type_Name = mapping.Other_Container_Type_Name;
        this.Source = mapping.Source;
        this.Container_Type = mapping.Container_Type;
    }


}
