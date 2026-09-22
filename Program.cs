using Npgsql;
using ProyectoArqSoft.Infrastructure.Persistence.Connection;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProyectoArqSoft.Application.Facades;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Application.Services;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Domain.Validators;
using ProyectoArqSoft.Infrastructure.Creadores;
using ProyectoArqSoft.Infrastructure.Middleware;
using ProyectoArqSoft.Infrastructure.Persistence.Repositories;
using System.Text;

using ClasificacionEntidad = ProyectoArqSoft.Domain.Models.Clasificacion;
using ClienteEntidad = ProyectoArqSoft.Domain.Models.Cliente;
using MedicamentoEntidad = ProyectoArqSoft.Domain.Models.Medicamento;
using VentaEntidad = ProyectoArqSoft.Domain.Models.Venta;

Env.NoClobber().Load();

var builder = WebApplication.CreateBuilder(args);


// =========================
// CONFIGURACIÓN BASE
// =========================
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
var postgresConnection = builder.Configuration.GetConnectionString("PostgresConnection");
if (string.IsNullOrWhiteSpace(postgresConnection))
    throw new InvalidOperationException("Configura ConnectionStrings__PostgresConnection en .env.");
var postgresSettings = new NpgsqlConnectionStringBuilder(postgresConnection)
{
    SearchPath = "farmacia", Timezone = "UTC", ApplicationName = "VitalCare"
};
if (postgresSettings.Host?.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) == true)
{
    postgresSettings.SslMode = SslMode.VerifyFull;
    if (string.IsNullOrWhiteSpace(postgresSettings.RootCertificate))
        postgresSettings.RootCertificate = Path.Combine(builder.Environment.ContentRootPath, "database/postgresql/certs/supabase-ca.crt");
}
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(postgresSettings.ConnectionString));
builder.Services.AddScoped<PostgresDatabase>();


// =========================
// MEDICAMENTOS
// =========================
builder.Services.AddScoped<MedicamentoRepositoryCreator>();

builder.Services.AddScoped<IMedicamentoRepository, MedicamentoRepository>();

builder.Services.AddScoped<IRepository<MedicamentoEntidad>>(provider =>
{
    var creator = provider.GetRequiredService<MedicamentoRepositoryCreator>();
    return creator.CreateRepo();
});

builder.Services.AddScoped<IResult<MedicamentoEntidad>, MedicamentoValidacion>();
builder.Services.AddScoped<IMedicamentoService, MedicamentoService>();
builder.Services.AddScoped<IResult<MovimientoStockDto>, MovimientoStockValidacion>();


// =========================
// CLIENTES
// =========================
builder.Services.AddScoped<ClienteRepositoryCreator>();

builder.Services.AddScoped<IRepository<ClienteEntidad>>(provider =>
{
    var creator = provider.GetRequiredService<ClienteRepositoryCreator>();
    return creator.CreateRepo();
});

builder.Services.AddScoped<IResult<ClienteEntidad>, ClienteValidacion>();
builder.Services.AddScoped<IClienteService, ClienteService>();


// =========================
// USUARIOS
// =========================
builder.Services.AddScoped<UsuarioRepositoryCreator>();
builder.Services.AddScoped<UsuarioTokenRepositoryCreator>();

builder.Services.AddScoped<IUsuarioRepository>(provider =>
{
    var creator = provider.GetRequiredService<UsuarioRepositoryCreator>();
    return creator.CreateRepo();
});

builder.Services.AddScoped<IUsuarioTokenRepository>(provider =>
{
    var creator = provider.GetRequiredService<UsuarioTokenRepositoryCreator>();
    return creator.CreateRepo();
});

builder.Services.AddScoped<UsuarioValidacionGeneral>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();


// =========================
// CLASIFICACIÓN
// =========================
builder.Services.AddScoped<ClasificacionRepositoryCreator>();

builder.Services.AddScoped<IRepository<ClasificacionEntidad>>(provider =>
{
    var creator = provider.GetRequiredService<ClasificacionRepositoryCreator>();
    return creator.CreateRepo();
});

builder.Services.AddScoped<IClasificacionRepository, ClasificacionRepository>();
builder.Services.AddScoped<IResult<ClasificacionEntidad>, ClasificacionValidacion>();
builder.Services.AddScoped<IClasificacionService, ClasificacionService>();


// =========================
// VENTAS
// =========================
builder.Services.AddScoped<VentaRepositoryCreator>();

builder.Services.AddScoped<IVentaRepository>(provider =>
{
    var creator = provider.GetRequiredService<VentaRepositoryCreator>();
    return creator.CreateRepo();
});

builder.Services.AddScoped<IResult<VentaEntidad>, VentaValidacion>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<FachadaVenta>();
builder.Services.AddScoped<FachadaAnular>();
builder.Services.AddScoped<FachadaActualizarStock>();
builder.Services.AddScoped<IVentaFacade, VentaFacade>();
builder.Services.AddScoped<ComprobanteVentaPdfService>();


//repos
builder.Services.AddScoped<IMedicamentoRepository, MedicamentoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IClasificacionRepository, ClasificacionRepository>();


// =========================
// DASHBOARD / INDEX
// =========================
builder.Services.AddScoped<EstadisticasService>();
builder.Services.AddScoped<IDashboardFacade, DashboardFacade>();


// =========================
// AUTH / EMAIL / TOKEN
// =========================
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<TokenService>();


// =========================
// JWT
// =========================
string jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? string.Empty;
string jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? string.Empty;
string jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? string.Empty;

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("No se encontró JWT_KEY en el archivo .env.");

if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new InvalidOperationException("No se encontró JWT_ISSUER en el archivo .env.");

if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("No se encontró JWT_AUDIENCE en el archivo .env.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();


// =========================
// APP
// =========================
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseMiddleware<SessionTokenMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

await app.RunAsync();
