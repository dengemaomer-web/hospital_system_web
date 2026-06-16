using System.IO.Compression;
using System.Text;
using HospitalSystem.Helpers;
using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;
using HospitalSystem.Models.ViewModels;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Controllers;

[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : BaseController
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Doctor> _doctors;
    private readonly IRepository<Patient> _patients;
    private readonly IRepository<Department> _departments;
    private readonly IRepository<Schedule> _schedules;
    private readonly IStatisticsService _statistics;
    private readonly IAuthService _authService;
    private readonly INotificationService _notifications;
    private readonly IAppLogger _logger;
    private readonly IAppointmentService _appointments;
    private readonly JsonStorageOptions _storage;

    public AdminController(
        IRepository<User> users,
        IRepository<Doctor> doctors,
        IRepository<Patient> patients,
        IRepository<Department> departments,
        IRepository<Schedule> schedules,
        IStatisticsService statistics,
        IAuthService authService,
        INotificationService notifications,
        IAppLogger logger,
        IAppointmentService appointments,
        JsonStorageOptions storage)
    {
        _users = users;
        _doctors = doctors;
        _patients = patients;
        _departments = departments;
        _schedules = schedules;
        _statistics = statistics;
        _authService = authService;
        _notifications = notifications;
        _logger = logger;
        _appointments = appointments;
        _storage = storage;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await _statistics.GetAdminDashboardAsync();
        return View(vm);
    }

    // ----- Users -----
    public async Task<IActionResult> Users()
    {
        var users = await _users.GetAllAsync();
        return View(users.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            await _users.UpdateAsync(user);
        }
        return RedirectToAction(nameof(Users));
    }

    // ----- Doctors -----
    public async Task<IActionResult> Doctors()
    {
        var doctors = await _doctors.GetAllAsync();
        ViewBag.Departments = await _departments.GetAllAsync();
        return View(doctors);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDoctor(string fullName, string tcKimlikNo, string title, Guid departmentId, string email, string phone, string password)
    {
        try
        {
            var user = await _authService.RegisterAsync(new User
            {
                TcKimlikNo = tcKimlikNo,
                FullName = fullName,
                Email = email,
                Phone = phone,
                Role = UserRole.Doctor
            }, string.IsNullOrWhiteSpace(password) ? "Doktor123!" : password);

            var doctor = await _doctors.AddAsync(new Doctor
            {
                UserId = user.Id,
                FullName = fullName,
                TcKimlikNo = tcKimlikNo,
                Title = title,
                DepartmentId = departmentId,
                Email = email,
                Phone = phone
            });

            for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
                await _schedules.AddAsync(new Schedule { DoctorId = doctor.Id, DayOfWeek = day });

            TempData["Success"] = "Doktor eklendi.";
        }
        catch (AppException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Doctors));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDoctor(Guid id)
    {
        var doctor = await _doctors.GetByIdAsync(id);
        if (doctor != null)
        {
            await _doctors.DeleteAsync(id);
            await _users.DeleteAsync(doctor.UserId);
            TempData["Success"] = "Doktor silindi.";
        }
        return RedirectToAction(nameof(Doctors));
    }

    public async Task<IActionResult> Patients()
    {
        var patients = await _patients.GetAllAsync();
        return View(patients);
    }

    // ----- Departments -----
    public async Task<IActionResult> Departments()
    {
        var departments = await _departments.GetAllAsync();
        return View(departments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepartment(string name, string description, string icon)
    {
        await _departments.AddAsync(new Department
        {
            Name = name,
            Description = description,
            Icon = string.IsNullOrWhiteSpace(icon) ? "fa-stethoscope" : icon
        });
        TempData["Success"] = "Poliklinik eklendi.";
        return RedirectToAction(nameof(Departments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        await _departments.DeleteAsync(id);
        TempData["Success"] = "Poliklinik silindi.";
        return RedirectToAction(nameof(Departments));
    }

    // ----- Appointments -----
    public async Task<IActionResult> Appointments()
    {
        var appointments = await _appointments.GetAllAsync();
        var patients = await _patients.GetAllAsync();
        var doctors = await _doctors.GetAllAsync();
        var departments = await _departments.GetAllAsync();
        var vm = appointments.Select(a => new AppointmentViewModel
        {
            Id = a.Id,
            PatientName = patients.FirstOrDefault(p => p.Id == a.PatientId)?.FullName ?? "-",
            DoctorName = doctors.FirstOrDefault(d => d.Id == a.DoctorId)?.FullName ?? "-",
            DepartmentName = departments.FirstOrDefault(d => d.Id == a.DepartmentId)?.Name ?? "-",
            Date = a.AppointmentDate,
            TimeSlot = a.TimeSlot,
            Status = a.Status,
            Notes = a.Notes
        }).ToList();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(Guid id)
    {
        await _appointments.CancelAsync(id);
        TempData["Success"] = "Randevu iptal edildi.";
        return RedirectToAction(nameof(Appointments));
    }

    // ----- Announcements -----
    [HttpGet]
    public IActionResult Announcements() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Announcements(string title, string message)
    {
        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(message))
        {
            await _notifications.BroadcastAsync(title, message, NotificationType.SystemAnnouncement);
            TempData["Success"] = "Duyuru tüm kullanıcılara gönderildi.";
        }
        return RedirectToAction(nameof(Announcements));
    }

    // ----- Reports -----
    public async Task<IActionResult> Reports()
    {
        var vm = await _statistics.GetReportsAsync();
        return View(vm);
    }

    // ----- Settings -----
    public IActionResult Settings() => View();

    // ----- Notifications / Profile (navbar) -----
    public async Task<IActionResult> Notifications()
    {
        var items = await _notifications.GetForUserAsync(User.GetUserId());
        return View(items);
    }

    public async Task<IActionResult> Profile()
    {
        var user = await _authService.GetByIdAsync(User.GetUserId());
        return View(user);
    }

    // ----- Logs -----
    public async Task<IActionResult> Logs()
    {
        var logs = await _logger.GetLogsAsync(300);
        return View(logs);
    }

    // ----- Backup (JSON zip download) -----
    public IActionResult Backup()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            foreach (var file in Directory.GetFiles(_storage.DataDirectory, "*.json"))
            {
                var entry = archive.CreateEntry(Path.GetFileName(file));
                using var entryStream = entry.Open();
                using var fileStream = System.IO.File.OpenRead(file);
                fileStream.CopyTo(entryStream);
            }
        }
        return File(memory.ToArray(), "application/zip", $"hastane-yedek-{DateTime.Now:yyyyMMdd-HHmm}.zip");
    }
}
