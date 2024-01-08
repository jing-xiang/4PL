using _4PL.Components.Pages;
using Amazon.Auth.AccessControlPolicy;
using System.ComponentModel.DataAnnotations;

namespace _4PL.Data;

public class ContainerTypeMappingReference
{
    [Key]
    public Guid Id { get; set; }
    public string Other_Container_Type_Name { get; set; }
    public string Source {  get; set; }
    public string Container_Type {  get; set; }

    public ContainerTypeMappingReference(string id, string other_Container_Type_Name, string source, string container_Type)
    {
        this.Id = Guid.Parse(id);
        this.Other_Container_Type_Name = other_Container_Type_Name;
        this.Source = source;
        this.Container_Type = container_Type;
    }

    public ContainerTypeMappingReference()
    {
        this.Id = Guid.NewGuid();
        this.Other_Container_Type_Name = "";
        this.Source = "";
        this.Container_Type = "";
    }

    public ContainerTypeMappingReference(ContainerTypeMappingReference mapping)
    {
        this.Id = mapping.Id; 
        this.Other_Container_Type_Name = mapping.Other_Container_Type_Name;
        this.Source = mapping.Source;
        this.Container_Type = mapping.Container_Type;
    }


}
