using HospitalSystem.Helpers;
using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;
using HospitalSystem.Models.ViewModels;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Controllers;

[Authorize(Roles = nameof(UserRole.Patient))]
public class PatientController : BaseController
{
    private readonly IRepository<Patient> _patients;
    private readonly IRepository<Doctor> _doctors;
    private readonly IRepository<Department> _departments;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly IAppointmentService _appointments;
    private readonly INotificationService _notifications;

    public PatientController(
        IRepository<Patient> patients,
        IRepository<Doctor> doctors,
        IRepository<Department> departments,
        IRepository<Prescription> prescriptions,
        IAppointmentService appointments,
        INotificationService notifications)
    {
        _patients = patients;
        _doctors = doctors;
        _departments = departments;
        _prescriptions = prescriptions;
        _appointments = appointments;
        _notifications = notifications;
    }

    private Guid PatientId => User.GetEntityId();

    public async Task<IActionResult> Index()
    {
        var patient = await _patients.GetByIdAsync(PatientId);
        if (patient == null) return Forbid();

        var appointments = await _appointments.GetByPatientAsync(PatientId);
        var doctors = await _doctors.GetAllAsync();
        var departments = await _departments.GetAllAsync();
        var prescriptions = await _prescriptions.FindAsync(p => p.PatientId == PatientId);

        var upcoming = appointments
            .Where(a => a.AppointmentDate.Date >= DateTime.Today && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.AppointmentDate)
            .Take(5)
            .Select(a => Map(a, doctors, departments, patient))
            .ToList();

        var vm = new PatientDashboardViewModel
        {
            Patient = patient,
            UpcomingAppointments = upcoming,
            ActivePrescriptions = prescriptions.OrderByDescending(p => p.Date).Take(5).ToList(),
            Stats = new List<StatCard>
            {
                new() { Title = "Yaklaşan Randevu", Value = upcoming.Count.ToString(), Icon = "fa-calendar-day", Color = "primary" },
                new() { Title = "Toplam Randevu", Value = appointments.Count.ToString(), Icon = "fa-calendar-check", Color = "accent" },
                new() { Title = "Geçmiş Randevu", Value = appointments.Count(a => a.Status == AppointmentStatus.Completed).ToString(), Icon = "fa-clock-rotate-left", Color = "success" },
                new() { Title = "Aktif Reçete", Value = prescriptions.Count.ToString(), Icon = "fa-prescription", Color = "warning" }
            }
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Book(Guid? doctorId)
    {
        var vm = new BookAppointmentViewModel
        {
            Departments = await _departments.GetAllAsync(),
            Doctors = await _doctors.GetAllAsync(),
            SelectedDoctorId = doctorId
        };
        if (doctorId.HasValue)
            vm.SelectedDepartmentId = (await _doctors.GetByIdAsync(doctorId.Value))?.DepartmentId;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookAppointmentViewModel model)
    {
        if (model.SelectedDoctorId == null || model.SelectedDepartmentId == null)
        {
            TempData["Error"] = "Lütfen poliklinik ve doktor seçiniz.";
            return RedirectToAction(nameof(Book));
        }

        try
        {
            await _appointments.BookAsync(PatientId, model.SelectedDoctorId.Value,
                model.SelectedDepartmentId.Value, model.SelectedDate, model.SelectedSlot ?? string.Empty, model.Notes);
            TempData["Success"] = "Randevunuz başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Appointments));
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Book), new { doctorId = model.SelectedDoctorId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Slots(Guid doctorId, DateTime date)
    {
        var slots = await _appointments.GetAvailableSlotsAsync(doctorId, date);
        return Json(slots);
    }

    [HttpGet]
    public async Task<IActionResult> DoctorsByDepartment(Guid departmentId)
    {
        var doctors = await _doctors.FindAsync(d => d.DepartmentId == departmentId);
        return Json(doctors.Select(d => new { id = d.Id, name = $"{d.Title} {d.FullName}" }));
    }

    public async Task<IActionResult> Appointments()
    {
        var patient = await _patients.GetByIdAsync(PatientId);
        var appointments = await _appointments.GetByPatientAsync(PatientId);
        var doctors = await _doctors.GetAllAsync();
        var departments = await _departments.GetAllAsync();
        var vm = appointments.Select(a => Map(a, doctors, departments, patient!)).ToList();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var appointment = await _appointments.GetByIdAsync(id);
        if (appointment?.PatientId != PatientId)
            return Forbid();
        await _appointments.CancelAsync(id);
        TempData["Success"] = "Randevu iptal edildi.";
        return RedirectToAction(nameof(Appointments));
    }

    public async Task<IActionResult> Prescriptions()
    {
        var prescriptions = await _prescriptions.FindAsync(p => p.PatientId == PatientId);
        var doctors = await _doctors.GetAllAsync();
        ViewBag.Doctors = doctors;
        return View(prescriptions.OrderByDescending(p => p.Date).ToList());
    }

    public IActionResult LabResults() => View();

    public async Task<IActionResult> FavoriteDoctors()
    {
        var patient = await _patients.GetByIdAsync(PatientId);
        var doctors = await _doctors.GetAllAsync();
        var departments = await _departments.GetAllAsync();
        ViewBag.Departments = departments;
        ViewBag.FavoriteIds = patient?.FavoriteDoctorIds ?? new List<Guid>();
        return View(doctors);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(Guid doctorId)
    {
        var patient = await _patients.GetByIdAsync(PatientId);
        if (patient == null) return Forbid();
        if (!patient.FavoriteDoctorIds.Remove(doctorId))
            patient.FavoriteDoctorIds.Add(doctorId);
        await _patients.UpdateAsync(patient);
        return RedirectToAction(nameof(FavoriteDoctors));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RateDoctor(Guid doctorId, int rating)
    {
        var doctor = await _doctors.GetByIdAsync(doctorId);
        if (doctor != null && rating is >= 1 and <= 5)
        {
            var total = doctor.Rating * doctor.RatingCount + rating;
            doctor.RatingCount += 1;
            doctor.Rating = Math.Round(total / doctor.RatingCount, 1);
            await _doctors.UpdateAsync(doctor);
            TempData["Success"] = "Değerlendirmeniz kaydedildi.";
        }
        return RedirectToAction(nameof(FavoriteDoctors));
    }

    public async Task<IActionResult> Notifications()
    {
        var items = await _notifications.GetForUserAsync(User.GetUserId());
        return View(items);
    }

    public async Task<IActionResult> Profile()
    {
        var patient = await _patients.GetByIdAsync(PatientId);
        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(Patient model)
    {
        var patient = await _patients.GetByIdAsync(PatientId);
        if (patient == null) return Forbid();
        patient.Phone = model.Phone;
        patient.Email = model.Email;
        patient.Address = model.Address;
        patient.BloodType = model.BloodType;
        patient.BirthDate = model.BirthDate;
        patient.Gender = model.Gender;
        await _patients.UpdateAsync(patient);
        TempData["Success"] = "Profil güncellendi.";
        return RedirectToAction(nameof(Profile));
    }

    private static AppointmentViewModel Map(Appointment a, List<Doctor> doctors, List<Department> departments, Patient patient)
        => new()
        {
            Id = a.Id,
            PatientId = a.PatientId,
            DoctorId = a.DoctorId,
            PatientName = patient.FullName,
            DoctorName = doctors.FirstOrDefault(d => d.Id == a.DoctorId)?.FullName ?? "-",
            DepartmentName = departments.FirstOrDefault(d => d.Id == a.DepartmentId)?.Name ?? "-",
            Date = a.AppointmentDate,
            TimeSlot = a.TimeSlot,
            Status = a.Status,
            Notes = a.Notes
        };
}
