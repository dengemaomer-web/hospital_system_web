namespace HospitalSystem.Helpers;

/// <summary>Domain-level exception that carries a user-friendly message.</summary>
public class AppException : Exception
{
    public AppException(string message) : base(message) { }
}

/// <summary>Thrown when domain validation fails (e.g. overlapping appointment).</summary>
public class ValidationException : AppException
{
    public ValidationException(string message) : base(message) { }
}
