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
    public class ClienteModel : BasePageModel
    {
        private readonly IClienteService clienteService;

        public DataTable ClienteDataTable { get; set; } = new DataTable();

        public ClienteModel(IClienteService clienteService)
        {
            this.clienteService = clienteService;
        }

        public void OnGet(string? filtro, string? mensaje, string? error)
        {
            CargarParametros(filtro, mensaje, error);

            Result resultado = FiltroHelper.ValidarFiltro(Estado.FiltroActual);
            Estado.MensajeError = resultado.Error;

            if (!resultado.IsSuccess)
                return;

            CargarClientes(Estado.FiltroActual);
        }

        public IActionResult OnPostEliminarClienteLogicamente(int id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                Estado.MensajeError = "No se pudo identificar el usuario que realiza la operacion.";
                CargarClientes(Estado.FiltroActual);
                return Page();
            }

            Result resultado = clienteService.Eliminar(id, idUsuario.Value);

            if (!resultado.IsSuccess)
            {
                Estado.MensajeError = resultado.Error;
                return Page();
            }

            return RedirectToPage("Cliente", new { mensaje = "Cliente eliminado correctamente" });
        }

        private void CargarParametros(string? filtro, string? mensaje, string? error)
        {
            Estado.FiltroActual = FiltroHelper.LimpiarFiltro(filtro);
            Estado.Mensaje = mensaje ?? string.Empty;
            Estado.MensajeError = error ?? string.Empty;
        }

        private void CargarClientes(string filtro)
        {
            ClienteDataTable = clienteService.ObtenerTodos(filtro);
        }
    }
}



