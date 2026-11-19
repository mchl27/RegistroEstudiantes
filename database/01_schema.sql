/* =============================================================
   01_schema.sql - Registro de Estudiantes (SQL Server)
   Crea la base de datos y las tablas.
   ADVERTENCIA: re-ejecutarlo BORRA y recrea las tablas.
   ============================================================= */
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requerido para índices filtrados
GO

IF DB_ID(N'RegistroEstudiantesDB') IS NULL
    CREATE DATABASE RegistroEstudiantesDB;
GO
USE RegistroEstudiantesDB;
GO

-- Borrado en orden inverso de dependencias
DROP TABLE IF EXISTS dbo.Enrollment;
DROP TABLE IF EXISTS dbo.Student;
DROP TABLE IF EXISTS dbo.Subject;
DROP TABLE IF EXISTS dbo.Professor;
DROP TABLE IF EXISTS dbo.CreditProgram;
GO

/* ---------- Programa de créditos (regla 2) ---------- */
CREATE TABLE dbo.CreditProgram (
    Id          INT            NOT NULL,
    Name        NVARCHAR(100)  NOT NULL,
    Description NVARCHAR(300)  NULL,
    CONSTRAINT PK_CreditProgram PRIMARY KEY (Id)
);

/* ---------- Profesores (regla 6) ---------- */
CREATE TABLE dbo.Professor (
    Id       INT            NOT NULL,
    FullName NVARCHAR(120)  NOT NULL,
    CONSTRAINT PK_Professor PRIMARY KEY (Id)
);

/* ---------- Materias (reglas 3, 4 y 6) ---------- */
CREATE TABLE dbo.Subject (
    Id            INT            NOT NULL,
    Code          VARCHAR(10)    NOT NULL,
    Name          NVARCHAR(100)  NOT NULL,
    Description   NVARCHAR(300)  NULL,
    Credits       TINYINT        NOT NULL CONSTRAINT DF_Subject_Credits DEFAULT (3),
    ProfessorId   INT            NOT NULL,
    ProfessorSlot TINYINT        NOT NULL,  -- 1 o 2: posición de la materia en la carga del profesor
    CONSTRAINT PK_Subject               PRIMARY KEY (Id),
    CONSTRAINT UQ_Subject_Code          UNIQUE (Code),
    CONSTRAINT FK_Subject_Professor     FOREIGN KEY (ProfessorId) REFERENCES dbo.Professor(Id),
    CONSTRAINT CK_Subject_Credits       CHECK (Credits = 3),                  -- regla 4
    CONSTRAINT CK_Subject_ProfessorSlot CHECK (ProfessorSlot IN (1, 2)),
    CONSTRAINT UQ_Subject_ProfessorSlot UNIQUE (ProfessorId, ProfessorSlot),  -- regla 6: máx. 2 por profesor
    CONSTRAINT UQ_Subject_Id_Professor  UNIQUE (Id, ProfessorId)              -- habilita la FK compuesta
);

/* ---------- Estudiantes (regla 1: CRUD) ---------- */
CREATE TABLE dbo.Student (
    Id              INT IDENTITY(1,1) NOT NULL,
    CreditProgramId INT            NOT NULL,
    FirstName       NVARCHAR(80)   NOT NULL,
    LastName        NVARCHAR(80)   NOT NULL,
    DocumentNumber  VARCHAR(20)    NOT NULL,
    Email           NVARCHAR(150)  NOT NULL,
    PasswordHash    NVARCHAR(255)  NOT NULL,
    IsDeleted       BIT            NOT NULL CONSTRAINT DF_Student_IsDeleted DEFAULT (0),
    CreatedAt       DATETIME2(0)   NOT NULL CONSTRAINT DF_Student_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(0)   NULL,
    CONSTRAINT PK_Student               PRIMARY KEY (Id),
    CONSTRAINT FK_Student_CreditProgram FOREIGN KEY (CreditProgramId) REFERENCES dbo.CreditProgram(Id)
);
GO
-- Únicos solo entre estudiantes activos: si uno se borra, el correo queda libre
CREATE UNIQUE INDEX UX_Student_Email    ON dbo.Student(Email)          WHERE IsDeleted = 0;
CREATE UNIQUE INDEX UX_Student_Document ON dbo.Student(DocumentNumber) WHERE IsDeleted = 0;
GO

/* ---------- Inscripciones (reglas 5 y 7) ---------- */
CREATE TABLE dbo.Enrollment (
    StudentId   INT           NOT NULL,
    SubjectId   INT           NOT NULL,
    ProfessorId INT           NOT NULL,   -- desnormalizado a propósito (protegido por FK compuesta)
    Slot        TINYINT       NOT NULL,   -- 1, 2 o 3
    EnrolledAt  DATETIME2(0)  NOT NULL CONSTRAINT DF_Enrollment_EnrolledAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Enrollment                  PRIMARY KEY (StudentId, SubjectId),
    CONSTRAINT FK_Enrollment_Student          FOREIGN KEY (StudentId) REFERENCES dbo.Student(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Enrollment_SubjectProfessor FOREIGN KEY (SubjectId, ProfessorId)
                                              REFERENCES dbo.Subject(Id, ProfessorId),
    CONSTRAINT CK_Enrollment_Slot             CHECK (Slot BETWEEN 1 AND 3),
    CONSTRAINT UQ_Enrollment_StudentSlot      UNIQUE (StudentId, Slot),        -- regla 5: máx. 3 materias
    CONSTRAINT UQ_Enrollment_StudentProfessor UNIQUE (StudentId, ProfessorId)  -- regla 7: profesor distinto
);
GO
-- Acelera la regla 9 (compañeros por materia)
CREATE INDEX IX_Enrollment_SubjectId ON dbo.Enrollment(SubjectId) INCLUDE (StudentId);
GO

PRINT N'Esquema creado correctamente.';
