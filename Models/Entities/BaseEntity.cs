namespace HospitalSystem.Models.Entities;

/// <summary>
/// Base type for all JSON-persisted entities. Relations are established through GUID ids.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
