[CmdletBinding()]
param([ValidateSet('Inspect','Schema','UpgradeEmpty','Export','Import','Verify')][string]$Accion = 'Inspect', [string]$Archivo)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$toolDirectory = Join-Path $projectRoot 'obj/PostgresMigration'
New-Item -ItemType Directory -Force -Path $toolDirectory | Out-Null
$project = @'
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
 <ItemGroup><PackageReference Include="Npgsql" Version="10.0.3"/><PackageReference Include="MySql.Data" Version="9.6.0"/><PackageReference Include="DotNetEnv" Version="3.1.1"/></ItemGroup>
</Project>
'@
[IO.File]::WriteAllText((Join-Path $toolDirectory 'PostgresMigration.csproj'),$project)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Migracion.cs.txt') -Destination (Join-Path $toolDirectory 'Program.cs') -Force
dotnet run --project (Join-Path $toolDirectory 'PostgresMigration.csproj') -- $Accion $projectRoot $Archivo
if ($LASTEXITCODE -ne 0) { throw "No se completo $Accion. Revisa el mensaje anterior." }
