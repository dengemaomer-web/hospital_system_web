using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;

namespace HospitalSystem.Models.ViewModels;

public class AppointmentViewModel
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
}

public class BookAppointmentViewModel
{
    public List<Department> Departments { get; set; } = new();
    public List<Doctor> Doctors { get; set; } = new();
    public Guid? SelectedDepartmentId { get; set; }
    public Guid? SelectedDoctorId { get; set; }
    public DateTime SelectedDate { get; set; } = DateTime.Today.AddDays(1);
    public string? SelectedSlot { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class PrescriptionFormViewModel
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public List<Diagnosis> Diagnoses { get; set; } = new();
    public string DiagnosisName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<PrescriptionItem> Items { get; set; } = new();
}
