using HospitalSystem.Models.Enums;

namespace HospitalSystem.Models.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid DepartmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string Notes { get; set; } = string.Empty;
}
