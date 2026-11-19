using FluentValidation;
using RegistroEstudiantes.Application.Abstractions;
using RegistroEstudiantes.Application.Common;
using RegistroEstudiantes.Application.Contracts;
using RegistroEstudiantes.Domain.Entities;

namespace RegistroEstudiantes.Application.Services;

public sealed class AuthService
{
    private const int DefaultCreditProgramId = 1;

    private readonly IStudentRepository _students;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(IStudentRepository students, IUnitOfWork unitOfWork, IPasswordHasher hasher,
        ITokenService tokens, IValidator<RegisterRequest> registerValidator, IValidator<LoginRequest> loginValidator)
    {
        _students = students;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _tokens = tokens;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    /// <summary>Regla 1 (Create) + regla 2: el estudiante se adhiere al programa de créditos.</summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        await _registerValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var document = request.DocumentNumber.Trim();
        await StudentGuards.EnsureUniqueAsync(_students, email, document, excludeStudentId: null, ct);

        var student = Student.Create(DefaultCreditProgramId, request.FirstName, request.LastName,
                                     document, email, _hasher.Hash(request.Password));
        _students.Add(student);
        await _unitOfWork.SaveChangesAsync(ct);

        return BuildResponse(student);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await _loginValidator.ValidateAndThrowAsync(request, ct);

        var student = await _students.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);

        // Mismo error si no existe o si la clave falla: no revelamos qué correos están registrados.
        if (student is null || !_hasher.Verify(student.PasswordHash, request.Password))
            throw new InvalidCredentialsException();

        return BuildResponse(student);
    }

    private AuthResponse BuildResponse(Student student)
    {
        var token = _tokens.CreateToken(student);
        return new AuthResponse(token.Token, token.ExpiresAt, student.ToProfileDto());
    }
}
