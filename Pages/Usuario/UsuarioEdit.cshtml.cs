using Microsoft.AspNetCore.Mvc;
using ProyectoArqSoft.Pages.Base;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Domain.Validators;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoArqSoft.Pages.Usuario
{
    [Authorize(Roles = "Admin")]
    public class UsuarioEditModel : BasePageModel
    {
        private readonly IUsuarioService _usuarioService;

        [BindProperty]
        public UsuarioActualizarDto Input { get; set; } = new();

        public UsuarioEditModel(IUsuarioService usuarioService) => _usuarioService = usuarioService;

        public IActionResult OnPostCargarUsuarioParaEdicion(int id)
        {
            var user = _usuarioService.ObtenerUsuarioPorId(id);
            if (user == null) return RedirectToPage("Usuario", new { error = "Usuario no encontrado" });

            Input.IdUsuario = user.IdUsuario;
            Input.Email = user.Email;
            Input.Role = user.Role;
            Input.Activo = (byte)user.Activo;
            return Page();
        }

        public IActionResult OnPostActualizarUsuario()
        {
            int? idUsuarioSesion = HttpContext.Session.GetInt32("IdUsuario");

            Result resultado = _usuarioService.ActualizarAccesoUsuario(Input, idUsuarioSesion);

            if (resultado.IsSuccess == false)
            {
                Estado.MensajeError = resultado.Error;
                return Page();
            }

            return RedirectToPage("Usuario", new { mensaje = "Perfil de usuario actualizado correctamente" });
                }
    }
}
