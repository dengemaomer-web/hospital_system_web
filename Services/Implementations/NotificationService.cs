using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;

namespace HospitalSystem.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notifications;
    private readonly IRepository<User> _users;

    public NotificationService(IRepository<Notification> notifications, IRepository<User> users)
    {
        _notifications = notifications;
        _users = users;
    }

    public Task<Notification> CreateAsync(Guid userId, string title, string message, NotificationType type)
    {
        return _notifications.AddAsync(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type
        });
    }

    public async Task BroadcastAsync(string title, string message, NotificationType type)
    {
        var users = await _users.GetAllAsync();
        foreach (var user in users.Where(u => u.IsActive))
        {
            await CreateAsync(user.Id, title, message, type);
        }
    }

    public async Task<List<Notification>> GetForUserAsync(Guid userId, int take = 50)
    {
        var all = await _notifications.FindAsync(n => n.UserId == userId);
        return all.OrderByDescending(n => n.CreatedAt).Take(take).ToList();
    }

    public Task<int> UnreadCountAsync(Guid userId)
        => _notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAsReadAsync(Guid id)
    {
        var notification = await _notifications.GetByIdAsync(id);
        if (notification is { IsRead: false })
        {
            notification.IsRead = true;
            await _notifications.UpdateAsync(notification);
        }
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var items = await _notifications.FindAsync(n => n.UserId == userId && !n.IsRead);
        foreach (var item in items)
        {
            item.IsRead = true;
            await _notifications.UpdateAsync(item);
        }
    }
}
