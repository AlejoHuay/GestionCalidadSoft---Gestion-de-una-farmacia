using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Application.Services;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Domain.Validators;
using ProyectoArqSoft.Pages.Base;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace ProyectoArqSoft.Pages
{
    [Authorize(Roles = "Admin,Bioquimico")]
    public class VentaCreateModel : BasePageModel
    {
        private readonly IVentaFacade ventaFacade;
        private readonly IClienteService clienteService;
        private readonly IMedicamentoService medicamentoService;

        [BindProperty]
        public int IdCliente { get; set; }

        [BindProperty]
        [Display(Name = "Método de Pago")]
        public string MetodoPago { get; set; } = "Efectivo";

        [BindProperty]
        public string DetallesJson { get; set; } = "[]";

        [BindProperty]
        public bool ClienteModalEsConsumidorFinal { get; set; }

        [BindProperty]
        public string ClienteModalNit { get; set; } = string.Empty;

        [BindProperty]
        public string ClienteModalRazonSocial { get; set; } = string.Empty;

        [BindProperty]
        public string ClienteModalCorreoElectronico { get; set; } = string.Empty;

        public DataTable ClienteDataTable { get; set; } = new();
        public DataTable MedicamentoDataTable { get; set; } = new();

        public VentaCreateModel(
            IVentaFacade ventaFacade,
            IClienteService clienteService,
            IMedicamentoService medicamentoService)
        {
            this.ventaFacade = ventaFacade;
            this.clienteService = clienteService;
            this.medicamentoService = medicamentoService;
        }

        public void OnGet()
        {
            CargarCatalogos();
        }

        public IActionResult OnGetMedicamentoStock(int id)
        {
            var medicamento = medicamentoService.ObtenerPorId(id);

            if (medicamento == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return new JsonResult(new
                {
                    success = false,
                    error = "El medicamento no existe o no esta activo."
                });
            }

            return new JsonResult(new
            {
                success = true,
                medicamento = new
                {
                    id = medicamento.Id,
                    nombre = medicamento.Nombre,
                    precio = medicamento.Precio,
                    stock = medicamento.Stock
                }
            });
        }

        public IActionResult OnPostCrearVenta()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            string cajero = HttpContext.Session.GetString("UserName") ?? "cajero";

            if (idUsuario == null)
            {
                Estado.MensajeError = "No se pudo identificar el usuario que registra la venta.";
                CargarCatalogos();
                return Page();
            }

            List<DetalleVentaInputDto> detalles;
            try
            {
                detalles = JsonSerializer.Deserialize<List<DetalleVentaInputDto>>(DetallesJson) ?? new();

                detalles = detalles
                    .Where(x => x.IdMedicamento > 0 && x.Cantidad > 0)
                    .GroupBy(x => x.IdMedicamento)
                    .Select(g => new DetalleVentaInputDto
                    {
                        IdMedicamento = g.Key,
                        Cantidad = g.Sum(x => x.Cantidad)
                    })
                    .ToList();

                DetallesJson = JsonSerializer.Serialize(detalles);
            }
            catch
            {
                Estado.MensajeError = "El detalle de la venta no tiene un formato válido.";
                CargarCatalogos();
                return Page();
            }

            if (IdCliente <= 0)
            {
                Estado.MensajeError = "Debe seleccionar un cliente válido.";
                CargarCatalogos();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(MetodoPago))
            {
                Estado.MensajeError = "Debe seleccionar un método de pago.";
                CargarCatalogos();
                return Page();
            }

            if (!detalles.Any())
            {
                Estado.MensajeError = "Debe agregar al menos un medicamento.";
                CargarCatalogos();
                return Page();
            }

            foreach (var detalle in detalles)
            {
                if (detalle.IdMedicamento <= 0)
                {
                    Estado.MensajeError = "Uno de los medicamentos no fue seleccionado correctamente.";
                    CargarCatalogos();
                    return Page();
                }

                if (detalle.Cantidad <= 0)
                {
                    Estado.MensajeError = "La cantidad debe ser mayor a cero.";
                    CargarCatalogos();
                    return Page();
                }
            }

            Result resultado = ventaFacade.CrearVenta(
                IdCliente,
                idUsuario.Value,
                MetodoPago,
                detalles
            );

            if (!resultado.IsSuccess)
            {
                Estado.MensajeError = resultado.Error;
                CargarCatalogos();
                return Page();
            }

            var cliente = clienteService.ObtenerPorId(IdCliente);
            if (cliente == null)
            {
                Estado.MensajeError = "No se pudo obtener la información del cliente para el comprobante.";
                CargarCatalogos();
                return Page();
            }

            var comprobante = new ComprobanteVentaPdfDto
            {
                Fecha = DateTime.Now,
                Nit = cliente.Nit,
                RazonSocial = cliente.RazonSocial,
                Cajero = cajero
            };

            foreach (var item in detalles)
            {
                var medicamento = medicamentoService.ObtenerPorId(item.IdMedicamento);

                comprobante.Detalles.Add(new ComprobanteVentaDetallePdfDto
                {
                    Cantidad = item.Cantidad,
                    Descripcion = medicamento?.Nombre ?? $"Medicamento #{item.IdMedicamento}",
                    PrecioUnitario = medicamento?.Precio ?? 0
                });
            }

            comprobante.Total = comprobante.Detalles.Sum(x => x.Importe);

            byte[] pdf = ComprobanteVentaPdfService.Generar(comprobante);

            Response.Headers["X-Mensaje-Exito"] = "Venta registrada correctamente.";
            Response.Headers["X-Redirect-To"] =
                Url.Page("Venta", new { mensaje = "Venta registrada correctamente." }) ?? "/Venta?mensaje=Venta%20registrada%20correctamente.";

            Response.Headers["Content-Disposition"] = "inline";
                return File(pdf, "application/pdf");
        }

        public IActionResult OnPostCrearClienteModal()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return new JsonResult(new
                {
                    success = false,
                    error = "No se pudo identificar el usuario que realiza la operacion."
                });
            }

            Result resultado = clienteService.Crear(
                ClienteModalEsConsumidorFinal,
                ClienteModalNit,
                ClienteModalRazonSocial,
                ClienteModalCorreoElectronico,
                idUsuario.Value);

            if (!resultado.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return new JsonResult(new
                {
                    success = false,
                    error = resultado.Error
                });
            }

            string nitBuscado = ClienteModalEsConsumidorFinal ? "CF" : ClienteModalNit;
            DataTable clientes = clienteService.ObtenerTodos(nitBuscado);
            DataRow? clienteCreado = BuscarClientePorNit(clientes, nitBuscado);

            if (clienteCreado == null)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return new JsonResult(new
                {
                    success = false,
                    error = "El cliente fue registrado, pero no se pudo recuperar para seleccionarlo."
                });
            }

            return new JsonResult(new
            {
                success = true,
                cliente = new
                {
                    id = Convert.ToInt32(clienteCreado["id"]),
                    nit = clienteCreado["nit"]?.ToString() ?? string.Empty,
                    razonSocial = clienteCreado["razon_social"]?.ToString() ?? string.Empty
                }
            });
        }

        private void CargarCatalogos()
        {
            ClienteDataTable = ventaFacade.ObtenerClientes();
            MedicamentoDataTable = ventaFacade.ObtenerMedicamentos();
        }

        private static DataRow? BuscarClientePorNit(DataTable clientes, string nit)
        {
            foreach (DataRow row in clientes.Rows)
            {
                string nitFila = row["nit"]?.ToString()?.Trim() ?? string.Empty;

                if (nitFila.Equals(nit.Trim(), StringComparison.OrdinalIgnoreCase))
                    return row;
            }

            return null;
        }
    }
}