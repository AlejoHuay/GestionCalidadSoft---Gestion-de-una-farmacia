using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ProyectoArqSoft.Pages.Base;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.Validators;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoArqSoft.Pages
{
    [Authorize(Roles = "Admin, Bioquimico")]
    public class ClienteCreateModel : BasePageModel
    {
        private readonly IClienteService clienteService;

        [BindProperty]
        public bool EsConsumidorFinal { get; set; }

        [BindProperty]
        [Display(Name = "NIT")]
        public string Nit { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "Razón Social")]
        public string RazonSocial { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "Correo Electrónico")]
        public string CorreoElectronico { get; set; } = string.Empty;

        public ClienteCreateModel(IClienteService clienteService)
        {
            this.clienteService = clienteService;
        }

        public void OnGet()
        {
            // This page only needs to render the empty creation form on GET.
        }

        public IActionResult OnPostCrearCliente()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                Estado.MensajeError = "No se pudo identificar el usuario que realiza la operacion.";
                return Page();
            }

            Result resultado = clienteService.Crear(
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

            return RedirectToPage("Cliente", new { mensaje = "Cliente registrado correctamente" });
        }
    }
}



