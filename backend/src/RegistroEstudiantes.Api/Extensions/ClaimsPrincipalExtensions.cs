using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RegistroEstudiantes.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id del estudiante autenticado, tomado del claim "sub" del JWT.</summary>
    public static int GetStudentId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new UnauthorizedAccessException("Token sin identificador de estudiante.");
}
