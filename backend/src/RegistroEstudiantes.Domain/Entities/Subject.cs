namespace RegistroEstudiantes.Domain.Entities;

public class Subject
{
    public int Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public byte Credits { get; private set; }
    public int ProfessorId { get; private set; }
    public byte ProfessorSlot { get; private set; }
    public Professor? Professor { get; private set; }

    private Subject() { }

    public Subject(int id, string code, string name, int professorId, byte professorSlot,
                   string? description = null, Professor? professor = null)
    {
        Id = id;
        Code = code;
        Name = name;
        Credits = 3;
        ProfessorId = professorId;
        ProfessorSlot = professorSlot;
        Description = description;
        Professor = professor;
    }
}
