using HospitalSystem.Helpers;
using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;

namespace HospitalSystem.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _users;
    private readonly IAppLogger _logger;

    public AuthService(IRepository<User> users, IAppLogger logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task<User?> ValidateCredentialsAsync(string tcKimlikNo, string password, UserRole role)
    {
        var user = await _users.FirstOrDefaultAsync(u =>
            u.TcKimlikNo == tcKimlikNo && u.Role == role && u.IsActive);

        if (user == null)
        {
            await _logger.WarningAsync($"Başarısız giriş denemesi: {tcKimlikNo} ({role})", "Auth");
            return null;
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
        {
            await _logger.WarningAsync($"Hatalı şifre: {tcKimlikNo}", "Auth");
            return null;
        }

        return user;
    }

    public Task<User?> GetByIdAsync(Guid id) => _users.GetByIdAsync(id);

    public Task<User?> GetByTcAsync(string tcKimlikNo)
        => _users.FirstOrDefaultAsync(u => u.TcKimlikNo == tcKimlikNo);

    public async Task<User> RegisterAsync(User user, string password)
    {
        var existing = await GetByTcAsync(user.TcKimlikNo);
        if (existing != null)
            throw new ValidationException("Bu TC Kimlik No ile kayıtlı bir kullanıcı zaten var.");

        var (hash, salt) = PasswordHasher.Hash(password);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        var created = await _users.AddAsync(user);
        await _logger.InfoAsync($"Yeni kullanıcı kaydı: {user.FullName} ({user.Role})", "Auth");
        return created;
    }

    public async Task RecordLoginAsync(User user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await _logger.InfoAsync($"Giriş yapıldı: {user.FullName} ({user.Role})", "Auth");
    }

    public async Task ChangePasswordAsync(Guid userId, string newPassword)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new AppException("Kullanıcı bulunamadı.");
        var (hash, salt) = PasswordHasher.Hash(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _users.UpdateAsync(user);
    }
}
