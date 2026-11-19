namespace RegistroEstudiantes.Application.Contracts;

// ---------- Peticiones (lo que llega del cliente) ----------

/// <summary>Campos de perfil compartidos por registro y actualización.</summary>
public interface IStudentProfileData
{
    string FirstName { get; }
    string LastName { get; }
    string DocumentNumber { get; }
    string Email { get; }
}

public sealed record RegisterRequest(
    string FirstName, string LastName, string DocumentNumber, string Email, string Password)
    : IStudentProfileData;

public sealed record UpdateProfileRequest(
    string FirstName, string LastName, string DocumentNumber, string Email)
    : IStudentProfileData;

public sealed record LoginRequest(string Email, string Password);

public sealed record EnrollRequest(IReadOnlyList<int> SubjectIds);

// ---------- Respuestas (lo que sale hacia el cliente) ----------

public sealed record StudentProfileDto(
    int Id, string FirstName, string LastName, string DocumentNumber, string Email, DateTime CreatedAt);

public sealed record AuthResponse(string Token, DateTime ExpiresAt, StudentProfileDto Student);

public sealed record SubjectDto(
    int Id, string Code, string Name, string? Description, int Credits,
    int ProfessorId, string ProfessorName, int EnrolledCount);

/// <summary>Regla 8: lo único que un estudiante ve de los demás.</summary>
public sealed record PublicStudentDto(
    int Id, string FullName, IReadOnlyList<string> Subjects, int TotalCredits);

/// <summary>Regla 9: Classmates contiene SOLO nombres.</summary>
public sealed record MyClassDto(
    int SubjectId, string Code, string Name, int Credits, string ProfessorName,
    IReadOnlyList<string> Classmates);

public sealed record MyEnrollmentDto(
    int TotalCredits, int RequiredCredits, IReadOnlyList<MyClassDto> Classes);
