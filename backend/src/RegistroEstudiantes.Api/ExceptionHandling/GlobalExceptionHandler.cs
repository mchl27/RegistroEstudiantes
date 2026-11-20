using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RegistroEstudiantes.Application.Common;
using RegistroEstudiantes.Domain.Common;

namespace RegistroEstudiantes.Api.ExceptionHandling;

/// <summary>
/// Punto único que convierte excepciones en respuestas ProblemDetails (RFC 7807).
/// Los controladores quedan limpios: sin try/catch.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        // Cada tipo de excepción tiene su código HTTP. La diferencia entre 400 y 422:
        //   400 -> el dato viene mal formado (correo sin @, campo vacío)
        //   422 -> el dato está bien formado pero rompe una regla (dos materias del mismo profesor)
        // Lo que no reconocemos cae en 500 y se registra en el log para revisarlo.
        var (status, title, code) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Datos inválidos", "validation"),
            DomainException d => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida", d.Code),
            NotFoundException n => (StatusCodes.Status404NotFound, "No encontrado", n.Code),
            ConflictException c => (StatusCodes.Status409Conflict, "Conflicto", c.Code),
            InvalidCredentialsException i => (StatusCodes.Status401Unauthorized, "No autorizado", i.Code),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado", "auth.unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "server.error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Error no controlado");

        ProblemDetails problem = exception is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        // En un 500 NO devolvemos exception.Message: podría filtrar detalles internos
        // (nombres de tablas, rutas del servidor). El detalle real queda solo en el log.
        problem.Detail = status switch
        {
            StatusCodes.Status500InternalServerError => "Ocurrió un error inesperado.",
            StatusCodes.Status400BadRequest => "Uno o más campos no son válidos.",
            _ => exception.Message
        };
        problem.Extensions["code"] = code;

        context.Response.StatusCode = status;
        var written = await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });

        // Si el cliente pidió otro formato (ej. Accept: text/plain), igual respondemos
        // el error real en JSON en vez de dejar que ASP.NET escriba un 500 genérico.
        if (!written)
        {
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem, problem.GetType(),
                options: null, contentType: "application/problem+json", cancellationToken: ct);
        }

        return true;
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
