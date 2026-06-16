using System.Security.Claims;
using HospitalSystem.Models.Enums;

namespace HospitalSystem.Helpers;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    public static Guid GetEntityId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("EntityId");
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    public static string GetFullName(this ClaimsPrincipal user)
        => user.FindFirstValue("FullName") ?? user.Identity?.Name ?? "Kullanıcı";

    public static UserRole GetRole(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(value, out var role) ? role : UserRole.Patient;
    }
}
