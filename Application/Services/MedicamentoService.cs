using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Domain.Validators;
using System.Data;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.DTOs;

namespace ProyectoArqSoft.Application.Services
{
    public class MedicamentoService : IMedicamentoService
    {
        private readonly IMedicamentoRepository _repository;
        private readonly IResult<Medicamento> _validador;

        private readonly IResult<MovimientoStockDTO> _movimientoStockValidacion;

        public MedicamentoService(
            IMedicamentoRepository repository,
            IResult<Medicamento> validador,
            IResult<MovimientoStockDTO> movimientoStockValidacion)
        {
            _repository = repository;
            _validador = validador;
            _movimientoStockValidacion = movimientoStockValidacion;
        }

        public DataTable ObtenerTodos()
        {
            return _repository.GetAll();
        }

        public DataTable ObtenerTodos(string filtro)
        {
            return _repository.GetAll(filtro);
        }

        public Medicamento? ObtenerPorId(int id)
        {
            return _repository.GetById(id);
        }

        public DataTable ObtenerDestacados()
        {
            return _repository.GetDestacados();
        }

        public Result Crear(
            string nombre,
            string presentacion,
            int idClasificacion,
            string concentracion,
            decimal precio,
            int stock,
            int idUsuario)
        {
            Medicamento medicamento = ConstruirMedicamento(
                0,
                nombre,
                presentacion,
                idClasificacion,
                concentracion,
                precio,
                stock);

            medicamento.IdUsuario = idUsuario;

            var validacion = _validador.Validar(medicamento);
            if (validacion.IsSuccess == false)
                return validacion;

            if (_repository.Insert(medicamento) <= 0)
                return Result.Fail("No se pudo registrar el medicamento.");

            return Result.Ok();
        }

        public Result Actualizar(MedicamentoActualizarDTO medicamento)
        {
            Medicamento medicamentoActualizado = ConstruirMedicamento(
                medicamento.Id,
                medicamento.Nombre,
                medicamento.Presentacion,
                medicamento.IdClasificacion,
                medicamento.Concentracion,
                medicamento.Precio,
                medicamento.Stock);

            medicamentoActualizado.IdUsuario = medicamento.IdUsuario;

            var validacion = _validador.Validar(medicamentoActualizado);
            if (validacion.IsSuccess == false)
                return validacion;

            if (_repository.Update(medicamentoActualizado) <= 0)
                return Result.Fail("No se pudo actualizar el medicamento.");

            return Result.Ok();
        }

        public Result EliminarLogicamente(int id, int idUsuario)
        {
            Medicamento medicamento = new Medicamento
            {
                Id = id,
                IdUsuario = idUsuario
            };

            if (_repository.Delete(medicamento) <= 0)
                return Result.Fail("No se pudo eliminar el medicamento.");

            return Result.Ok();
        }

        private Medicamento ConstruirMedicamento(
            int id,
            string nombre,
            string presentacion,
            int idClasificacion,
            string concentracion,
            decimal precio,
            int stock)
        {
            Medicamento medicamento = new Medicamento
            {
                Id = id,
                Nombre = nombre,
                Presentacion = presentacion,
                IdClasificacion = idClasificacion,
                Concentracion = concentracion,
                Precio = precio,
                Stock = stock
            };

            LimpiarCampos(medicamento);

            return medicamento;
        }

        private static void LimpiarCampos(Medicamento medicamento)
        {
            medicamento.Nombre = StringHelper.LimpiarEspacios(medicamento.Nombre);
            medicamento.Presentacion = StringHelper.LimpiarEspacios(medicamento.Presentacion);
            medicamento.Concentracion = StringHelper.LimpiarEspacios(medicamento.Concentracion);
        }

        public Result UpdateStock(int idMedicamento, int cantidad, bool esEntrada, int idUsuario)
        {
            Medicamento? medicamento = _repository.GetById(idMedicamento);

            if (medicamento == null)
            {
                return Result.Fail("El medicamento no existe o no está activo.");
            }

            MovimientoStockDTO movimiento = new MovimientoStockDTO
            {
                IdMedicamento = medicamento.Id,
                Cantidad = cantidad,
                EsEntrada = esEntrada,
                IdUsuario = idUsuario,
                StockActual = medicamento.Stock
            };

            Result validacion = _movimientoStockValidacion.Validar(movimiento);

            if (validacion.IsSuccess == false)
            {
                return validacion;
            }

            int filasAfectadas = _repository.UpdateStock(
                idMedicamento,
                cantidad,
                esEntrada,
                idUsuario
            );

            if (filasAfectadas <= 0)
            {
                return Result.Fail("No se pudo actualizar el stock del medicamento.");
            }

            return Result.Ok();
        }

    }
}


