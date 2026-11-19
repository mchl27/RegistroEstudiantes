namespace RegistroEstudiantes.Domain.Entities;

public class CreditProgram
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private CreditProgram() { } // requerido por EF Core

    public CreditProgram(int id, string name, string? description = null)
    {
        Id = id;
        Name = name;
        Description = description;
    }
}
