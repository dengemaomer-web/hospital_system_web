using System.Globalization;
using HospitalSystem.Helpers;
using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;

namespace HospitalSystem.Services.Implementations;

public class AppointmentService : IAppointmentService
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<Schedule> _schedules;
    private readonly IRepository<Doctor> _doctors;
    private readonly IRepository<Patient> _patients;
    private readonly INotificationService _notifications;
    private readonly IAppLogger _logger;

    public AppointmentService(
        IRepository<Appointment> appointments,
        IRepository<Schedule> schedules,
        IRepository<Doctor> doctors,
        IRepository<Patient> patients,
        INotificationService notifications,
        IAppLogger logger)
    {
        _appointments = appointments;
        _schedules = schedules;
        _doctors = doctors;
        _patients = patients;
        _notifications = notifications;
        _logger = logger;
    }

    /// <summary>
    /// Generates all slots from the doctor's schedule for the given weekday and removes
    /// slots that are already booked (Pending/Confirmed) so callers can render busy slots as passive.
    /// </summary>
    public async Task<List<string>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
    {
        var schedule = await _schedules.FirstOrDefaultAsync(s =>
            s.DoctorId == doctorId && s.DayOfWeek == date.DayOfWeek && s.IsActive);

        if (schedule == null)
            return new List<string>();

        var slots = BuildSlots(schedule);

        var booked = await _appointments.FindAsync(a =>
            a.DoctorId == doctorId &&
            a.AppointmentDate.Date == date.Date &&
            a.Status != AppointmentStatus.Cancelled);

        var bookedSlots = booked.Select(b => b.TimeSlot).ToHashSet();

        // Respect daily capacity: if reached, no slots are free.
        if (booked.Count >= schedule.DailyCapacity)
            return new List<string>();

        return slots.Where(s => !bookedSlots.Contains(s)).ToList();
    }

    private static List<string> BuildSlots(Schedule schedule)
    {
        var slots = new List<string>();
        var start = TimeSpan.Parse(schedule.StartTime, CultureInfo.InvariantCulture);
        var end = TimeSpan.Parse(schedule.EndTime, CultureInfo.InvariantCulture);
        var step = TimeSpan.FromMinutes(schedule.SlotMinutes);

        for (var t = start; t + step <= end; t += step)
            slots.Add(t.ToString(@"hh\:mm"));

        return slots;
    }

    public async Task<Appointment> BookAsync(Guid patientId, Guid doctorId, Guid departmentId, DateTime date, string slot, string notes)
    {
        if (string.IsNullOrWhiteSpace(slot))
            throw new ValidationException("Lütfen bir saat seçiniz.");

        // Past date check.
        var slotTime = TimeSpan.Parse(slot, CultureInfo.InvariantCulture);
        var appointmentMoment = date.Date + slotTime;
        if (appointmentMoment < DateTime.Now)
            throw new ValidationException("Geçmiş bir tarih veya saat seçilemez.");

        var schedule = await _schedules.FirstOrDefaultAsync(s =>
            s.DoctorId == doctorId && s.DayOfWeek == date.DayOfWeek && s.IsActive);
        if (schedule == null)
            throw new ValidationException("Seçilen doktorun bu gün için çalışma takvimi bulunmuyor.");

        // Conflict check: same doctor, same date, same slot, not cancelled.
        var conflict = await _appointments.FirstOrDefaultAsync(a =>
            a.DoctorId == doctorId &&
            a.AppointmentDate.Date == date.Date &&
            a.TimeSlot == slot &&
            a.Status != AppointmentStatus.Cancelled);
        if (conflict != null)
            throw new ValidationException("Seçilen saat dolu. Lütfen başka bir saat seçiniz.");

        // Daily capacity check.
        var dailyCount = await _appointments.CountAsync(a =>
            a.DoctorId == doctorId &&
            a.AppointmentDate.Date == date.Date &&
            a.Status != AppointmentStatus.Cancelled);
        if (dailyCount >= schedule.DailyCapacity)
            throw new ValidationException("Doktorun günlük randevu kapasitesi dolmuştur.");

        var appointment = await _appointments.AddAsync(new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            DepartmentId = departmentId,
            AppointmentDate = date.Date,
            TimeSlot = slot,
            Status = AppointmentStatus.Pending,
            Notes = notes
        });

        var doctor = await _doctors.GetByIdAsync(doctorId);
        var patient = await _patients.GetByIdAsync(patientId);

        if (patient != null)
            await _notifications.CreateAsync(patient.UserId,
                "Randevu Oluşturuldu",
                $"{date:dd.MM.yyyy} {slot} - {doctor?.FullName} randevunuz oluşturuldu.",
                NotificationType.NewAppointment);

        if (doctor != null)
            await _notifications.CreateAsync(doctor.UserId,
                "Yeni Randevu",
                $"{patient?.FullName} hastası {date:dd.MM.yyyy} {slot} için randevu aldı.",
                NotificationType.NewAppointment);

        await _logger.InfoAsync($"Randevu oluşturuldu: {appointment.Id}", "Appointment");
        return appointment;
    }

    public Task<Appointment?> GetByIdAsync(Guid id) => _appointments.GetByIdAsync(id);

    public async Task<List<Appointment>> GetByPatientAsync(Guid patientId)
    {
        var items = await _appointments.FindAsync(a => a.PatientId == patientId);
        return items.OrderByDescending(a => a.AppointmentDate).ToList();
    }

    public async Task<List<Appointment>> GetByDoctorAsync(Guid doctorId)
    {
        var items = await _appointments.FindAsync(a => a.DoctorId == doctorId);
        return items.OrderByDescending(a => a.AppointmentDate).ToList();
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        var items = await _appointments.GetAllAsync();
        return items.OrderByDescending(a => a.AppointmentDate).ToList();
    }

    public async Task CancelAsync(Guid appointmentId)
    {
        var appointment = await _appointments.GetByIdAsync(appointmentId)
            ?? throw new AppException("Randevu bulunamadı.");
        appointment.Status = AppointmentStatus.Cancelled;
        await _appointments.UpdateAsync(appointment);

        var doctor = await _doctors.GetByIdAsync(appointment.DoctorId);
        var patient = await _patients.GetByIdAsync(appointment.PatientId);
        if (patient != null)
            await _notifications.CreateAsync(patient.UserId, "Randevu İptal Edildi",
                $"{appointment.AppointmentDate:dd.MM.yyyy} {appointment.TimeSlot} randevunuz iptal edildi.",
                NotificationType.CancelledAppointment);
        if (doctor != null)
            await _notifications.CreateAsync(doctor.UserId, "Randevu İptal Edildi",
                $"{patient?.FullName} hastasının {appointment.AppointmentDate:dd.MM.yyyy} randevusu iptal edildi.",
                NotificationType.CancelledAppointment);

        await _logger.InfoAsync($"Randevu iptal edildi: {appointmentId}", "Appointment");
    }

    public async Task UpdateStatusAsync(Guid appointmentId, AppointmentStatus status)
    {
        var appointment = await _appointments.GetByIdAsync(appointmentId)
            ?? throw new AppException("Randevu bulunamadı.");
        appointment.Status = status;
        await _appointments.UpdateAsync(appointment);
        await _logger.InfoAsync($"Randevu durumu güncellendi: {appointmentId} -> {status}", "Appointment");
    }
}
