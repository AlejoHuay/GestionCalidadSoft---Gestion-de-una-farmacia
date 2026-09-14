[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$bcryptAssembly = Join-Path $projectRoot 'bin/Debug/net9.0/BCrypt.Net-Next.dll'
if (-not (Test-Path -LiteralPath $bcryptAssembly)) {
    dotnet build (Join-Path $projectRoot 'ProyectoArqSoft.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el proyecto.' }
}
$toolDirectory = Join-Path $projectRoot 'obj/HashAdmin'
New-Item -ItemType Directory -Force -Path $toolDirectory | Out-Null
$escapedAssembly = [System.Security.SecurityElement]::Escape($bcryptAssembly)
$sdkVersion = dotnet --version
if ($LASTEXITCODE -ne 0) { throw 'No se encontro un SDK .NET.' }
$sdkMajor = [int]($sdkVersion.Split('.')[0])
if ($sdkMajor -lt 9) { throw 'Se necesita un SDK .NET 9 o superior.' }
$toolFramework = "net$sdkMajor.0"
$projectText = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>$toolFramework</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><Reference Include="BCrypt.Net-Next"><HintPath>$escapedAssembly</HintPath></Reference></ItemGroup>
</Project>
"@
$sourceText = @'
using System.Text;
Console.Error.Write("Contrasena nueva del administrador (no se muestra): ");
var buffer = new StringBuilder();
while (true)
{
    var key = Console.ReadKey(intercept: true);
    if (key.Key == ConsoleKey.Enter) break;
    if (key.Key == ConsoleKey.Backspace) { if (buffer.Length > 0) buffer.Length--; }
    else if (!char.IsControl(key.KeyChar)) buffer.Append(key.KeyChar);
}
Console.Error.WriteLine();
var password = buffer.ToString();
if (password.Length < 12 || Encoding.UTF8.GetByteCount(password) > 72 || password != password.Trim())
{
    Console.Error.WriteLine("Usa al menos 12 caracteres, hasta 72 bytes UTF-8, sin espacios exteriores.");
    Environment.Exit(1);
}
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
if (!BCrypt.Net.BCrypt.Verify(password, hash)) throw new Exception("Fallo la verificacion bcrypt.");
Console.WriteLine("SET @admin_password_hash = '" + hash + "';");
'@
[System.IO.File]::WriteAllText((Join-Path $toolDirectory 'HashAdmin.csproj'), $projectText)
[System.IO.File]::WriteAllText((Join-Path $toolDirectory 'Program.cs'), $sourceText)
[System.IO.File]::WriteAllText((Join-Path $toolDirectory 'NuGet.Config'), '<configuration><packageSources><clear /></packageSources></configuration>')
dotnet restore (Join-Path $toolDirectory 'HashAdmin.csproj') --configfile (Join-Path $toolDirectory 'NuGet.Config') --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar la herramienta.' }
dotnet run --project (Join-Path $toolDirectory 'HashAdmin.csproj') --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'No se pudo generar el hash.' }
