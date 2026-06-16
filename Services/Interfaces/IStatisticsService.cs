using HospitalSystem.Models.ViewModels;

namespace HospitalSystem.Services.Interfaces;

public interface IStatisticsService
{
    Task<AdminDashboardViewModel> GetAdminDashboardAsync();
    Task<ReportsViewModel> GetReportsAsync();
}
