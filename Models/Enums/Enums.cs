namespace HospitalSystem.Models.Enums;

public enum UserRole
{
    Patient = 0,
    Doctor = 1,
    Admin = 2
}

public enum Gender
{
    Unspecified = 0,
    Male = 1,
    Female = 2
}

public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}

public enum NotificationType
{
    NewAppointment = 0,
    UpcomingAppointment = 1,
    CancelledAppointment = 2,
    PrescriptionCreated = 3,
    SystemAnnouncement = 4
}

public enum LogLevel
{
    Information = 0,
    Warning = 1,
    Error = 2
}
