using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistroEstudiantes.Api.Extensions;
using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Application.Services;

namespace RegistroEstudiantes.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/students")]
public sealed class StudentsController : ControllerBase
{
    private readonly StudentService _students;

    public StudentsController(StudentService students)
    {
        _students = students;
    }

    /// <summary>Regla 8: registros de todos los estudiantes (nombre y materias).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublicStudentDto>>> GetDirectory(CancellationToken ct) =>
        Ok(await _students.GetDirectoryAsync(ct));

    [HttpGet("me")]
    public async Task<ActionResult<StudentProfileDto>> GetMe(CancellationToken ct) =>
        Ok(await _students.GetProfileAsync(User.GetStudentId(), ct));

    [HttpPut("me")]
    public async Task<ActionResult<StudentProfileDto>> UpdateMe(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await _students.UpdateProfileAsync(User.GetStudentId(), request, ct));

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(CancellationToken ct)
    {
        await _students.DeleteAsync(User.GetStudentId(), ct);
        return NoContent();
    }
}
