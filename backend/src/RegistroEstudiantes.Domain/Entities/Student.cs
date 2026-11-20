using RegistroEstudiantes.Domain.Common;

namespace RegistroEstudiantes.Domain.Entities;

public class Student
{
    private readonly List<Enrollment> _enrollments = new();

    public int Id { get; private set; }
    public int CreditProgramId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<Enrollment> Enrollments => _enrollments.AsReadOnly();
    public string FullName => $"{FirstName} {LastName}";
    public int TotalCredits => _enrollments.Count * EnrollmentRules.CreditsPerSubject;

    private Student() { }

    public static Student Create(int creditProgramId, string firstName, string lastName,
                                 string documentNumber, string email, string passwordHash)
    {
        var student = new Student
        {
            CreditProgramId = creditProgramId,
            PasswordHash = Required(passwordHash, "contraseña", 255),
            CreatedAt = DateTime.UtcNow
        };
        student.SetProfile(firstName, lastName, documentNumber, email);
        return student;
    }

    public void UpdateProfile(string firstName, string lastName, string documentNumber, string email)
    {
        EnsureActive();
        SetProfile(firstName, lastName, documentNumber, email);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reemplaza la inscripción completa. Reglas 5 y 7:
    /// exactamente 3 materias, sin repetir materia ni profesor.
    /// </summary>
    public void Enroll(IReadOnlyCollection<Subject> subjects)
    {
        EnsureActive();

        if (subjects is null || subjects.Count != EnrollmentRules.RequiredSubjects)
            throw new DomainException("enrollment.invalid_count",
                $"Debes seleccionar exactamente {EnrollmentRules.RequiredSubjects} materias.");

        if (subjects.Select(s => s.Id).Distinct().Count() != subjects.Count)
            throw new DomainException("enrollment.duplicate_subject",
                "No puedes inscribir la misma materia dos veces.");

        // Agrupamos por profesor: si algún grupo tiene más de una materia,
        // el estudiante eligió dos clases con la misma persona (regla 7).
        var repeated = subjects.GroupBy(s => s.ProfessorId).FirstOrDefault(g => g.Count() > 1);
        if (repeated is not null)
        {
            var professor = repeated.First().Professor?.FullName ?? $"#{repeated.Key}";
            throw new DomainException("enrollment.same_professor",
                $"No puedes tomar dos materias con el mismo profesor ({professor}).");
        }

        // Todo validado: reemplazamos la inscripción completa.
        // Cada materia ocupa un "slot" 1, 2 o 3. En la BD hay un UNIQUE (StudentId, Slot)
        // y un CHECK (Slot 1..3), por eso es imposible guardar una cuarta materia
        // aunque alguien se salte esta validación. Ordenamos por Id para que el
        // resultado sea siempre el mismo sin importar el orden en que llegaron.
        _enrollments.Clear();
        byte slot = 1;
        foreach (var subject in subjects.OrderBy(s => s.Id))
            _enrollments.Add(new Enrollment(Id, subject, slot++));

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Borrado lógico: libera las inscripciones.</summary>
    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        _enrollments.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetProfile(string firstName, string lastName, string documentNumber, string email)
    {
        FirstName = Required(firstName, "nombre", 80);
        LastName = Required(lastName, "apellido", 80);
        DocumentNumber = Required(documentNumber, "documento", 20);
        Email = Required(email, "correo", 150).ToLowerInvariant();
    }

    private void EnsureActive()
    {
        if (IsDeleted)
            throw new DomainException("student.deleted", "El estudiante fue eliminado.");
    }

    private static string Required(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new DomainException("student.required", $"El campo {field} es obligatorio.");
        if (trimmed.Length > maxLength)
            throw new DomainException("student.too_long", $"El campo {field} admite máximo {maxLength} caracteres.");
        return trimmed;
    }
}
