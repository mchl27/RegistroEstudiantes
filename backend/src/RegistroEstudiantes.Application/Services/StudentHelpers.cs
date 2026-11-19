using RegistroEstudiantes.Application.Abstractions;
using RegistroEstudiantes.Application.Common;
using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Domain.Entities;

namespace RegistroEstudiantes.Application.Services;

internal static class StudentGuards
{
    /// <summary>Mensaje amigable antes de llegar al índice único de la BD (que sigue siendo la garantía final).</summary>
    public static async Task EnsureUniqueAsync(IStudentRepository students, string email, string document,
        int? excludeStudentId, CancellationToken ct)
    {
        if (await students.EmailExistsAsync(email, excludeStudentId, ct))
            throw new ConflictException("student.email_taken", "El correo ya está registrado.");

        if (await students.DocumentExistsAsync(document, excludeStudentId, ct))
            throw new ConflictException("student.document_taken", "El documento ya está registrado.");
    }
}

internal static class StudentMappings
{
    public static StudentProfileDto ToProfileDto(this Student s) =>
        new(s.Id, s.FirstName, s.LastName, s.DocumentNumber, s.Email, s.CreatedAt);
}
