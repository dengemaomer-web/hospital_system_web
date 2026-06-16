using System.Security.Claims;
using HospitalSystem.Helpers;
using HospitalSystem.Models.DTOs;
using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IRepository<Patient> _patients;
    private readonly IRepository<Doctor> _doctors;
    private readonly IAppLogger _logger;

    public AccountController(
        IAuthService authService,
        IRepository<Patient> patients,
        IRepository<Doctor> doctors,
        IAppLogger logger)
    {
        _authService = authService;
        _patients = patients;
        _doctors = doctors;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _authService.ValidateCredentialsAsync(model.TcKimlikNo, model.Password, model.Role);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "TC Kimlik No, şifre veya rol hatalı.");
            return View(model);
        }

        await SignInAsync(user, model.RememberMe);
        await _authService.RecordLoginAsync(user);

        return user.Role switch
        {
            UserRole.Admin => RedirectToAction("Index", "Admin"),
            UserRole.Doctor => RedirectToAction("Index", "Doctor"),
            _ => RedirectToAction("Index", "Patient")
        };
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new RegisterDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!Validators.IsValidTcKimlikNo(model.TcKimlikNo))
            ModelState.AddModelError(nameof(model.TcKimlikNo), "Geçersiz TC Kimlik No.");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var user = await _authService.RegisterAsync(new User
            {
                TcKimlikNo = model.TcKimlikNo,
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Role = UserRole.Patient
            }, model.Password);

            await _patients.AddAsync(new Patient
            {
                UserId = user.Id,
                FullName = model.FullName,
                TcKimlikNo = model.TcKimlikNo,
                BirthDate = model.BirthDate,
                Gender = model.Gender,
                Email = model.Email,
                Phone = model.Phone
            });

            TempData["Success"] = "Kayıt başarılı. Giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Login));
        }
        catch (AppException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(User user, bool rememberMe)
    {
        Guid entityId = Guid.Empty;
        if (user.Role == UserRole.Patient)
            entityId = (await _patients.FirstOrDefaultAsync(p => p.UserId == user.Id))?.Id ?? Guid.Empty;
        else if (user.Role == UserRole.Doctor)
            entityId = (await _doctors.FirstOrDefaultAsync(d => d.UserId == user.Id))?.Id ?? Guid.Empty;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.TcKimlikNo),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("FullName", user.FullName),
            new("EntityId", entityId.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), properties);
    }
}
