using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;

namespace HospitalSystem.Services.Interfaces;

public interface IAuthService
{
    Task<User?> ValidateCredentialsAsync(string tcKimlikNo, string password, UserRole role);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByTcAsync(string tcKimlikNo);
    Task<User> RegisterAsync(User user, string password);
    Task RecordLoginAsync(User user);
    Task ChangePasswordAsync(Guid userId, string newPassword);
}
