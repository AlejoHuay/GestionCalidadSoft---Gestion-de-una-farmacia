using Microsoft.AspNetCore.Mvc;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Pages.Base;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.Validators;
using System.Data;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoArqSoft.Pages
{
    [Authorize(Roles = "Admin, Bioquimico")]
    public class MedicamentoModel : BasePageModel
    {
        private readonly IMedicamentoService medicamentoService;

        public DataTable MedicamentoDataTable { get; set; } = new DataTable();

        public MedicamentoModel(IMedicamentoService medicamentoService)
        {
            this.medicamentoService = medicamentoService;
        }

        public void OnGet(string? filtro, string? mensaje, string? error)
        {
            CargarParametros(filtro, mensaje, error);

            Result resultado = FiltroHelper.ValidarFiltro(Estado.FiltroActual);
            Estado.MensajeError = resultado.Error;

            if (!resultado.IsSuccess)
                return;

            CargarMedicamentos(Estado.FiltroActual);
        }

        public IActionResult OnPostEliminarMedicamentoLogicamente(int id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                Estado.MensajeError = "No se pudo identificar el usuario que realiza la operación.";
                CargarMedicamentos(Estado.FiltroActual);
                return Page();
            }

            Result resultado = medicamentoService.EliminarLogicamente(id, idUsuario.Value);

            if (!resultado.IsSuccess)
            {
                Estado.MensajeError = resultado.Error;
                return Page();
            }

            return RedirectToPage();
        }

        private void CargarParametros(string? filtro, string? mensaje, string? error)
        {
            Estado.FiltroActual = FiltroHelper.LimpiarFiltro(filtro);
            Estado.Mensaje = mensaje ?? string.Empty;
            Estado.MensajeError = error ?? string.Empty;
        }

        private void CargarMedicamentos(string filtro)
        {
            MedicamentoDataTable = medicamentoService.ObtenerTodos(filtro);
        }
    }
}


