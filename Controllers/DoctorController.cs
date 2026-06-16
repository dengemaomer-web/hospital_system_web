using HospitalSystem.Helpers;
using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;
using HospitalSystem.Models.ViewModels;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Controllers;

[Authorize(Roles = nameof(UserRole.Doctor))]
public class DoctorController : BaseController
{
    private readonly IRepository<Doctor> _doctors;
    private readonly IRepository<Patient> _patients;
    private readonly IRepository<Department> _departments;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly IRepository<Schedule> _schedules;
    private readonly IAppointmentService _appointments;
    private readonly IMedicineSuggestionService _suggestions;
    private readonly INotificationService _notifications;

    public DoctorController(
        IRepository<Doctor> doctors,
        IRepository<Patient> patients,
        IRepository<Department> departments,
        IRepository<Prescription> prescriptions,
        IRepository<Schedule> schedules,
        IAppointmentService appointments,
        IMedicineSuggestionService suggestions,
        INotificationService notifications)
    {
        _doctors = doctors;
        _patients = patients;
        _departments = departments;
        _prescriptions = prescriptions;
        _schedules = schedules;
        _appointments = appointments;
        _suggestions = suggestions;
        _notifications = notifications;
    }

    private Guid DoctorId => User.GetEntityId();

    public async Task<IActionResult> Index()
    {
        var doctor = await _doctors.GetByIdAsync(DoctorId);
        if (doctor == null) return Forbid();

        var appointments = await _appointments.GetByDoctorAsync(DoctorId);
        var patients = await _patients.GetAllAsync();
        var departments = await _departments.GetAllAsync();
        var today = DateTime.Today;

        var todayAppointments = appointments
            .Where(a => a.AppointmentDate.Date == today && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.TimeSlot)
            .Select(a => Map(a, patients, doctor, departments))
            .ToList();

        var vm = new DoctorDashboardViewModel
        {
            Doctor = doctor,
            TodayAppointments = todayAppointments,
            Stats = new List<StatCard>
            {
                new() { Title = "Bugünkü Randevular", Value = todayAppointments.Count.ToString(), Icon = "fa-calendar-day", Color = "primary" },
                new() { Title = "Bekleyen Hastalar", Value = appointments.Count(a => a.Status == AppointmentStatus.Pending).ToString(), Icon = "fa-hourglass-half", Color = "warning" },
                new() { Title = "Tamamlanan Muayene", Value = appointments.Count(a => a.Status == AppointmentStatus.Completed).ToString(), Icon = "fa-clipboard-check", Color = "success" },
                new() { Title = "Toplam Hasta", Value = appointments.Select(a => a.PatientId).Distinct().Count().ToString(), Icon = "fa-hospital-user", Color = "accent" }
            }
        };
        return View(vm);
    }

    public async Task<IActionResult> Appointments(DateTime? date)
    {
        var doctor = await _doctors.GetByIdAsync(DoctorId);
        var selectedDate = date ?? DateTime.Today;
        var appointments = await _appointments.GetByDoctorAsync(DoctorId);
        var patients = await _patients.GetAllAsync();
        var departments = await _departments.GetAllAsync();

        var list = appointments
            .Where(a => a.AppointmentDate.Date == selectedDate.Date)
            .OrderBy(a => a.TimeSlot)
            .Select(a => Map(a, patients, doctor!, departments))
            .ToList();

        ViewBag.SelectedDate = selectedDate;
        return View(list);
    }

    public async Task<IActionResult> Patients()
    {
        var appointments = await _appointments.GetByDoctorAsync(DoctorId);
        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToHashSet();
        var patients = await _patients.FindAsync(p => patientIds.Contains(p.Id));
        return View(patients);
    }

    public async Task<IActionResult> PatientHistory(Guid id)
    {
        var patient = await _patients.GetByIdAsync(id);
        if (patient == null) return NotFound();

        var appointments = await _appointments.GetByDoctorAsync(DoctorId);
        var doctor = await _doctors.GetByIdAsync(DoctorId);
        var departments = await _departments.GetAllAsync();
        var prescriptions = await _prescriptions.FindAsync(p => p.PatientId == id);

        ViewBag.Patient = patient;
        ViewBag.Appointments = appointments
            .Where(a => a.PatientId == id)
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => Map(a, new List<Patient> { patient }, doctor!, departments))
            .ToList();
        ViewBag.Prescriptions = prescriptions.OrderByDescending(p => p.Date).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteAppointment(Guid id)
    {
        var appointment = await _appointments.GetByIdAsync(id);
        if (appointment?.DoctorId != DoctorId) return Forbid();
        await _appointments.UpdateStatusAsync(id, AppointmentStatus.Completed);
        TempData["Success"] = "Muayene tamamlandı olarak işaretlendi.";
        return RedirectToAction(nameof(Appointments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAppointment(Guid id)
    {
        var appointment = await _appointments.GetByIdAsync(id);
        if (appointment?.DoctorId != DoctorId) return Forbid();
        await _appointments.UpdateStatusAsync(id, AppointmentStatus.Confirmed);
        TempData["Success"] = "Randevu onaylandı.";
        return RedirectToAction(nameof(Appointments));
    }

    [HttpGet]
    public async Task<IActionResult> WritePrescription(Guid appointmentId)
    {
        var appointment = await _appointments.GetByIdAsync(appointmentId);
        if (appointment?.DoctorId != DoctorId) return Forbid();
        var patient = await _patients.GetByIdAsync(appointment.PatientId);

        var vm = new PrescriptionFormViewModel
        {
            AppointmentId = appointmentId,
            PatientId = appointment.PatientId,
            PatientName = patient?.FullName ?? "-",
            Diagnoses = await _suggestions.GetDiagnosesAsync(),
            Items = new List<PrescriptionItem> { new() }
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WritePrescription(PrescriptionFormViewModel model)
    {
        var appointment = await _appointments.GetByIdAsync(model.AppointmentId);
        if (appointment?.DoctorId != DoctorId) return Forbid();

        var items = (model.Items ?? new List<PrescriptionItem>())
            .Where(i => !string.IsNullOrWhiteSpace(i.MedicineName))
            .ToList();

        if (items.Count == 0)
        {
            TempData["Error"] = "En az bir ilaç eklemelisiniz.";
            return RedirectToAction(nameof(WritePrescription), new { appointmentId = model.AppointmentId });
        }

        await _prescriptions.AddAsync(new Prescription
        {
            AppointmentId = model.AppointmentId,
            PatientId = model.PatientId,
            DoctorId = DoctorId,
            DiagnosisName = model.DiagnosisName,
            Notes = model.Notes,
            Items = items,
            Date = DateTime.UtcNow
        });

        await _appointments.UpdateStatusAsync(model.AppointmentId, AppointmentStatus.Completed);

        var patient = await _patients.GetByIdAsync(model.PatientId);
        if (patient != null)
            await _notifications.CreateAsync(patient.UserId, "Reçete Oluşturuldu",
                "Doktorunuz size yeni bir reçete yazdı.", NotificationType.PrescriptionCreated);

        TempData["Success"] = "Reçete oluşturuldu.";
        return RedirectToAction(nameof(Appointments));
    }

    [HttpGet]
    public async Task<IActionResult> SuggestMedicines(string diagnosis)
    {
        var meds = await _suggestions.SuggestAsync(diagnosis);
        return Json(meds);
    }

    public async Task<IActionResult> Diagnoses()
    {
        var diagnoses = await _suggestions.GetDiagnosesAsync();
        return View(diagnoses);
    }

    public async Task<IActionResult> Schedule()
    {
        var schedules = await _schedules.FindAsync(s => s.DoctorId == DoctorId);
        return View(schedules.OrderBy(s => s.DayOfWeek).ToList());
    }

    public async Task<IActionResult> Notifications()
    {
        var items = await _notifications.GetForUserAsync(User.GetUserId());
        return View(items);
    }

    public async Task<IActionResult> Profile()
    {
        var doctor = await _doctors.GetByIdAsync(DoctorId);
        ViewBag.Department = doctor != null ? await _departments.GetByIdAsync(doctor.DepartmentId) : null;
        return View(doctor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(Doctor model)
    {
        var doctor = await _doctors.GetByIdAsync(DoctorId);
        if (doctor == null) return Forbid();
        doctor.Phone = model.Phone;
        doctor.Email = model.Email;
        doctor.Bio = model.Bio;
        doctor.Title = model.Title;
        await _doctors.UpdateAsync(doctor);
        TempData["Success"] = "Profil güncellendi.";
        return RedirectToAction(nameof(Profile));
    }

    private static AppointmentViewModel Map(Appointment a, List<Patient> patients, Doctor doctor, List<Department> departments)
        => new()
        {
            Id = a.Id,
            PatientId = a.PatientId,
            DoctorId = a.DoctorId,
            PatientName = patients.FirstOrDefault(p => p.Id == a.PatientId)?.FullName ?? "-",
            DoctorName = doctor.FullName,
            DepartmentName = departments.FirstOrDefault(d => d.Id == a.DepartmentId)?.Name ?? "-",
            Date = a.AppointmentDate,
            TimeSlot = a.TimeSlot,
            Status = a.Status,
            Notes = a.Notes
        };
}
