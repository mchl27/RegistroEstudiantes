using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RegistroEstudiantes.Application.Abstractions;
using RegistroEstudiantes.Domain.Entities;

namespace RegistroEstudiantes.Infrastructure.Persistence;

/// <summary>
/// Crea estudiantes de demostración (solo si no hay ninguno) para que el evaluador
/// vea compañeros de clase y el directorio desde el primer arranque.
/// </summary>
public sealed class DemoDataSeeder
{
    public const string DemoPassword = "Demo1234";

    private static readonly (string First, string Last, string Document, string Email, int[] Subjects)[] DemoStudents =
    {
        ("Ana",   "García",    "10000001", "ana.garcia@registro.test",    new[] { 1, 3, 5 }),
        ("Luis",  "Martínez",  "10000002", "luis.martinez@registro.test", new[] { 1, 4, 7 }),
        ("Sofía", "Rodríguez", "10000003", "sofia.rodriguez@registro.test", new[] { 2, 3, 9 }),
        ("Mateo", "López",     "10000004", "mateo.lopez@registro.test",   new[] { 5, 8, 10 })
    };

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(AppDbContext db, IPasswordHasher hasher, ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Students.IgnoreQueryFilters().AnyAsync(ct))
            return;

        var subjects = await _db.Subjects.Include(s => s.Professor).ToDictionaryAsync(s => s.Id, ct);
        if (subjects.Count == 0)
        {
            _logger.LogWarning("No hay materias. Ejecuta database/02_seed.sql antes de iniciar la API.");
            return;
        }

        var created = DemoStudents
            .Select(d => (Data: d, Entity: Student.Create(1, d.First, d.Last, d.Document, d.Email, _hasher.Hash(DemoPassword))))
            .ToList();

        _db.Students.AddRange(created.Select(c => c.Entity));
        await _db.SaveChangesAsync(ct); // genera los Id de los estudiantes

        foreach (var (data, student) in created)
            student.Enroll(data.Subjects.Select(id => subjects[id]).ToList());

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Se crearon {Count} estudiantes de demostración.", created.Count);
    }
}
