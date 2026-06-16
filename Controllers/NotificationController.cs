using HospitalSystem.Helpers;
using HospitalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notifications;

    public NotificationController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _notifications.MarkAsReadAsync(id);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllReadAsync(User.GetUserId());
        return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : "/");
    }
}
