using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Pages.Base;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using DetalleVenta = ProyectoArqSoft.Domain.Models.DetalleVenta;
using VentaEntidad = ProyectoArqSoft.Domain.Models.Venta;

namespace ProyectoArqSoft.Pages
{
    [Authorize(Roles = "Admin,Bioquimico")]
    public class VentaUpdateModel : BasePageModel
    {
        private readonly IVentaFacade ventaFacade;

        [BindProperty]
        public int IdVenta { get; set; }

        [BindProperty]
        public int IdCliente { get; set; }

        [BindProperty]
        [Display(Name = "Método de Pago")] // Corrección de tilde
        public string MetodoPago { get; set; } = "Efectivo"; // Valor por defecto inicial

        [BindProperty]
        public string DetallesJson { get; set; } = "[]";

        public DataTable ClienteDataTable { get; set; } = new DataTable();
        public DataTable MedicamentoDataTable { get; set; } = new DataTable();

        public VentaUpdateModel(IVentaFacade ventaFacade)
        {
            this.ventaFacade = ventaFacade;
        }

        public void OnGet()
        {
            CargarCatalogos();
        }

        public IActionResult OnPostCargarVenta(int id)
        {
            VentaEntidad? venta = ventaFacade.ObtenerVentaPorId(id);

            if (venta == null)
                return RedirectToPage("Venta", new { error = "Venta no encontrada." });

            List<DetalleVenta> detalles = ventaFacade.ObtenerDetalles(id);

            IdVenta = venta.Id;
            IdCliente = venta.IdCliente;

            // Asigna el valor por defecto "Efectivo" si la BD trae el dato nulo o vacío
            MetodoPago = string.IsNullOrWhiteSpace(venta.MetodoPago) ? "Efectivo" : venta.MetodoPago;

            List<DetalleVentaDto> detallesInput = detalles.Select(x => new DetalleVentaDto
            {
                IdMedicamento = x.IdMedicamento,
                Cantidad = x.Cantidad,
                PrecioUnitario = x.PrecioUnitario
            }).ToList();

            DetallesJson = JsonSerializer.Serialize(detallesInput);

            CargarCatalogos();
            return Page();
        }

        public IActionResult OnPostActualizarVenta()
        {
            var editor = HttpContext.Session.GetInt32("IdUsuario");
            if (editor == null) return RedirectToPage("/Auth/Login");
            try
            {
                var detalles = JsonSerializer.Deserialize<List<DetalleVentaInputDto>>(DetallesJson) ?? [];
                var resultado = ventaFacade.ActualizarVenta(IdVenta, IdCliente, MetodoPago, detalles, editor.Value);
                if (!resultado.IsSuccess)
                {
                    Estado.MensajeError = resultado.Error;
                    CargarCatalogos();
                    return Page();
                }
            }
            catch (JsonException)
            {
                Estado.MensajeError = "El detalle de la venta no es válido.";
                CargarCatalogos();
                return Page();
            }
            return RedirectToPage("Venta", new { mensaje = "Venta actualizada correctamente." });
        }

        private void CargarCatalogos()
        {
            ClienteDataTable = ventaFacade.ObtenerClientes();
            MedicamentoDataTable = ventaFacade.ObtenerMedicamentos();
        }
    }
}
