using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistroEstudiantes.Api.Extensions;
using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Application.Services;

namespace RegistroEstudiantes.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/enrollments")]
public sealed class EnrollmentsController : ControllerBase
{
    private readonly EnrollmentService _enrollments;

    public EnrollmentsController(EnrollmentService enrollments)
    {
        _enrollments = enrollments;
    }

    /// <summary>Mis clases con los nombres de mis compañeros (regla 9).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<MyEnrollmentDto>> GetMine(CancellationToken ct) =>
        Ok(await _enrollments.GetMineAsync(User.GetStudentId(), ct));

    /// <summary>Inscribe exactamente 3 materias con profesores distintos (reglas 5 y 7).</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(MyEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MyEnrollmentDto>> Enroll(EnrollRequest request, CancellationToken ct) =>
        Ok(await _enrollments.EnrollAsync(User.GetStudentId(), request, ct));
}
