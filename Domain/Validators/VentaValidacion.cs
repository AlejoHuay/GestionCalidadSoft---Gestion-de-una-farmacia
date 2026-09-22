using System.Text.RegularExpressions;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Application.Interfaces;

namespace ProyectoArqSoft.Domain.Validators
{
    public class VentaValidacion : IResult<Venta>
    {
        public Result Validar(Venta venta)
        {
            return ValidarCliente(venta.IdCliente)
                ?? ValidarUsuario(venta.IdUsuario)
                ?? ValidarMetodoPago(venta.MetodoPago)
                ?? ValidarDetalles(venta.Detalles)
                ?? ValidarTotal(venta.Total)
                ?? Result.Ok();
        }

        private static Result? ValidarCliente(int idCliente)
        {
            if (idCliente <= 0)
                return Result.Fail("La venta debe tener un cliente válido.");

            return null;
        }

        private static Result? ValidarUsuario(int idUsuario)
        {
            if (idUsuario <= 0)
                return Result.Fail("La venta debe tener un usuario registrador válido.");

            return null;
        }

        private static Result? ValidarMetodoPago(string metodoPago)
        {
            if (string.IsNullOrWhiteSpace(metodoPago))
                return Result.Fail("El método de pago es obligatorio.");

            metodoPago = metodoPago.Trim();

            if (metodoPago.Length > 45)
                return Result.Fail("El método de pago no puede exceder 45 caracteres.");

            return null;
        }

        private static Result? ValidarDetalles(List<DetalleVenta> detalles)
        {
            if (detalles == null || detalles.Count == 0)
                return Result.Fail("No se puede registrar una venta sin al menos un producto.");

            HashSet<int> medicamentos = new();

            foreach (DetalleVenta detalle in detalles)
            {
                if (detalle.IdMedicamento <= 0)
                    return Result.Fail("Todos los detalles deben tener un medicamento válido.");

                if (detalle.Cantidad <= 0)
                    return Result.Fail("La cantidad vendida debe ser mayor a 0.");

                if (detalle.PrecioUnitario <= 0)
                    return Result.Fail("El precio de venta debe ser mayor a 0.");

                if (detalle.Subtotal <= 0)
                    return Result.Fail("El subtotal debe ser mayor a 0.");

                if (!medicamentos.Add(detalle.IdMedicamento))
                    return Result.Fail("Un medicamento no puede repetirse dentro de la misma venta.");
            }

            return null;
        }

        private static Result? ValidarTotal(decimal total)
        {
            if (total <= 0)
                return Result.Fail("El total de la venta debe ser mayor a 0.");

            return null;
        }
    }
}
