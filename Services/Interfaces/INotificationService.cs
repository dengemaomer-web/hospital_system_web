using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;

namespace HospitalSystem.Services.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(Guid userId, string title, string message, NotificationType type);
    Task BroadcastAsync(string title, string message, NotificationType type);
    Task<List<Notification>> GetForUserAsync(Guid userId, int take = 50);
    Task<int> UnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid id);
    Task MarkAllReadAsync(Guid userId);
}
