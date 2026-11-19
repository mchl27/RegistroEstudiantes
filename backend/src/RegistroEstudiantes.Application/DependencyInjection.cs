using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RegistroEstudiantes.Application.Services;

namespace RegistroEstudiantes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Mensajes de FluentValidation en español
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<AuthService>();
        services.AddScoped<StudentService>();
        services.AddScoped<EnrollmentService>();
        services.AddScoped<SubjectService>();
        return services;
    }
}
