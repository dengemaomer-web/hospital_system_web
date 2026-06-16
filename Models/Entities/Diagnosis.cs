namespace HospitalSystem.Models.Entities;

public class Diagnosis : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>Medicine names recommended by the smart suggestion system for this diagnosis.</summary>
    public List<string> RecommendedMedicines { get; set; } = new();
}
