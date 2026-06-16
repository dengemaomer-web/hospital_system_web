using HospitalSystem.Helpers;
using HospitalSystem.Middleware;
using HospitalSystem.Models.Entities;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Implementations;
using HospitalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ----- JSON storage -----
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "Data", "Json");
builder.Services.AddSingleton(new JsonStorageOptions { DataDirectory = dataDir });
builder.Services.AddSingleton(typeof(IRepository<>), typeof(JsonRepository<>));

// ----- Services -----
builder.Services.AddScoped<IAppLogger, AppLogger>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IMedicineSuggestionService, MedicineSuggestionService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IDataSeeder, DataSeeder>();

// ----- Authentication / Authorization -----
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ----- Seed demo data -----
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync();
}

app.Run();
