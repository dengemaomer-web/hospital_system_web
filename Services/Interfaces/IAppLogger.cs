using HospitalSystem.Models.Entities;

namespace HospitalSystem.Services.Interfaces;

public interface IAppLogger
{
    Task InfoAsync(string message, string source = "App");
    Task WarningAsync(string message, string source = "App");
    Task ErrorAsync(string message, Exception? ex = null, string source = "App");
    Task<List<LogEntry>> GetLogsAsync(int take = 200);
}
