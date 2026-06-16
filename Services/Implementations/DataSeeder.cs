using HospitalSystem.Helpers;
using HospitalSystem.Models.Entities;
using HospitalSystem.Models.Enums;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;

namespace HospitalSystem.Services.Implementations;

/// <summary>
/// Seeds demo data on first run so the application is fully navigable out of the box.
/// </summary>
public class DataSeeder : IDataSeeder
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Department> _departments;
    private readonly IRepository<Doctor> _doctors;
    private readonly IRepository<Patient> _patients;
    private readonly IRepository<Schedule> _schedules;
    private readonly IRepository<Medicine> _medicines;
    private readonly IRepository<Diagnosis> _diagnoses;
    private readonly IRepository<Appointment> _appointments;
    private readonly IAppLogger _logger;

    public DataSeeder(
        IRepository<User> users,
        IRepository<Department> departments,
        IRepository<Doctor> doctors,
        IRepository<Patient> patients,
        IRepository<Schedule> schedules,
        IRepository<Medicine> medicines,
        IRepository<Diagnosis> diagnoses,
        IRepository<Appointment> appointments,
        IAppLogger logger)
    {
        _users = users;
        _departments = departments;
        _doctors = doctors;
        _patients = patients;
        _schedules = schedules;
        _medicines = medicines;
        _diagnoses = diagnoses;
        _appointments = appointments;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _users.CountAsync() > 0)
            return;

        await _logger.InfoAsync("Demo verileri oluşturuluyor...", "Seeder");

        // ----- Departments -----
        var departments = new List<Department>
        {
            new() { Name = "Dahiliye", Description = "İç Hastalıkları", Icon = "fa-stethoscope" },
            new() { Name = "Kardiyoloji", Description = "Kalp ve Damar Hastalıkları", Icon = "fa-heart-pulse" },
            new() { Name = "Nöroloji", Description = "Sinir Sistemi Hastalıkları", Icon = "fa-brain" },
            new() { Name = "Ortopedi", Description = "Kemik ve Eklem Hastalıkları", Icon = "fa-bone" },
            new() { Name = "Göz Hastalıkları", Description = "Oftalmoloji", Icon = "fa-eye" },
            new() { Name = "KBB", Description = "Kulak Burun Boğaz", Icon = "fa-ear-listen" }
        };
        foreach (var d in departments) await _departments.AddAsync(d);

        // ----- Admin -----
        await CreateUserAsync("11111111110", "Sistem Yöneticisi", "Admin123!", UserRole.Admin,
            "admin@hastane.com", "0212 000 0000");

        // ----- Doctors -----
        var doctorSeeds = new[]
        {
            ("12345678950", "Dr. Ahmet Yılmaz", "Uzm. Dr.", "Dahiliye", "ahmet@hastane.com"),
            ("12345678901", "Dr. Ayşe Demir", "Prof. Dr.", "Kardiyoloji", "ayse@hastane.com"),
            ("10000000146", "Dr. Mehmet Kaya", "Doç. Dr.", "Nöroloji", "mehmet@hastane.com"),
            ("10000000308", "Dr. Elif Şahin", "Uzm. Dr.", "Ortopedi", "elif@hastane.com")
        };

        var doctorIds = new List<(Guid doctorId, Guid userId)>();
        foreach (var (tc, name, title, deptName, email) in doctorSeeds)
        {
            var user = await CreateUserAsync(tc, name, "Doktor123!", UserRole.Doctor, email, "0212 111 1111");
            var dept = departments.First(d => d.Name == deptName);
            var doctor = await _doctors.AddAsync(new Doctor
            {
                UserId = user.Id,
                FullName = name,
                TcKimlikNo = tc,
                DepartmentId = dept.Id,
                Title = title,
                Email = email,
                Phone = "0212 111 1111",
                Bio = $"{deptName} bölümünde uzman hekim.",
                Rating = 4.5 + (doctorIds.Count % 2 == 0 ? 0.3 : 0.1),
                RatingCount = 20 + doctorIds.Count * 5
            });
            doctorIds.Add((doctor.Id, user.Id));

            // Working schedule: weekdays 09:00-17:00.
            for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
            {
                await _schedules.AddAsync(new Schedule
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = day,
                    StartTime = "09:00",
                    EndTime = "17:00",
                    SlotMinutes = 30,
                    DailyCapacity = 16
                });
            }
        }

        // ----- Patient -----
        var patientUser = await CreateUserAsync("10000000014", "Can Öztürk", "Hasta123!", UserRole.Patient,
            "can@example.com", "0532 123 4567");
        var patient = await _patients.AddAsync(new Patient
        {
            UserId = patientUser.Id,
            FullName = "Can Öztürk",
            TcKimlikNo = "10000000014",
            BirthDate = new DateTime(1990, 5, 12),
            Gender = Gender.Male,
            Phone = "0532 123 4567",
            Email = "can@example.com",
            Address = "İstanbul",
            BloodType = "A Rh+"
        });

        // ----- Medicines -----
        var medicineNames = new (string name, string type)[]
        {
            ("Parol", "Ağrı Kesici"), ("Theraflu", "Soğuk Algınlığı"), ("Aferin", "Soğuk Algınlığı"),
            ("Arveles", "Ağrı Kesici"), ("Apranax", "Ağrı Kesici"), ("Augmentin", "Antibiyotik"),
            ("Nexium", "Mide"), ("Coraspin", "Kan Sulandırıcı"), ("Majezik", "Ağrı Kesici"),
            ("Cipro", "Antibiyotik")
        };
        foreach (var (name, type) in medicineNames)
            await _medicines.AddAsync(new Medicine { Name = name, Type = type, Description = $"{name} ({type})" });

        // ----- Diagnoses with smart suggestions -----
        var diagnoses = new (string name, string[] meds)[]
        {
            ("Grip", new[] { "Parol", "Theraflu", "Aferin" }),
            ("Migren", new[] { "Arveles", "Apranax" }),
            ("Üst Solunum Yolu Enfeksiyonu", new[] { "Augmentin", "Parol" }),
            ("Reflü", new[] { "Nexium" }),
            ("Bel Ağrısı", new[] { "Majezik", "Arveles" }),
            ("İdrar Yolu Enfeksiyonu", new[] { "Cipro" })
        };
        foreach (var (name, meds) in diagnoses)
            await _diagnoses.AddAsync(new Diagnosis
            {
                Name = name,
                Description = $"{name} tanısı",
                RecommendedMedicines = meds.ToList()
            });

        // ----- Sample appointments -----
        var firstDoctor = doctorIds.First();
        var today = DateTime.Today;
        await _appointments.AddAsync(new Appointment
        {
            PatientId = patient.Id,
            DoctorId = firstDoctor.doctorId,
            DepartmentId = departments.First(d => d.Name == "Dahiliye").Id,
            AppointmentDate = today,
            TimeSlot = "10:00",
            Status = AppointmentStatus.Confirmed,
            Notes = "Genel kontrol"
        });
        await _appointments.AddAsync(new Appointment
        {
            PatientId = patient.Id,
            DoctorId = firstDoctor.doctorId,
            DepartmentId = departments.First(d => d.Name == "Dahiliye").Id,
            AppointmentDate = today.AddDays(-10),
            TimeSlot = "14:00",
            Status = AppointmentStatus.Completed,
            Notes = "Geçmiş muayene"
        });

        await _logger.InfoAsync("Demo verileri oluşturuldu.", "Seeder");
    }

    private async Task<User> CreateUserAsync(string tc, string name, string password, UserRole role, string email, string phone)
    {
        var (hash, salt) = PasswordHasher.Hash(password);
        return await _users.AddAsync(new User
        {
            TcKimlikNo = tc,
            FullName = name,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            Email = email,
            Phone = phone,
            IsActive = true
        });
    }
}
