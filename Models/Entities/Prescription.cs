namespace HospitalSystem.Models.Entities;

public class PrescriptionItem
{
    public string MedicineName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
}

public class Prescription : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string DiagnosisName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public List<PrescriptionItem> Items { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}
