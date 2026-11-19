using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RegistroEstudiantes.Application.Abstractions;
using RegistroEstudiantes.Application.Common;

namespace RegistroEstudiantes.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    // Traduce el nombre del constraint de SQL Server a un error de negocio legible.
    private static readonly Dictionary<string, (string Code, string Message)> ConstraintErrors = new()
    {
        ["UQ_Enrollment_StudentProfessor"] = ("enrollment.same_professor", "No puedes tomar dos materias con el mismo profesor."),
        ["UQ_Enrollment_StudentSlot"] = ("enrollment.invalid_count", "Solo puedes inscribir 3 materias."),
        ["PK_Enrollment"] = ("enrollment.duplicate_subject", "Ya estás inscrito en esa materia."),
        ["UX_Student_Email"] = ("student.email_taken", "El correo ya está registrado."),
        ["UX_Student_Document"] = ("student.document_taken", "El documento ya está registrado.")
    };

    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        // 2601/2627 = índice o constraint único; 547 = CHECK o FOREIGN KEY
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 or 547 } sql)
        {
            var match = ConstraintErrors.FirstOrDefault(c => sql.Message.Contains(c.Key));
            throw match.Key is null
                ? new ConflictException("data.conflict", "La operación entra en conflicto con datos existentes.")
                : new ConflictException(match.Value.Code, match.Value.Message);
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            await action();
            await transaction.CommitAsync(ct);
        });
    }
}
