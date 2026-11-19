using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RegistroEstudiantes.Application.Abstractions;
using RegistroEstudiantes.Infrastructure.Persistence;
using RegistroEstudiantes.Infrastructure.Security;

namespace RegistroEstudiantes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<DemoDataSeeder>();
        return services;
    }
}
