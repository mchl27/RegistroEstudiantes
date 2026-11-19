<#
.SYNOPSIS
    Prepara y ejecuta Registro de Estudiantes con un solo comando.

.DESCRIPTION
    1. Verifica requisitos (.NET 8, Node.js, sqlcmd).
    2. Detecta una instancia de SQL Server local (o usa -SqlServer).
    3. Crea la base de datos con los scripts de /database (si no existe).
    4. Compila el backend y ejecuta los tests.
    5. Instala las dependencias del frontend.
    6. Levanta la API (5080) y la web (4200) en ventanas nuevas y abre el navegador.

.EXAMPLE
    .\setup.ps1
.EXAMPLE
    .\setup.ps1 -SqlServer "localhost"
.EXAMPLE
    .\setup.ps1 -ResetDatabase      # recrea la base de datos desde cero
.EXAMPLE
    .\setup.ps1 -NoStart            # solo prepara, no levanta nada
#>
[CmdletBinding()]
param(
    [string]$SqlServer,
    [switch]$ResetDatabase,
    [switch]$NoStart,
    [switch]$NoBrowser,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
$Database = 'RegistroEstudiantesDB'
$ApiPort = 5080
$WebPort = 4200
$ApiProject = Join-Path $Root 'backend\src\RegistroEstudiantes.Api'
$Solution = Join-Path $Root 'backend\RegistroEstudiantes.sln'
$Frontend = Join-Path $Root 'frontend'

# ---------------------------------------------------------------- utilidades

function Write-Step([int]$Number, [string]$Text) {
    Write-Host ''
    Write-Host "[$Number/6] $Text" -ForegroundColor Cyan
}

function Write-Ok([string]$Text) { Write-Host "  OK   $Text" -ForegroundColor Green }
function Write-Info([string]$Text) { Write-Host "  ..   $Text" -ForegroundColor Gray }
function Write-Warn([string]$Text) { Write-Host "  !!   $Text" -ForegroundColor Yellow }

function Stop-Setup([string]$Text, [string]$Hint) {
    Write-Host "  ERROR  $Text" -ForegroundColor Red
    if ($Hint) { Write-Host "         $Hint" -ForegroundColor Yellow }
    exit 1
}

function Test-Command([string]$Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

# Ejecuta sqlcmd con autenticación de Windows. -C confía en el certificado local.
function Invoke-Sql([string]$Server, [string[]]$Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $output = & sqlcmd -S $Server -E -C -b -I -l 5 @Arguments 2>&1
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    return [pscustomobject]@{ ExitCode = $code; Output = ($output | Out-String) }
}

function Test-PortInUse([int]$Port) {
    return [bool](Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

function Wait-Port([int]$Port, [int]$TimeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-PortInUse $Port) { return $true }
        Start-Sleep -Seconds 2
    }
    return $false
}

Write-Host ''
Write-Host '=== Registro de Estudiantes - instalación ===' -ForegroundColor White

# ------------------------------------------------------ 1. requisitos

Write-Step 1 'Verificando requisitos'

if (-not (Test-Command 'dotnet')) {
    Stop-Setup 'No se encontró .NET.' 'Instala el SDK de .NET 8: https://dotnet.microsoft.com/download/dotnet/8.0'
}
$sdks = (& dotnet --list-sdks) -join "`n"
if ($sdks -notmatch '(?m)^([89]|\d{2,})\.') {
    Stop-Setup 'Se requiere el SDK de .NET 8 o superior.' 'https://dotnet.microsoft.com/download/dotnet/8.0'
}
$runtimes = (& dotnet --list-runtimes) -join "`n"
if ($runtimes -notmatch 'Microsoft\.AspNetCore\.App 8\.') {
    Stop-Setup 'Falta el runtime ASP.NET Core 8.' 'https://dotnet.microsoft.com/download/dotnet/8.0'
}
Write-Ok '.NET 8'

if (-not (Test-Command 'node')) {
    Stop-Setup 'No se encontró Node.js.' 'Instala Node.js 20.19+ o 22 LTS: https://nodejs.org'
}
$nodeVersion = (& node --version).TrimStart('v')
$nodeMajor = [int]($nodeVersion.Split('.')[0])
if ($nodeMajor -lt 20) {
    Stop-Setup "Node.js $nodeVersion es muy antiguo." 'Angular 20 requiere Node.js 20.19+ o 22 LTS.'
}
Write-Ok "Node.js $nodeVersion"

if (-not (Test-Command 'sqlcmd')) {
    Stop-Setup 'No se encontró sqlcmd.' 'Instálalo con: winget install Microsoft.Sqlcmd'
}
Write-Ok 'sqlcmd'

# ------------------------------------------------------ 2. SQL Server

Write-Step 2 'Buscando SQL Server'

$candidates = if ($SqlServer) { @($SqlServer) } else { @('.\SQLEXPRESS', 'localhost', '(localdb)\MSSQLLocalDB') }
$server = $null
foreach ($candidate in $candidates) {
    Write-Info "Probando $candidate"
    $probe = Invoke-Sql $candidate @('-Q', 'SET NOCOUNT ON; SELECT 1')
    if ($probe.ExitCode -eq 0) { $server = $candidate; break }
}
if (-not $server) {
    Stop-Setup 'No se pudo conectar a SQL Server con autenticación de Windows.' `
        'Indica la instancia con: .\setup.ps1 -SqlServer "MI_SERVIDOR\INSTANCIA"'
}
Write-Ok "Conectado a $server"

# ------------------------------------------------------ 3. base de datos

Write-Step 3 'Preparando la base de datos'

$check = Invoke-Sql $server @('-h', '-1', '-W', '-Q',
    "SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID('$Database.dbo.Enrollment') IS NULL THEN 0 ELSE 1 END")
$exists = $check.ExitCode -eq 0 -and $check.Output.Trim() -eq '1'

if ($ResetDatabase -or -not $exists) {
    if ($exists) { Write-Warn 'Recreando la base de datos (-ResetDatabase): se borrarán los datos.' }
    $schema = Invoke-Sql $server @('-f', '65001', '-i', (Join-Path $Root 'database\01_schema.sql'))
    if ($schema.ExitCode -ne 0) { Stop-Setup 'Falló 01_schema.sql' $schema.Output }
    Write-Ok 'Esquema creado (01_schema.sql)'
}
else {
    Write-Ok "La base de datos $Database ya existe (usa -ResetDatabase para recrearla)"
}

$seed = Invoke-Sql $server @('-f', '65001', '-i', (Join-Path $Root 'database\02_seed.sql'))
if ($seed.ExitCode -ne 0) { Stop-Setup 'Falló 02_seed.sql' $seed.Output }
Write-Ok 'Catálogo cargado: 5 profesores, 10 materias (02_seed.sql)'

$verify = Invoke-Sql $server @('-f', '65001', '-W', '-i', (Join-Path $Root 'database\03_verify_rules.sql'))
if ($verify.ExitCode -ne 0 -or $verify.Output -match '\bFALLA\b') {
    Stop-Setup 'Las reglas de negocio no se verificaron en la base de datos.' $verify.Output
}
Write-Ok 'Reglas verificadas en la base de datos (03_verify_rules.sql)'

# ------------------------------------------------------ 4. backend

Write-Step 4 'Compilando el backend'

$previous = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$build = & dotnet build $Solution --nologo -v q 2>&1 | Out-String
$buildCode = $LASTEXITCODE
$ErrorActionPreference = $previous

$apiDll = Join-Path $ApiProject 'bin\Debug\net8.0\RegistroEstudiantes.Api.dll'
$locked = $build -match 'MSB3027|MSB3021|being used by another process'
if ($buildCode -eq 0) {
    Write-Ok 'Backend compilado'
}
elseif ($locked -and (Test-Path $apiDll)) {
    # Una API en ejecución bloquea sus .dll: se usa la versión ya compilada
    Write-Warn 'La API está en ejecución y bloquea sus archivos: se usa la versión ya compilada.'
}
else {
    Stop-Setup 'Falló la compilación del backend.' $build
}

if (-not $SkipTests) {
    # El proyecto de tests no depende de la API, así que compila aunque la API esté corriendo
    $testProject = Join-Path $Root 'backend\tests\RegistroEstudiantes.Tests'
    $ErrorActionPreference = 'Continue'
    $tests = & dotnet test $testProject --nologo -v q 2>&1 | Out-String
    $testCode = $LASTEXITCODE
    $ErrorActionPreference = $previous
    if ($testCode -ne 0) { Stop-Setup 'Fallaron los tests del backend.' $tests }
    $summary = ($tests -split "`n" | Where-Object { $_ -match 'Passed!|Failed!' } | Select-Object -First 1)
    Write-Ok "Tests: $($summary.Trim())"
}

# ------------------------------------------------------ 5. frontend

Write-Step 5 'Instalando dependencias del frontend'

if (-not (Test-Path (Join-Path $Frontend 'node_modules'))) {
    Write-Info 'npm ci (la primera vez tarda un poco)...'
    Push-Location $Frontend
    try {
        $ErrorActionPreference = 'Continue'
        & npm ci --no-audit --no-fund --loglevel=error
        $npmCode = $LASTEXITCODE
        $ErrorActionPreference = $previous
    }
    finally { Pop-Location }
    if ($npmCode -ne 0) { Stop-Setup 'Falló npm ci.' 'Revisa tu conexión a internet o el registro de npm.' }
    Write-Ok 'Dependencias instaladas'
}
else {
    Write-Ok 'Dependencias ya instaladas'
}

# ------------------------------------------------------ 6. ejecutar

$connectionString = "Server=$server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True"

if ($NoStart) {
    Write-Step 6 'Listo (sin iniciar, -NoStart)'
    Write-Host ''
    Write-Host '  Para iniciar manualmente:' -ForegroundColor White
    Write-Host "    API:  `$env:ConnectionStrings__Default='$connectionString'; dotnet run --project `"$ApiProject`""
    Write-Host "    Web:  cd `"$Frontend`"; npm start"
    exit 0
}

Write-Step 6 'Iniciando la aplicación'

if (Test-PortInUse $ApiPort) {
    Write-Warn "El puerto $ApiPort ya está en uso: se reutiliza la API que está corriendo."
}
else {
    $apiCommand = @"
`$host.UI.RawUI.WindowTitle = 'Registro de Estudiantes - API ($ApiPort)'
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ASPNETCORE_URLS = 'http://localhost:$ApiPort'
`$env:ConnectionStrings__Default = '$connectionString'
dotnet run --project '$ApiProject' --no-build --no-launch-profile
"@
    Start-Process powershell -ArgumentList '-NoExit', '-Command', $apiCommand | Out-Null
    Write-Info "Iniciando API en http://localhost:$ApiPort ..."
    if (-not (Wait-Port $ApiPort 60)) { Stop-Setup 'La API no respondió a tiempo.' 'Revisa la ventana "Registro de Estudiantes - API".' }
    Write-Ok "API: http://localhost:$ApiPort/swagger"
}

if (Test-PortInUse $WebPort) {
    Write-Warn "El puerto $WebPort ya está en uso: se reutiliza la web que está corriendo."
}
else {
    $webCommand = @"
`$host.UI.RawUI.WindowTitle = 'Registro de Estudiantes - Web ($WebPort)'
Set-Location '$Frontend'
npm start
"@
    Start-Process powershell -ArgumentList '-NoExit', '-Command', $webCommand | Out-Null
    Write-Info "Iniciando web en http://localhost:$WebPort (compila en unos segundos)..."
    if (-not (Wait-Port $WebPort 120)) { Stop-Setup 'La web no respondió a tiempo.' 'Revisa la ventana "Registro de Estudiantes - Web".' }
    Write-Ok "Web: http://localhost:$WebPort"
}

if (-not $NoBrowser) { Start-Process "http://localhost:$WebPort" }

Write-Host ''
Write-Host '=== Todo listo ===' -ForegroundColor Green
Write-Host "  Web:      http://localhost:$WebPort"
Write-Host "  Swagger:  http://localhost:$ApiPort/swagger"
Write-Host '  Cuentas demo: ana.garcia@registro.test (también luis.martinez, sofia.rodriguez, mateo.lopez)'
Write-Host '  Contraseña:   Demo1234'
Write-Host '  Para detener: cierra las ventanas de la API y la web.'
