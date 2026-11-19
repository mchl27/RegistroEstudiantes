namespace RegistroEstudiantes.Domain.Common;

/// <summary>Constantes del programa de créditos (reglas 4 y 5).</summary>
public static class EnrollmentRules
{
    public const int RequiredSubjects = 3;
    public const int CreditsPerSubject = 3;
    public const int TotalCredits = RequiredSubjects * CreditsPerSubject; // 9
}
