using HospitalSystem.Helpers;
using HospitalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HospitalSystem.Controllers;

/// <summary>
/// Shared base controller that hydrates the layout (current user, unread notifications)
/// for every authenticated request.
/// </summary>
public abstract class BaseController : Controller
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var notifications = HttpContext.RequestServices.GetRequiredService<INotificationService>();
            var userId = User.GetUserId();
            ViewBag.UnreadCount = await notifications.UnreadCountAsync(userId);
            ViewBag.RecentNotifications = await notifications.GetForUserAsync(userId, 6);
            ViewBag.FullName = User.GetFullName();
            ViewBag.Role = User.GetRole();
        }

        await next();
    }
}
