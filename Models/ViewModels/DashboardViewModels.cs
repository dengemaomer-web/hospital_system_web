using HospitalSystem.Models.Entities;

namespace HospitalSystem.Models.ViewModels;

public class StatCard
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-chart-simple";
    public string Color { get; set; } = "primary";
    public string? Trend { get; set; }
}

public class PatientDashboardViewModel
{
    public Patient Patient { get; set; } = null!;
    public List<StatCard> Stats { get; set; } = new();
    public List<AppointmentViewModel> UpcomingAppointments { get; set; } = new();
    public List<Prescription> ActivePrescriptions { get; set; } = new();
}

public class DoctorDashboardViewModel
{
    public Doctor Doctor { get; set; } = null!;
    public List<StatCard> Stats { get; set; } = new();
    public List<AppointmentViewModel> TodayAppointments { get; set; } = new();
}

public class AdminDashboardViewModel
{
    public List<StatCard> Stats { get; set; } = new();
    public List<string> MonthLabels { get; set; } = new();
    public List<int> MonthlyAppointments { get; set; } = new();
    public List<string> DepartmentLabels { get; set; } = new();
    public List<int> DepartmentLoads { get; set; } = new();
    public List<AppointmentViewModel> RecentAppointments { get; set; } = new();
}

public class ReportsViewModel
{
    public List<string> MonthLabels { get; set; } = new();
    public List<int> MonthlyAppointments { get; set; } = new();
    public List<string> DepartmentLabels { get; set; } = new();
    public List<int> DepartmentLoads { get; set; } = new();
    public List<string> DoctorLabels { get; set; } = new();
    public List<int> DoctorPerformance { get; set; } = new();
    public List<string> StatusLabels { get; set; } = new();
    public List<int> StatusCounts { get; set; } = new();
}
