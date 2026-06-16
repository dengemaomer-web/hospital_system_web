using HospitalSystem.Models.Entities;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;
using LogLevel = HospitalSystem.Models.Enums.LogLevel;

namespace HospitalSystem.Services.Implementations;

/// <summary>
/// Persists log records to Logs.json and mirrors them to the framework logger.
/// </summary>
public class AppLogger : IAppLogger
{
    private readonly IRepository<LogEntry> _repository;
    private readonly ILogger<AppLogger> _logger;

    public AppLogger(IRepository<LogEntry> repository, ILogger<AppLogger> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    private async Task WriteAsync(LogLevel level, string message, string source, Exception? ex = null)
    {
        try
        {
            await _repository.AddAsync(new LogEntry
            {
                Level = level,
                Message = message,
                Source = source,
                Exception = ex?.ToString()
            });
        }
        catch (Exception writeEx)
        {
            _logger.LogError(writeEx, "Failed to persist log entry");
        }
    }

    public Task InfoAsync(string message, string source = "App")
    {
        _logger.LogInformation("[{Source}] {Message}", source, message);
        return WriteAsync(LogLevel.Information, message, source);
    }

    public Task WarningAsync(string message, string source = "App")
    {
        _logger.LogWarning("[{Source}] {Message}", source, message);
        return WriteAsync(LogLevel.Warning, message, source);
    }

    public Task ErrorAsync(string message, Exception? ex = null, string source = "App")
    {
        _logger.LogError(ex, "[{Source}] {Message}", source, message);
        return WriteAsync(LogLevel.Error, message, source, ex);
    }

    public async Task<List<LogEntry>> GetLogsAsync(int take = 200)
    {
        var all = await _repository.GetAllAsync();
        return all.OrderByDescending(l => l.CreatedAt).Take(take).ToList();
    }
}
