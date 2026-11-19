namespace RegistroEstudiantes.Domain.Entities;

public class Professor
{
    public int Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;

    private Professor() { }

    public Professor(int id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }
}
