using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegistroEstudiantes.Domain.Entities;

namespace RegistroEstudiantes.Infrastructure.Persistence.Configurations;

internal sealed class CreditProgramConfiguration : IEntityTypeConfiguration<CreditProgram>
{
    public void Configure(EntityTypeBuilder<CreditProgram> b)
    {
        b.ToTable("CreditProgram");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(300);
    }
}

internal sealed class ProfessorConfiguration : IEntityTypeConfiguration<Professor>
{
    public void Configure(EntityTypeBuilder<Professor> b)
    {
        b.ToTable("Professor");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.FullName).HasMaxLength(120).IsRequired();
    }
}

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("Subject");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Code).HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(300);

        b.HasOne(x => x.Professor).WithMany().HasForeignKey(x => x.ProfessorId);
    }
}

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.ToTable("Student");
        b.HasKey(x => x.Id);
        b.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        b.Property(x => x.DocumentNumber).HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.Email).HasMaxLength(150).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");

        b.Ignore(x => x.FullName);
        b.Ignore(x => x.TotalCredits);

        b.HasOne<CreditProgram>().WithMany().HasForeignKey(x => x.CreditProgramId);

        // Student expone Enrollments como solo lectura (IReadOnlyCollection) para que nadie
        // agregue inscripciones "por fuera" del método Enroll. Con PropertyAccessMode.Field
        // le decimos a EF que lea y escriba directamente la lista privada _enrollments.
        b.HasMany(x => x.Enrollments).WithOne(e => e.Student).HasForeignKey(e => e.StudentId);
        b.Navigation(x => x.Enrollments).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Borrado lógico: los eliminados desaparecen de todas las consultas
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.ToTable("Enrollment");
        b.HasKey(x => new { x.StudentId, x.SubjectId });
        b.Property(x => x.EnrolledAt).HasColumnType("datetime2(0)");

        // FK compuesta (SubjectId, ProfessorId) -> Subject(Id, ProfessorId), igual que en el script SQL.
        // HasPrincipalKey apunta a la llave alterna UQ_Subject_Id_Professor y no a la PK normal.
        // Gracias a esto el ProfessorId guardado en Enrollment siempre es el real de la materia.
        b.HasOne(x => x.Subject).WithMany()
            .HasForeignKey(x => new { x.SubjectId, x.ProfessorId })
            .HasPrincipalKey(s => new { s.Id, s.ProfessorId });

        // Mismo filtro que Student: si el estudiante está borrado, sus inscripciones
        // tampoco aparecen (ni en "compañeros" ni en el conteo de inscritos).
        b.HasQueryFilter(x => !x.Student!.IsDeleted);
    }
}
