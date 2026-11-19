using RegistroEstudiantes.Domain.Entities;

namespace RegistroEstudiantes.Tests.Domain;

/// <summary>Réplica en memoria del 02_seed.sql.</summary>
internal static class TestCatalog
{
    public static readonly Professor[] Professors =
    {
        new(1, "Carlos Ramírez"), new(2, "Laura Gómez"), new(3, "Andrés Martínez"),
        new(4, "Diana Torres"),   new(5, "Jorge Herrera")
    };

    public static readonly Subject[] Subjects =
    {
        new(1,  "MAT101", "Cálculo Diferencial",    1, 1, professor: Professors[0]),
        new(2,  "MAT102", "Álgebra Lineal",         1, 2, professor: Professors[0]),
        new(3,  "PRG101", "Programación I",         2, 1, professor: Professors[1]),
        new(4,  "PRG201", "Estructuras de Datos",   2, 2, professor: Professors[1]),
        new(5,  "BDD101", "Bases de Datos",         3, 1, professor: Professors[2]),
        new(6,  "ING301", "Ingeniería de Software", 3, 2, professor: Professors[2]),
        new(7,  "RED201", "Redes de Computadores",  4, 1, professor: Professors[3]),
        new(8,  "SIS201", "Sistemas Operativos",    4, 2, professor: Professors[3]),
        new(9,  "FIS101", "Física Mecánica",        5, 1, professor: Professors[4]),
        new(10, "EST201", "Estadística",            5, 2, professor: Professors[4])
    };

    public static Subject[] Pick(params int[] ids) => ids.Select(id => Subjects[id - 1]).ToArray();

    public static Student NewStudent(string email = "ana@test.local") =>
        Student.Create(1, "Ana", "Pérez", "1001", email, "hash");
}
