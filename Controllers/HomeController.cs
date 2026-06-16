using System.Diagnostics;
using HospitalSystem.Helpers;
using HospitalSystem.Models.Enums;
using HospitalSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return User.GetRole() switch
            {
                UserRole.Admin => RedirectToAction("Index", "Admin"),
                UserRole.Doctor => RedirectToAction("Index", "Doctor"),
                _ => RedirectToAction("Index", "Patient")
            };
        }
        return RedirectToAction("Login", "Account");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
