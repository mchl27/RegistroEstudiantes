using Microsoft.EntityFrameworkCore;
using RegistroEstudiantes.Application.Abstractions;
using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Domain.Common;
using RegistroEstudiantes.Domain.Entities;

namespace RegistroEstudiantes.Infrastructure.Persistence;

internal sealed class StudentRepository : IStudentRepository
{
    private readonly AppDbContext _db;

    public StudentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Student?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Student?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Students.FirstOrDefaultAsync(s => s.Email == email, ct);

    // excludeStudentId: al EDITAR el perfil, el propio estudiante ya tiene ese correo
    // y no debe contar como duplicado. Al REGISTRAR llega null y se revisan todos.
    // (EF traduce bien la comparación con null, no hace falta un if.)
    public Task<bool> EmailExistsAsync(string email, int? excludeStudentId, CancellationToken ct) =>
        _db.Students.AnyAsync(s => s.Email == email && s.Id != excludeStudentId, ct);

    public Task<bool> DocumentExistsAsync(string documentNumber, int? excludeStudentId, CancellationToken ct) =>
        _db.Students.AnyAsync(s => s.DocumentNumber == documentNumber && s.Id != excludeStudentId, ct);

    public void Add(Student student) => _db.Students.Add(student);

    // ExecuteDeleteAsync manda un DELETE directo a SQL, sin cargar las filas en memoria.
    // IgnoreQueryFilters: el filtro global oculta inscripciones de estudiantes borrados,
    // pero aquí queremos borrarlas TODAS, incluso al momento de eliminar la cuenta.
    public async Task DeleteEnrollmentsAsync(int studentId, CancellationToken ct) =>
        await _db.Enrollments.IgnoreQueryFilters()
            .Where(e => e.StudentId == studentId)
            .ExecuteDeleteAsync(ct);

    public async Task<IReadOnlyList<PublicStudentDto>> GetDirectoryAsync(CancellationToken ct)
    {
        // Dos pasos a propósito:
        //  1) La consulta trae SOLO las columnas que necesitamos (nada de correo ni documento).
        //  2) Ya en memoria armamos el DTO: el $"{...}" y el cálculo de créditos son C# puro
        //     y así no le pedimos a EF que los traduzca a SQL.
        // AsNoTracking: es solo lectura, EF no necesita vigilar cambios -> más rápido.
        var rows = await _db.Students.AsNoTracking()
            .OrderBy(s => s.FirstName).ThenBy(s => s.LastName)
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                Subjects = s.Enrollments.OrderBy(e => e.Slot).Select(e => e.Subject!.Name).ToList()
            })
            .ToListAsync(ct);

        return rows.Select(r => new PublicStudentDto(
                r.Id, $"{r.FirstName} {r.LastName}", r.Subjects,
                r.Subjects.Count * EnrollmentRules.CreditsPerSubject))
            .ToList();
    }

    public async Task<IReadOnlyList<MyClassDto>> GetMyClassesAsync(int studentId, CancellationToken ct)
    {
        var rows = await _db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.Slot)
            .Select(e => new
            {
                e.SubjectId,
                e.Subject!.Code,
                e.Subject.Name,
                e.Subject.Credits,
                ProfessorName = e.Subject.Professor!.FullName,

                // Regla 9: solo el nombre de quienes comparten la clase.
                // Es una subconsulta "correlacionada": para cada materia mía busca las
                // otras inscripciones de esa misma materia (menos la mía). EF la resuelve
                // en UNA sola ida a la BD, no hace una consulta por cada materia (N+1).
                // El índice IX_Enrollment_SubjectId del script SQL existe por esta consulta.
                Classmates = _db.Enrollments
                    .Where(o => o.SubjectId == e.SubjectId && o.StudentId != studentId)
                    .OrderBy(o => o.Student!.FirstName).ThenBy(o => o.Student!.LastName)
                    .Select(o => o.Student!.FirstName + " " + o.Student.LastName)
                    .ToList()
            })
            .ToListAsync(ct);

        return rows.Select(r => new MyClassDto(
                r.SubjectId, r.Code, r.Name, r.Credits, r.ProfessorName, r.Classmates))
            .ToList();
    }
}

internal sealed class SubjectRepository : ISubjectRepository
{
    private readonly AppDbContext _db;

    public SubjectRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SubjectDto>> GetCatalogAsync(CancellationToken ct) =>
        await _db.Subjects.AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => new SubjectDto(
                s.Id, s.Code, s.Name, s.Description, s.Credits,
                s.ProfessorId, s.Professor!.FullName,
                _db.Enrollments.Count(e => e.SubjectId == s.Id)))
            .ToListAsync(ct);

    // Include(Professor): el dominio usa el nombre del profesor para armar el mensaje
    // "No puedes tomar dos materias con el mismo profesor (Carlos Ramírez)".
    // ids.Contains(...) se traduce a un WHERE Id IN (...) en SQL.
    public async Task<IReadOnlyList<Subject>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        await _db.Subjects.Include(s => s.Professor)
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);
}
