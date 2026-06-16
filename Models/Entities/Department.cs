namespace HospitalSystem.Models.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-stethoscope";
}
