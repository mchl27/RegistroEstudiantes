# Registro de Estudiantes

Aplicación web para el registro de estudiantes en un programa de créditos.

- **Backend:** .NET 8 Web API (Clean Architecture)
- **Frontend:** Angular
- **Base de datos:** SQL Server

## Estructura

```
database/   Scripts SQL (esquema y datos semilla)
backend/    Solución .NET 8 (Domain, Application, Infrastructure, Api, Tests)
frontend/   Aplicación Angular
docs/       Documentación de la entrega
```

## Base de datos

```powershell
sqlcmd -S .\SQLEXPRESS -E -I -b -f 65001 -i database\01_schema.sql
sqlcmd -S .\SQLEXPRESS -E -I -b -f 65001 -i database\02_seed.sql
```

> Instrucciones completas de despliegue en construcción.
