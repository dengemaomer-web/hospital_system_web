using HospitalSystem.Helpers;
using HospitalSystem.Services.Interfaces;

namespace HospitalSystem.Middleware;

/// <summary>
/// Global exception handler. Domain (<see cref="AppException"/>) errors are surfaced to the
/// user via TempData; unexpected errors are logged and redirected to the error page.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAppLogger appLogger)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await appLogger.ErrorAsync($"İşlenmeyen hata: {ex.Message}", ex, "Middleware");

            if (context.Response.HasStarted)
                throw;

            context.Response.Redirect("/Home/Error");
        }
    }
}
