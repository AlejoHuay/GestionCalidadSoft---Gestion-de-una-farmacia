using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Domain.Validators;
using System.Data;
using ProyectoArqSoft.Application.Interfaces;

namespace ProyectoArqSoft.Application.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IRepository<Cliente> _repository;
        private readonly IResult<Cliente> _validador;

        public ClienteService(
            IRepository<Cliente> repository,
            IResult<Cliente> validador)
        {
            _repository = repository;
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

        public Cliente? ObtenerPorId(int id)
        {
            return _repository.GetById(id);
        }

        public Result Crear(
            bool esConsumidorFinal,
            string nit,
            string razonSocial,
            string? correoElectronico,
            int idUsuario)
        {
            Cliente cliente = ConstruirCliente(0, esConsumidorFinal, nit, razonSocial, correoElectronico);
            cliente.IdUsuario = idUsuario;

            var validacion = _validador.Validar(cliente);
            if (!validacion.IsSuccess)
                return validacion;

            LimpiarCampos(cliente);

            var validacionDuplicado = ValidarDuplicado(cliente);
            if (!validacionDuplicado.IsSuccess)
                return validacionDuplicado;

            if (_repository.Insert(cliente) <= 0)
                return Result.Fail("No se pudo registrar el cliente.");

            return Result.Ok();
        }

        public Result Actualizar(
            int id,
            bool esConsumidorFinal,
            string nit,
            string razonSocial,
            string? correoElectronico,
            int idUsuario)
        {
            Cliente cliente = ConstruirCliente(id, esConsumidorFinal, nit, razonSocial, correoElectronico);
            cliente.IdUsuario = idUsuario;

            var validacion = _validador.Validar(cliente);
            if (!validacion.IsSuccess)
                return validacion;

            LimpiarCampos(cliente);

            var validacionDuplicado = ValidarDuplicado(cliente);
            if (!validacionDuplicado.IsSuccess)
                return validacionDuplicado;

            if (_repository.Update(cliente) <= 0)
                return Result.Fail("No se pudo actualizar el cliente.");

            return Result.Ok();
        }

        public Result Eliminar(int id, int idUsuario)
        {
            Cliente cliente = new Cliente
            {
                IdCliente = id,
                IdUsuario = idUsuario
            };

            if (_repository.Delete(cliente) <= 0)
                return Result.Fail("No se pudo eliminar el cliente.");

            return Result.Ok();
        }

        private Cliente ConstruirCliente(
            int id,
            bool esConsumidorFinal,
            string nit,
            string razonSocial,
            string? correoElectronico)
        {
            Cliente cliente = new Cliente
            {
                IdCliente = id,
                Nit = nit,
                RazonSocial = razonSocial,
                CorreoElectronico = correoElectronico ?? string.Empty
            };

            AplicarConsumidorFinal(cliente, esConsumidorFinal);

            return cliente;
        }

        private static void LimpiarCampos(Cliente cliente)
        {
            cliente.Nit = StringHelper.QuitarEspacios(cliente.Nit);
            cliente.RazonSocial = StringHelper.LimpiarEspacios(cliente.RazonSocial);
            cliente.CorreoElectronico = StringHelper.QuitarEspacios(cliente.CorreoElectronico);
        }

        private Result ValidarDuplicado(Cliente cliente)
        {
            if (cliente.Nit.Equals("CF", StringComparison.OrdinalIgnoreCase))
                return Result.Ok();

            DataTable clientes = _repository.GetAll(cliente.Nit);

            foreach (DataRow row in clientes.Rows)
            {
                string nit = StringHelper.QuitarEspacios(row["nit"]?.ToString());
                int idCliente = Convert.ToInt32(row["id"]);

                if (nit.Equals(cliente.Nit, StringComparison.OrdinalIgnoreCase) &&
                    idCliente != cliente.IdCliente)
                {
                    return Result.Fail("Ya existe un cliente con ese NIT.");
                }
            }

            return Result.Ok();
        }

        private static void AplicarConsumidorFinal(Cliente cliente, bool esConsumidorFinal)
        {
            if (!esConsumidorFinal)
                return;

            cliente.Nit = "CF";
            cliente.RazonSocial = "Consumidor Final";
        }
    }
}



