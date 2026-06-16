using HospitalSystem.Models.Enums;

namespace HospitalSystem.Models.Entities;

public class Patient : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string TcKimlikNo { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public Gender Gender { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public List<Guid> FavoriteDoctorIds { get; set; } = new();
}
