namespace RegistroEstudiantes.Domain.Entities;

public class Enrollment
{
    public int StudentId { get; private set; }
    public int SubjectId { get; private set; }
    public int ProfessorId { get; private set; }
    public byte Slot { get; private set; }
    public DateTime EnrolledAt { get; private set; }

    public Student? Student { get; private set; }
    public Subject? Subject { get; private set; }

    private Enrollment() { }

    // internal: solo Student (mismo ensamblado) puede crear inscripciones
    internal Enrollment(int studentId, Subject subject, byte slot)
    {
        StudentId = studentId;
        SubjectId = subject.Id;
        ProfessorId = subject.ProfessorId;
        Slot = slot;
        EnrolledAt = DateTime.UtcNow;
    }
}
