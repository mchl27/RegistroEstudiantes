using Microsoft.AspNetCore.Mvc;
using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Application.Services;

namespace RegistroEstudiantes.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Registro en línea de un estudiante (regla 1: Create).</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var response = await _auth.RegisterAsync(request, ct);
        return Created("/api/students/me", response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await _auth.LoginAsync(request, ct));
}
