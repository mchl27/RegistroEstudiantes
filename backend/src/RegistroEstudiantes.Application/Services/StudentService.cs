using FluentValidation;
using RegistroEstudiantes.Application.Abstractions;
using RegistroEstudiantes.Application.Common;
using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Domain.Entities;

namespace RegistroEstudiantes.Application.Services;

/// <summary>Regla 1: Read, Update y Delete del propio registro. Regla 8: directorio.</summary>
public sealed class StudentService
{
    private readonly IStudentRepository _students;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateProfileRequest> _updateValidator;

    public StudentService(IStudentRepository students, IUnitOfWork unitOfWork,
        IValidator<UpdateProfileRequest> updateValidator)
    {
        _students = students;
        _unitOfWork = unitOfWork;
        _updateValidator = updateValidator;
    }

    public async Task<StudentProfileDto> GetProfileAsync(int studentId, CancellationToken ct) =>
        (await GetActiveAsync(studentId, ct)).ToProfileDto();

    public async Task<StudentProfileDto> UpdateProfileAsync(int studentId, UpdateProfileRequest request,
        CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);
        var student = await GetActiveAsync(studentId, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var document = request.DocumentNumber.Trim();
        await StudentGuards.EnsureUniqueAsync(_students, email, document, studentId, ct);

        student.UpdateProfile(request.FirstName, request.LastName, document, email);
        await _unitOfWork.SaveChangesAsync(ct);
        return student.ToProfileDto();
    }

    /// <summary>Borrado lógico + liberación de cupos, todo en una transacción.</summary>
    public async Task DeleteAsync(int studentId, CancellationToken ct)
    {
        var student = await GetActiveAsync(studentId, ct);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _students.DeleteEnrollmentsAsync(studentId, ct);
            student.Delete();
            await _unitOfWork.SaveChangesAsync(ct);
        }, ct);
    }

    public Task<IReadOnlyList<PublicStudentDto>> GetDirectoryAsync(CancellationToken ct) =>
        _students.GetDirectoryAsync(ct);

    private async Task<Student> GetActiveAsync(int studentId, CancellationToken ct) =>
        await _students.GetByIdAsync(studentId, ct)
        ?? throw new NotFoundException("student.not_found", "Estudiante no encontrado.");
}
