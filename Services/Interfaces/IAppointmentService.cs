using HospitalSystem.Models.Entities;

namespace HospitalSystem.Services.Interfaces;

public interface IAppointmentService
{
    Task<List<string>> GetAvailableSlotsAsync(Guid doctorId, DateTime date);
    Task<Appointment> BookAsync(Guid patientId, Guid doctorId, Guid departmentId, DateTime date, string slot, string notes);
    Task<Appointment?> GetByIdAsync(Guid id);
    Task<List<Appointment>> GetByPatientAsync(Guid patientId);
    Task<List<Appointment>> GetByDoctorAsync(Guid doctorId);
    Task<List<Appointment>> GetAllAsync();
    Task CancelAsync(Guid appointmentId);
    Task UpdateStatusAsync(Guid appointmentId, Models.Enums.AppointmentStatus status);
}
