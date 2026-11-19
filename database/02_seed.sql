/* =============================================================
   02_seed.sql - Datos de catálogo: programa, 5 profesores, 10 materias
   Idempotente: solo inserta si las tablas están vacías.
   ============================================================= */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO
USE RegistroEstudiantesDB;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CreditProgram)
    INSERT INTO dbo.CreditProgram (Id, Name, Description) VALUES
    (1, N'Programa de Créditos - Ingeniería',
        N'El estudiante inscribe 3 materias de 3 créditos (9 créditos) con profesores distintos.');

IF NOT EXISTS (SELECT 1 FROM dbo.Professor)
    INSERT INTO dbo.Professor (Id, FullName) VALUES
    (1, N'Carlos Ramírez'),
    (2, N'Laura Gómez'),
    (3, N'Andrés Martínez'),
    (4, N'Diana Torres'),
    (5, N'Jorge Herrera');

IF NOT EXISTS (SELECT 1 FROM dbo.Subject)
    INSERT INTO dbo.Subject (Id, Code, Name, Description, Credits, ProfessorId, ProfessorSlot) VALUES
    (1,  'MAT101', N'Cálculo Diferencial',     N'Límites, derivadas y aplicaciones.',        3, 1, 1),
    (2,  'MAT102', N'Álgebra Lineal',          N'Matrices, vectores y transformaciones.',    3, 1, 2),
    (3,  'PRG101', N'Programación I',          N'Fundamentos de algoritmos y programación.', 3, 2, 1),
    (4,  'PRG201', N'Estructuras de Datos',    N'Listas, árboles, grafos y complejidad.',    3, 2, 2),
    (5,  'BDD101', N'Bases de Datos',          N'Modelo relacional, SQL y normalización.',   3, 3, 1),
    (6,  'ING301', N'Ingeniería de Software',  N'Procesos, requisitos y arquitectura.',      3, 3, 2),
    (7,  'RED201', N'Redes de Computadores',   N'Modelo OSI, TCP/IP y enrutamiento.',        3, 4, 1),
    (8,  'SIS201', N'Sistemas Operativos',     N'Procesos, memoria y concurrencia.',         3, 4, 2),
    (9,  'FIS101', N'Física Mecánica',         N'Cinemática, dinámica y energía.',           3, 5, 1),
    (10, 'EST201', N'Estadística',             N'Probabilidad e inferencia estadística.',    3, 5, 2);
GO

-- Verificación: cada profesor debe tener exactamente 2 materias
SELECT p.FullName, COUNT(s.Id) AS Materias, SUM(s.Credits) AS Creditos
FROM dbo.Professor p
LEFT JOIN dbo.Subject s ON s.ProfessorId = p.Id
GROUP BY p.FullName
ORDER BY p.FullName;
GO
PRINT N'Datos semilla cargados.';
