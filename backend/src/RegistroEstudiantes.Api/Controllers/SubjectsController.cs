using Microsoft.AspNetCore.Mvc;
using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Application.Services;

namespace RegistroEstudiantes.Api.Controllers;

[ApiController]
[Route("api/subjects")]
public sealed class SubjectsController : ControllerBase
{
    private readonly SubjectService _subjects;

    public SubjectsController(SubjectService subjects)
    {
        _subjects = subjects;
    }

    /// <summary>Catálogo público: 10 materias, 3 créditos, profesor y cantidad de inscritos.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubjectDto>>> GetAll(CancellationToken ct) =>
        Ok(await _subjects.GetCatalogAsync(ct));
}
