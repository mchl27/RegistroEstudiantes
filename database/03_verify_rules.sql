/* =============================================================
   03_verify_rules.sql - Pruebas de las reglas de negocio en la BD
   Intenta romper cada regla y verifica que SQL Server la rechace.
   Todo corre en una transacción con ROLLBACK: no deja datos.
   ============================================================= */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT OFF;  -- un error de constraint no aborta toda la transacción
GO
USE RegistroEstudiantesDB;
GO

DECLARE @Results TABLE (
    Id       INT IDENTITY(1,1),
    Regla    NVARCHAR(10),
    Prueba   NVARCHAR(150),
    Esperado NVARCHAR(10),
    Obtenido NVARCHAR(10),
    Detalle  NVARCHAR(400)
);
DECLARE @Ana INT, @Luis INT;

BEGIN TRANSACTION;

/* ---------- Datos de prueba ---------- */
INSERT INTO dbo.Student (CreditProgramId, FirstName, LastName, DocumentNumber, Email, PasswordHash)
VALUES (1, N'Ana', N'Prueba', 'TEST-001', N'ana@test.local', N'hash');
SET @Ana = SCOPE_IDENTITY();

INSERT INTO dbo.Student (CreditProgramId, FirstName, LastName, DocumentNumber, Email, PasswordHash)
VALUES (1, N'Luis', N'Prueba', 'TEST-002', N'luis@test.local', N'hash');
SET @Luis = SCOPE_IDENTITY();

/* 1. Caso válido: 3 materias, 3 profesores distintos */
BEGIN TRY
    INSERT INTO dbo.Enrollment (StudentId, SubjectId, ProfessorId, Slot)
    VALUES (@Ana, 1, 1, 1), (@Ana, 3, 2, 2), (@Ana, 5, 3, 3);
    INSERT INTO @Results VALUES (N'5, 7', N'Inscribir 3 materias con profesores distintos', N'ACEPTA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'5, 7', N'Inscribir 3 materias con profesores distintos', N'ACEPTA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* 2. Regla 5: cuarta materia (slot 4) */
BEGIN TRY
    INSERT INTO dbo.Enrollment (StudentId, SubjectId, ProfessorId, Slot) VALUES (@Ana, 7, 4, 4);
    INSERT INTO @Results VALUES (N'5', N'Cuarta materia usando slot 4', N'RECHAZA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'5', N'Cuarta materia usando slot 4', N'RECHAZA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* 3. Regla 5: cuarta materia reutilizando un slot ocupado */
BEGIN TRY
    INSERT INTO dbo.Enrollment (StudentId, SubjectId, ProfessorId, Slot) VALUES (@Ana, 7, 4, 1);
    INSERT INTO @Results VALUES (N'5', N'Cuarta materia reutilizando slot 1', N'RECHAZA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'5', N'Cuarta materia reutilizando slot 1', N'RECHAZA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* 4. Regla 7: dos materias del mismo profesor */
BEGIN TRY
    INSERT INTO dbo.Enrollment (StudentId, SubjectId, ProfessorId, Slot)
    VALUES (@Luis, 1, 1, 1), (@Luis, 2, 1, 2);
    INSERT INTO @Results VALUES (N'7', N'Dos materias del mismo profesor', N'RECHAZA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'7', N'Dos materias del mismo profesor', N'RECHAZA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* 5. Misma materia dos veces */
BEGIN TRY
    INSERT INTO dbo.Enrollment (StudentId, SubjectId, ProfessorId, Slot) VALUES (@Ana, 1, 1, 1);
    INSERT INTO @Results VALUES (N'5', N'Inscribir la misma materia dos veces', N'RECHAZA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'5', N'Inscribir la misma materia dos veces', N'RECHAZA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* 6. Integridad: profesor falso para la materia (Cálculo es del prof. 1, no del 2) */
BEGIN TRY
    INSERT INTO dbo.Enrollment (StudentId, SubjectId, ProfessorId, Slot) VALUES (@Luis, 1, 2, 1);
    INSERT INTO @Results VALUES (N'6, 7', N'Inscripción con profesor que no dicta la materia', N'RECHAZA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'6, 7', N'Inscripción con profesor que no dicta la materia', N'RECHAZA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* 7. Regla 4: materia con créditos distintos de 3 */
BEGIN TRY
    UPDATE dbo.Subject SET Credits = 4 WHERE Id = 1;
    INSERT INTO @Results VALUES (N'4', N'Cambiar créditos de una materia a 4', N'RECHAZA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'4', N'Cambiar créditos de una materia a 4', N'RECHAZA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* 8. Regla 6: tercera materia para un profesor */
BEGIN TRY
    INSERT INTO dbo.Subject (Id, Code, Name, Credits, ProfessorId, ProfessorSlot)
    VALUES (11, 'TST999', N'Materia extra', 3, 1, 3);
    INSERT INTO @Results VALUES (N'6', N'Asignar una tercera materia a un profesor', N'RECHAZA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'6', N'Asignar una tercera materia a un profesor', N'RECHAZA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* 9. Regla 1: correo duplicado entre estudiantes activos */
BEGIN TRY
    INSERT INTO dbo.Student (CreditProgramId, FirstName, LastName, DocumentNumber, Email, PasswordHash)
    VALUES (1, N'Otra', N'Ana', 'TEST-003', N'ana@test.local', N'hash');
    INSERT INTO @Results VALUES (N'1', N'Registrar correo ya usado por estudiante activo', N'RECHAZA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'1', N'Registrar correo ya usado por estudiante activo', N'RECHAZA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* 10. Luis se inscribe válidamente y comparte Cálculo con Ana */
BEGIN TRY
    INSERT INTO dbo.Enrollment (StudentId, SubjectId, ProfessorId, Slot)
    VALUES (@Luis, 1, 1, 1), (@Luis, 4, 2, 2), (@Luis, 6, 3, 3);
    INSERT INTO @Results VALUES (N'5, 7', N'Segundo estudiante con inscripción válida', N'ACEPTA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'5, 7', N'Segundo estudiante con inscripción válida', N'ACEPTA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* Regla 9: Ana ve SOLO los nombres de sus compañeros por clase */
PRINT N'--- Regla 9: compañeros de clase de Ana (solo nombres) ---';
SELECT s.Name                          AS Materia,
       c.FirstName + N' ' + c.LastName AS Companero
FROM dbo.Enrollment mine
JOIN dbo.Subject    s     ON s.Id = mine.SubjectId
JOIN dbo.Enrollment other ON other.SubjectId = mine.SubjectId
                         AND other.StudentId <> mine.StudentId
JOIN dbo.Student    c     ON c.Id = other.StudentId AND c.IsDeleted = 0
WHERE mine.StudentId = @Ana
ORDER BY s.Name, Companero;

/* 11. Borrado lógico: el correo queda libre para un nuevo registro */
BEGIN TRY
    UPDATE dbo.Student SET IsDeleted = 1 WHERE Id = @Ana;
    INSERT INTO dbo.Student (CreditProgramId, FirstName, LastName, DocumentNumber, Email, PasswordHash)
    VALUES (1, N'Ana', N'Nueva', 'TEST-001', N'ana@test.local', N'hash');
    INSERT INTO @Results VALUES (N'1', N'Reutilizar correo tras borrado lógico', N'ACEPTA', N'ACEPTA', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (N'1', N'Reutilizar correo tras borrado lógico', N'ACEPTA', N'RECHAZA', LEFT(ERROR_MESSAGE(), 400));
END CATCH;

/* ---------- Deshacer todo ---------- */
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

/* ---------- Resultados (la variable de tabla sobrevive al ROLLBACK) ---------- */
PRINT N'--- Resultados de las pruebas ---';
SELECT Id, Regla, Prueba, Esperado, Obtenido,
       CASE WHEN Esperado = Obtenido THEN N'PASA' ELSE N'FALLA' END AS Estado
FROM @Results
ORDER BY Id;

SELECT COUNT(*) AS Total,
       SUM(CASE WHEN Esperado = Obtenido THEN 1 ELSE 0 END) AS Pasan
FROM @Results;

-- Confirmación de que no quedaron datos de prueba
SELECT COUNT(*) AS EstudiantesDePruebaRestantes
FROM dbo.Student WHERE DocumentNumber LIKE 'TEST-%';
GO
