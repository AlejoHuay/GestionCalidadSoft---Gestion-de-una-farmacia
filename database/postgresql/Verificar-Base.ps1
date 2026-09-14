[CmdletBinding()]
param()
# Usa una base LOCAL DE PRUEBAS con 01 aplicado y SIN usuarios/datos iniciales.
# La conexion se recibe en VITALCARE_TEST_CONNECTION; nunca se imprime.
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($env:VITALCARE_TEST_CONNECTION)) {
    throw 'Define VITALCARE_TEST_CONNECTION apuntando a una base local de pruebas vacia con el esquema 01.'
}
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$toolDirectory = Join-Path $projectRoot 'obj/PostgresVerification'
New-Item -ItemType Directory -Force -Path $toolDirectory | Out-Null
$reference = [System.Security.SecurityElement]::Escape((Join-Path $projectRoot 'ProyectoArqSoft.csproj'))
$projectText = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><ProjectReference Include="$reference" /></ItemGroup>
</Project>
"@
[System.IO.File]::WriteAllText((Join-Path $toolDirectory 'PostgresVerification.csproj'), $projectText)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Verificacion.cs.txt') -Destination (Join-Path $toolDirectory 'Program.cs') -Force
dotnet run --project (Join-Path $toolDirectory 'PostgresVerification.csproj') -- $projectRoot
if ($LASTEXITCODE -ne 0) { throw 'Fallo la verificacion de PostgreSQL; revisa el resultado anterior.' }
