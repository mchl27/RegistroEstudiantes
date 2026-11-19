using RegistroEstudiantes.Application.Abstractions;
using RegistroEstudiantes.Application.Contracts;

namespace RegistroEstudiantes.Application.Services;

/// <summary>Reglas 3, 4 y 6: catálogo de materias con su profesor.</summary>
public sealed class SubjectService
{
    private readonly ISubjectRepository _subjects;

    public SubjectService(ISubjectRepository subjects)
    {
        _subjects = subjects;
    }

    public Task<IReadOnlyList<SubjectDto>> GetCatalogAsync(CancellationToken ct) =>
        _subjects.GetCatalogAsync(ct);
}
