namespace HospitalSystem.Models.Entities;

/// <summary>
/// Working schedule for a doctor on a given day of the week.
/// </summary>
public class Schedule : BaseEntity
{
    public Guid DoctorId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = "09:00";
    public string EndTime { get; set; } = "17:00";
    public int SlotMinutes { get; set; } = 30;
    public int DailyCapacity { get; set; } = 16;
    public bool IsActive { get; set; } = true;
}
