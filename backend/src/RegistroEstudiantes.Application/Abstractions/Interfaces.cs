using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Domain.Entities;

namespace RegistroEstudiantes.Application.Abstractions;

// Puertos: la capa Application define QUÉ necesita; Infrastructure y Api deciden CÓMO.

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id, CancellationToken ct);
    Task<Student?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, int? excludeStudentId, CancellationToken ct);
    Task<bool> DocumentExistsAsync(string documentNumber, int? excludeStudentId, CancellationToken ct);
    void Add(Student student);
    Task DeleteEnrollmentsAsync(int studentId, CancellationToken ct);

    /// <summary>Regla 8: registros de todos los estudiantes (solo datos públicos).</summary>
    Task<IReadOnlyList<PublicStudentDto>> GetDirectoryAsync(CancellationToken ct);

    /// <summary>Regla 9: mis clases con SOLO los nombres de mis compañeros.</summary>
    Task<IReadOnlyList<MyClassDto>> GetMyClassesAsync(int studentId, CancellationToken ct);
}

public interface ISubjectRepository
{
    Task<IReadOnlyList<SubjectDto>> GetCatalogAsync(CancellationToken ct);
    Task<IReadOnlyList<Subject>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateToken(Student student);
}
