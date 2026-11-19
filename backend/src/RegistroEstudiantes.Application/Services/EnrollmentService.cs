using FluentValidation;
using RegistroEstudiantes.Application.Abstractions;
using RegistroEstudiantes.Application.Common;
using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Domain.Common;

namespace RegistroEstudiantes.Application.Services;

/// <summary>Reglas 5, 7 y 9: inscripción de materias y compañeros de clase.</summary>
public sealed class EnrollmentService
{
    private readonly IStudentRepository _students;
    private readonly ISubjectRepository _subjects;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<EnrollRequest> _validator;

    public EnrollmentService(IStudentRepository students, ISubjectRepository subjects,
        IUnitOfWork unitOfWork, IValidator<EnrollRequest> validator)
    {
        _students = students;
        _subjects = subjects;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<MyEnrollmentDto> GetMineAsync(int studentId, CancellationToken ct)
    {
        var classes = await _students.GetMyClassesAsync(studentId, ct);
        return new MyEnrollmentDto(classes.Sum(c => c.Credits), EnrollmentRules.TotalCredits, classes);
    }

    /// <summary>Reemplaza la inscripción completa de forma atómica.</summary>
    public async Task<MyEnrollmentDto> EnrollAsync(int studentId, EnrollRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        var student = await _students.GetByIdAsync(studentId, ct)
            ?? throw new NotFoundException("student.not_found", "Estudiante no encontrado.");

        // Conservamos los duplicados del request para que el dominio los detecte y explique.
        var found = await _subjects.GetByIdsAsync(request.SubjectIds.Distinct().ToList(), ct);
        var selected = request.SubjectIds
            .Select(id => found.FirstOrDefault(s => s.Id == id)
                ?? throw new NotFoundException("subject.not_found", $"La materia {id} no existe."))
            .ToList();

        // 1) Reglas de negocio en memoria: si falla, no se toca la base de datos.
        student.Enroll(selected);

        // 2) Persistencia atómica: borrar la inscripción anterior e insertar la nueva.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _students.DeleteEnrollmentsAsync(studentId, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }, ct);

        return await GetMineAsync(studentId, ct);
    }
}
