using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.Validators;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Pages.Base;
using System.Data;

namespace ProyectoArqSoft.Pages
{
    [Authorize(Roles = "Admin, Bioquimico")]
    public class ClasificacionModel : BasePageModel
    {
        private readonly IClasificacionService clasificacionService;

        public DataTable ClasificacionDataTable { get; set; } = new DataTable();

        public ClasificacionModel(IClasificacionService clasificacionService)
        {
            this.clasificacionService = clasificacionService;
        }

        public void OnGet(string? filtro, string? mensaje, string? error)
        {
            CargarParametros(filtro, mensaje, error);

            Result resultado = FiltroHelper.ValidarFiltro(Estado.FiltroActual);
            Estado.MensajeError = resultado.Error;

            if (!resultado.IsSuccess)
                return;

            CargarClasificaciones(Estado.FiltroActual);
        }

        public IActionResult OnPostEliminarClasificacionLogicamente(int id)
        {            
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                Estado.MensajeError = "No se pudo identificar el usuario que realiza la operación.";
                CargarClasificaciones(Estado.FiltroActual);
                return Page();
            }

            Result resultado = clasificacionService.EliminarLogicamente(id, idUsuario.Value);

            if (!resultado.IsSuccess)
            {
                Estado.MensajeError = resultado.Error;
                CargarClasificaciones(Estado.FiltroActual);
                return Page();
            }

            return RedirectToPage("Clasificacion", new { mensaje = "Clasificación eliminada correctamente" });
        }

        private void CargarParametros(string? filtro, string? mensaje, string? error)
        {
            Estado.FiltroActual = FiltroHelper.LimpiarFiltro(filtro);
            Estado.Mensaje = mensaje ?? string.Empty;
            Estado.MensajeError = error ?? string.Empty;
        }

        private void CargarClasificaciones(string filtro)
        {
            ClasificacionDataTable = clasificacionService.ObtenerTodos(filtro);
        }
    }
}


