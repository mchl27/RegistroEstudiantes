using RegistroEstudiantes.Domain.Common;
using RegistroEstudiantes.Domain.Entities;
using static RegistroEstudiantes.Tests.Domain.TestCatalog;

namespace RegistroEstudiantes.Tests.Domain;

public class StudentTests
{
    [Fact]
    public void Enroll_ThreeSubjectsWithDifferentProfessors_Succeeds()
    {
        var student = NewStudent();

        student.Enroll(Pick(1, 3, 5));

        Assert.Equal(3, student.Enrollments.Count);
        Assert.Equal(9, student.TotalCredits);
        Assert.Equal(new byte[] { 1, 2, 3 }, student.Enrollments.Select(e => e.Slot).ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1, 3)]
    [InlineData(1, 3, 5, 7)]
    public void Enroll_NotExactlyThreeSubjects_Throws(params int[] ids)
    {
        var student = NewStudent();

        var ex = Assert.Throws<DomainException>(() => student.Enroll(Pick(ids)));

        Assert.Equal("enrollment.invalid_count", ex.Code);
    }

    [Fact]
    public void Enroll_TwoSubjectsFromSameProfessor_Throws()
    {
        var student = NewStudent();

        var ex = Assert.Throws<DomainException>(() => student.Enroll(Pick(1, 2, 5)));

        Assert.Equal("enrollment.same_professor", ex.Code);
        Assert.Contains("Carlos Ramírez", ex.Message);
    }

    [Fact]
    public void Enroll_SameSubjectTwice_Throws()
    {
        var student = NewStudent();

        var ex = Assert.Throws<DomainException>(() => student.Enroll(Pick(1, 1, 5)));

        Assert.Equal("enrollment.duplicate_subject", ex.Code);
    }

    [Fact]
    public void Enroll_Again_ReplacesPreviousEnrollment()
    {
        var student = NewStudent();
        student.Enroll(Pick(1, 3, 5));

        student.Enroll(Pick(2, 4, 10));

        Assert.Equal(new[] { 2, 4, 10 }, student.Enrollments.Select(e => e.SubjectId).ToArray());
    }

    [Fact]
    public void Delete_ClearsEnrollmentsAndBlocksChanges()
    {
        var student = NewStudent();
        student.Enroll(Pick(1, 3, 5));

        student.Delete();

        Assert.True(student.IsDeleted);
        Assert.Empty(student.Enrollments);
        var ex = Assert.Throws<DomainException>(() => student.Enroll(Pick(1, 3, 5)));
        Assert.Equal("student.deleted", ex.Code);
    }

    [Fact]
    public void Create_NormalizesEmailAndTrimsNames()
    {
        var student = NewStudent("  ANA@Test.Local ");

        Assert.Equal("ana@test.local", student.Email);
        Assert.Equal("Ana Pérez", student.FullName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyFirstName_Throws(string firstName)
    {
        var ex = Assert.Throws<DomainException>(() =>
            Student.Create(1, firstName, "Pérez", "1001", "a@test.local", "hash"));

        Assert.Equal("student.required", ex.Code);
    }
}
