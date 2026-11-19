namespace RegistroEstudiantes.Application.Common;

/// <summary>Base de los errores de aplicación. La API los traduce a códigos HTTP.</summary>
public abstract class AppException : Exception
{
    public string Code { get; }

    protected AppException(string code, string message) : base(message)
    {
        Code = code;
    }
}

/// <summary>El recurso no existe (HTTP 404).</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string code, string message) : base(code, message) { }
}

/// <summary>Choque con datos existentes: correo repetido, restricción de BD (HTTP 409).</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string code, string message) : base(code, message) { }
}

/// <summary>Correo o contraseña incorrectos (HTTP 401).</summary>
public sealed class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException()
        : base("auth.invalid_credentials", "Correo o contraseña incorrectos.") { }
}
