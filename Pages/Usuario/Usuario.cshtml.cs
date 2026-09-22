using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.Validators;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Pages.Base;
using System.Data;

namespace ProyectoArqSoft.Pages.Usuario
{
    [Authorize(Roles = "Admin")]
    public class UsuarioModel : BasePageModel
    {
        private readonly IUsuarioService usuarioService;

        public DataTable UsuarioDataTable { get; set; } = new DataTable();

        public UsuarioModel(IUsuarioService usuarioService)
        {
            this.usuarioService = usuarioService;
        }

        public void OnGet(string? filtro, string? mensaje, string? error)
        {
            CargarParametros(filtro, mensaje, error);

            Result resultado = FiltroHelper.ValidarFiltro(Estado.FiltroActual);
            Estado.MensajeError = resultado.Error;

            if (!resultado.IsSuccess)
                return;

            CargarUsuarios(Estado.FiltroActual);
        }

        public IActionResult OnPostEliminarUsuarioLogicamente(int id)
        {
            int? idUsuarioSesion = HttpContext.Session.GetInt32("IdUsuario");

            // Ejecuta la baja l�gica (activo = 0)
            Result resultado = usuarioService.EliminarUsuario(id, idUsuarioSesion);

            if (!resultado.IsSuccess)
            {
                Estado.MensajeError = resultado.Error;
                CargarUsuarios(Estado.FiltroActual);
                return Page();
            }

            return RedirectToPage("Usuario", new { mensaje = "Usuario dado de baja correctamente" });
        }

        private void CargarParametros(string? filtro, string? mensaje, string? error)
        {
            Estado.FiltroActual = FiltroHelper.LimpiarFiltro(filtro);
            Estado.Mensaje = mensaje ?? string.Empty;
            Estado.MensajeError = error ?? string.Empty;
        }

        private void CargarUsuarios(string filtro)
        {
            UsuarioDataTable = usuarioService.ObtenerTodos(filtro);
        }
    }
}


