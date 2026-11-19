using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RegistroEstudiantes.Api.Security;
using RegistroEstudiantes.Application.Abstractions;

namespace RegistroEstudiantes.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(JwtOptions.SectionName);
        var jwt = section.Get<JwtOptions>() ?? new JwtOptions();
        if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
            throw new InvalidOperationException("Configura 'Jwt:Key' con al menos 32 caracteres.");

        services.Configure<JwtOptions>(section);
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // conserva "sub" tal cual
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = JwtRegisteredClaimNames.Name
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Registro de Estudiantes API",
                Version = "v1",
                Description = "Inscripción en programa de créditos: 3 materias, profesores distintos."
            });

            var bearer = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Pega el token obtenido en /api/auth/login (sin la palabra Bearer).",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            c.AddSecurityDefinition("Bearer", bearer);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = Array.Empty<string>() });
        });
        return services;
    }
}
