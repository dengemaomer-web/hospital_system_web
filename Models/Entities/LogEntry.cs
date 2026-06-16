namespace HospitalSystem.Models.Entities;

public class LogEntry : BaseEntity
{
    public Enums.LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Exception { get; set; }
}
