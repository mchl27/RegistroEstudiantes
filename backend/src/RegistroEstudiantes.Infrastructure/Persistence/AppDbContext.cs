using Microsoft.EntityFrameworkCore;
using RegistroEstudiantes.Domain.Entities;

namespace RegistroEstudiantes.Infrastructure.Persistence;

/// <summary>
/// Mapea las tablas creadas por database/01_schema.sql.
/// No usamos migraciones: los scripts SQL son la fuente de verdad.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Professor> Professors => Set<Professor>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<CreditProgram> CreditPrograms => Set<CreditProgram>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
