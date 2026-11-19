namespace RegistroEstudiantes.Domain.Common;

/// <summary>
/// Error de regla de negocio. El Code permite al frontend identificar el error
/// sin depender del texto del mensaje.
/// </summary>
public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
