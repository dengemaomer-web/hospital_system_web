using System.Globalization;
using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;
using HospitalSystem.Models.ViewModels;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;

namespace HospitalSystem.Services.Implementations;

public class StatisticsService : IStatisticsService
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<Patient> _patients;
    private readonly IRepository<Doctor> _doctors;
    private readonly IRepository<Department> _departments;
    private readonly IRepository<User> _users;

    public StatisticsService(
        IRepository<Appointment> appointments,
        IRepository<Patient> patients,
        IRepository<Doctor> doctors,
        IRepository<Department> departments,
        IRepository<User> users)
    {
        _appointments = appointments;
        _patients = patients;
        _doctors = doctors;
        _departments = departments;
        _users = users;
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
    {
        var appointments = await _appointments.GetAllAsync();
        var patients = await _patients.GetAllAsync();
        var doctors = await _doctors.GetAllAsync();
        var departments = await _departments.GetAllAsync();
        var users = await _users.GetAllAsync();
        var today = DateTime.Today;

        var vm = new AdminDashboardViewModel
        {
            Stats = new List<StatCard>
            {
                new() { Title = "Toplam Hasta", Value = patients.Count.ToString(), Icon = "fa-hospital-user", Color = "primary" },
                new() { Title = "Toplam Doktor", Value = doctors.Count.ToString(), Icon = "fa-user-doctor", Color = "accent" },
                new() { Title = "Toplam Randevu", Value = appointments.Count.ToString(), Icon = "fa-calendar-check", Color = "success" },
                new() { Title = "Günlük Randevu", Value = appointments.Count(a => a.AppointmentDate.Date == today).ToString(), Icon = "fa-calendar-day", Color = "warning" },
                new() { Title = "Aktif Kullanıcı", Value = users.Count(u => u.IsActive).ToString(), Icon = "fa-users", Color = "primary" },
                new() { Title = "Sistem Durumu", Value = "Çevrimiçi", Icon = "fa-heart-pulse", Color = "success" }
            }
        };

        FillMonthly(vm.MonthLabels, vm.MonthlyAppointments, appointments);
        FillDepartments(vm.DepartmentLabels, vm.DepartmentLoads, appointments, departments);

        vm.RecentAppointments = appointments
            .OrderByDescending(a => a.CreatedAt)
            .Take(8)
            .Select(a => ToViewModel(a, patients, doctors, departments))
            .ToList();

        return vm;
    }

    public async Task<ReportsViewModel> GetReportsAsync()
    {
        var appointments = await _appointments.GetAllAsync();
        var doctors = await _doctors.GetAllAsync();
        var departments = await _departments.GetAllAsync();

        var vm = new ReportsViewModel();
        FillMonthly(vm.MonthLabels, vm.MonthlyAppointments, appointments);
        FillDepartments(vm.DepartmentLabels, vm.DepartmentLoads, appointments, departments);

        foreach (var doctor in doctors)
        {
            vm.DoctorLabels.Add(doctor.FullName);
            vm.DoctorPerformance.Add(appointments.Count(a =>
                a.DoctorId == doctor.Id && a.Status == AppointmentStatus.Completed));
        }

        foreach (AppointmentStatus status in Enum.GetValues<AppointmentStatus>())
        {
            vm.StatusLabels.Add(TranslateStatus(status));
            vm.StatusCounts.Add(appointments.Count(a => a.Status == status));
        }

        return vm;
    }

    private static void FillMonthly(List<string> labels, List<int> values, List<Appointment> appointments)
    {
        var culture = new CultureInfo("tr-TR");
        var now = DateTime.Today;
        for (var i = 5; i >= 0; i--)
        {
            var month = now.AddMonths(-i);
            labels.Add(month.ToString("MMM yyyy", culture));
            values.Add(appointments.Count(a =>
                a.AppointmentDate.Year == month.Year && a.AppointmentDate.Month == month.Month));
        }
    }

    private static void FillDepartments(List<string> labels, List<int> values, List<Appointment> appointments, List<Department> departments)
    {
        foreach (var dept in departments)
        {
            labels.Add(dept.Name);
            values.Add(appointments.Count(a => a.DepartmentId == dept.Id));
        }
    }

    private static AppointmentViewModel ToViewModel(Appointment a, List<Patient> patients, List<Doctor> doctors, List<Department> departments)
        => new()
        {
            Id = a.Id,
            PatientId = a.PatientId,
            DoctorId = a.DoctorId,
            PatientName = patients.FirstOrDefault(p => p.Id == a.PatientId)?.FullName ?? "-",
            DoctorName = doctors.FirstOrDefault(d => d.Id == a.DoctorId)?.FullName ?? "-",
            DepartmentName = departments.FirstOrDefault(d => d.Id == a.DepartmentId)?.Name ?? "-",
            Date = a.AppointmentDate,
            TimeSlot = a.TimeSlot,
            Status = a.Status,
            Notes = a.Notes
        };

    public static string TranslateStatus(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Pending => "Beklemede",
        AppointmentStatus.Confirmed => "Onaylandı",
        AppointmentStatus.Completed => "Tamamlandı",
        AppointmentStatus.Cancelled => "İptal Edildi",
        _ => status.ToString()
    };
}
