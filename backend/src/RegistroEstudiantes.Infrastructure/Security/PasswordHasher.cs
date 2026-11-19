using Microsoft.AspNetCore.Identity;
using RegistroEstudiantes.Domain.Entities;
using AppPasswordHasher = RegistroEstudiantes.Application.Abstractions.IPasswordHasher;

namespace RegistroEstudiantes.Infrastructure.Security;

/// <summary>PBKDF2 con sal aleatoria (implementación estándar de ASP.NET Core Identity).</summary>
internal sealed class PasswordHasher : AppPasswordHasher
{
    private readonly PasswordHasher<Student> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _inner.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
