using FluentValidation;
using RegistroEstudiantes.Application.Contracts;

namespace RegistroEstudiantes.Application.Validation;

// Validan la FORMA de los datos de entrada. Las reglas de negocio viven en el dominio.

public sealed class StudentProfileValidator : AbstractValidator<IStudentProfileData>
{
    public StudentProfileValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80).WithName("Nombre");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80).WithName("Apellido");
        RuleFor(x => x.DocumentNumber).NotEmpty().Matches("^[0-9]{5,20}$")
            .WithMessage("El documento debe tener entre 5 y 20 dígitos.").WithName("Documento");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150).WithName("Correo");
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        Include(new StudentProfileValidator());
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Za-z]").WithMessage("La contraseña debe contener al menos una letra.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un número.")
            .WithName("Contraseña");
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        Include(new StudentProfileValidator());
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithName("Correo");
        RuleFor(x => x.Password).NotEmpty().WithName("Contraseña");
    }
}

public sealed class EnrollRequestValidator : AbstractValidator<EnrollRequest>
{
    public EnrollRequestValidator()
    {
        RuleFor(x => x.SubjectIds).NotNull().WithName("Materias");
        RuleForEach(x => x.SubjectIds).GreaterThan(0).WithName("Materia");
    }
}
