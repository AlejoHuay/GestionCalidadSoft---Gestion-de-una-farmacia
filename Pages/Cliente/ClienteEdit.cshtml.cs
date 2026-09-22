using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ProyectoArqSoft.Pages.Base;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.Validators;
using ClienteEntidad = ProyectoArqSoft.Domain.Models.Cliente;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoArqSoft.Pages
{
    [Authorize(Roles = "Admin, Bioquimico")]
    public class ClienteEditModel : BasePageModel
    {
        private readonly IClienteService clienteService;

        [BindProperty]
        public bool EsConsumidorFinal { get; set; }

        [BindProperty]
        public int IdCliente { get; set; }

        [BindProperty]
        [Display(Name = "NIT")]
        public string Nit { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "Razón Social")]
        public string RazonSocial { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "Correo Electrónico")]
        public string CorreoElectronico { get; set; } = string.Empty;

        public ClienteEditModel(IClienteService clienteService)
        {
            this.clienteService = clienteService;
        }

        public void OnGet()
        {
            // This page only needs to render the form when it is loaded directly.
        }

        public IActionResult OnPostCargarClienteParaEdicion(int id)
        {
            ClienteEntidad? cliente = clienteService.ObtenerPorId(id);

            if (cliente == null)
                return RedirectToPage("Cliente", new { error = "Cliente no encontrado" });

            IdCliente = cliente.IdCliente;
            Nit = cliente.Nit;
            RazonSocial = cliente.RazonSocial;
            CorreoElectronico = cliente.CorreoElectronico;
            EsConsumidorFinal =
                cliente.Nit.Equals("CF", StringComparison.OrdinalIgnoreCase) &&
                cliente.RazonSocial.Equals("Consumidor Final", StringComparison.OrdinalIgnoreCase);

            return Page();
        }

        public IActionResult OnPostActualizarCliente()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                Estado.MensajeError = "No se pudo identificar el usuario que realiza la operacion.";
                return Page();
            }

            Result resultado = clienteService.Actualizar(
                IdCliente,
                EsConsumidorFinal,
                Nit,
                RazonSocial,
                CorreoElectronico,
                idUsuario.Value);

            if (!resultado.IsSuccess)
            {
                Estado.MensajeError = resultado.Error;
                return Page();
            }

            return RedirectToPage("Cliente", new { mensaje = "Cliente actualizado correctamente" });
        }
    }
}



