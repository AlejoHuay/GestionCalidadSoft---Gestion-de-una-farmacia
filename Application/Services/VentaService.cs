using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Domain.Validators;
using System.Data;

namespace ProyectoArqSoft.Application.Services
{
    public class VentaService : IVentaService
    {
        private readonly IVentaRepository _repository;
        private readonly IResult<Venta> _validador;
        private readonly IMedicamentoRepository _medicamentoRepository;

        public VentaService(
            IVentaRepository repository,
            IMedicamentoRepository medicamentoRepository,
            IResult<Venta> validador)
        {
            _repository = repository;
            _medicamentoRepository = medicamentoRepository;
            _validador = validador;
        }


        public DataTable ObtenerTodos()
        {
            return _repository.GetAll();
        }

        public DataTable ObtenerTodos(string filtro)
        {
            return _repository.GetAll(filtro);
        }

        public Venta? ObtenerPorId(int id)
        {
            return _repository.GetById(id);
        }

        public List<DetalleVenta> ObtenerDetallesPorVenta(int idVenta)
        {
            return _repository.GetDetallesByVentaId(idVenta);
        }

        public Result Crear(
            int idCliente,
            int idUsuario,
            string metodoPago,
            List<DetalleVentaInputDto> detallesInput)
        {
            try
            {
                Venta venta = ConstruirVenta(
                    id: 0,
                    idCliente: idCliente,
                    idUsuario: idUsuario,
                    metodoPago: metodoPago,
                    detallesInput: detallesInput,
                    idUsuarioEditor: null);


                Result validacion = _validador.Validar(venta);

                if (!validacion.IsSuccess)
                    return validacion;

                return _repository.RegistrarVenta(venta);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }


        public Result Actualizar(
            int idVenta,
            int idCliente,
            string metodoPago,
            List<DetalleVentaInputDto> detallesInput,
            int idUsuarioEditor)
        {
            try
            {
                Venta ventaExistente = _repository.GetById(idVenta) ?? new Venta();

                if (ventaExistente.Id == 0)
                    return Result.Fail("La venta no existe.");

                if (ventaExistente.Estado == 0)
                    return Result.Fail("No se puede modificar una venta anulada.");

                Venta venta = ConstruirVenta(
                    id: idVenta,
                    idCliente: idCliente,
                    idUsuario: ventaExistente.IdUsuario,
                    metodoPago: metodoPago,
                    detallesInput: detallesInput,
                    idUsuarioEditor: idUsuarioEditor);

                Result validacion = _validador.Validar(venta);

                if (!validacion.IsSuccess)
                    return validacion;

                return _repository.ActualizarVenta(venta);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }


        public Result EliminarLogicamente(int idVenta, int idUsuarioEditor)
        {
            return _repository.AnularVentaLogicamente(idVenta, idUsuarioEditor);
        }

        private Venta ConstruirVenta(
            int id,
            int idCliente,
            int idUsuario,
            string metodoPago,
            List<DetalleVentaInputDto> detallesInput,
            int? idUsuarioEditor)
        {
            if (detallesInput == null || !detallesInput.Any())
                throw new InvalidOperationException("Debe agregar al menos un medicamento.");

            Venta venta = new Venta
            {
                Id = id,
                IdCliente = idCliente,
                IdUsuario = idUsuario,
                IdUsuarioEditor = idUsuarioEditor,
                MetodoPago = StringHelper.LimpiarEspacios(metodoPago),
                Detalles = new List<DetalleVenta>()
            };

            var preciosAnteriores = id > 0
                ? _repository.GetDetallesByVentaId(id).ToDictionary(d => d.IdMedicamento, d => d.PrecioUnitario)
                : new Dictionary<int, decimal>();
            foreach (DetalleVentaInputDto item in detallesInput)
            {
                var medicamento = _medicamentoRepository.GetById(item.IdMedicamento);

                if (medicamento == null)
                    throw new InvalidOperationException(
                        $"Medicamento con ID {item.IdMedicamento} no encontrado.");

                if (id == 0 && item.Cantidad > medicamento.Stock)
                    throw new InvalidOperationException(
                        $"Stock insuficiente para {medicamento.Nombre}. Disponible: {medicamento.Stock}.");

                decimal precioReal = preciosAnteriores.GetValueOrDefault(item.IdMedicamento, medicamento.Precio);

                DetalleVenta detalle = new DetalleVenta
                {
                    IdMedicamento = item.IdMedicamento,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precioReal,
                };

                venta.Detalles.Add(detalle);
            }

            venta.Total = venta.Detalles.Sum(x => x.Subtotal);

            return venta;
        }
    }
}


