using Microsoft.AspNetCore.Mvc;
using ProyectoArqSoft.Pages.Base;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Domain.Validators;
using Microsoft.AspNetCore.Authorization;
using ProyectoArqSoft.Infrastructure.Helpers;

namespace ProyectoArqSoft.Pages.Usuario
{
    [Authorize(Roles = "Admin")]
    public class UsuarioCreateModel : BasePageModel
    {
        private readonly IUsuarioService usuarioService;

        [BindProperty] public string nombres { get; set; } = string.Empty;
        [BindProperty] public string apPaterno { get; set; } = string.Empty;
        [BindProperty] public string apMaterno { get; set; } = string.Empty;
        [BindProperty] public string ci { get; set; } = string.Empty;
        [BindProperty] public string? ciComplemento { get; set; }
        [BindProperty] public string ciExtencion { get; set; } = string.Empty;
        [BindProperty] public string telefono { get; set; } = string.Empty;
        [BindProperty] public string email { get; set; } = string.Empty;
        [BindProperty] public string role { get; set; } = "Bioquimico";

        public UsuarioCreateModel(IUsuarioService usuarioService) => this.usuarioService = usuarioService;

        public IActionResult OnPostCrearUsuario()
        {
            int? idUsuarioSesion = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuarioSesion == null)
            {
                Estado.MensajeError = "No se pudo identificar el usuario que realiza la operacion.";
                return Page();
            }

            var username = CredencialesHelper.GenerarUserName(
                nombres,
                apPaterno,
                ci
            );
            var passAleatorio = CredencialesHelper.GenerarPasswordTemporal();


            var dto = new UsuarioRegistroDto
            {
                Nombres = nombres,
                ApellidoPaterno = apPaterno,
                ApellidoMaterno = apMaterno,
                Ci = ci,
                CiExtencion = ciExtencion,
                Telefono = telefono,
                Email = email,
                UserName = username,
                Password = passAleatorio
            };

            Result resultado = usuarioService.CrearUsuario(dto, role, idUsuarioSesion);

            if (!resultado.IsSuccess)
            {
                Estado.MensajeError = resultado.Error;
                return Page();
            }

            return RedirectToPage("Usuario", new { mensaje = $"Usuario registrado. Username: {username}. Credenciales enviadas por mail." });
        }
    }
}



