using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Domain.Validators;
using ProyectoArqSoft.Infrastructure.Helpers;
using System.Data;

namespace ProyectoArqSoft.Application.Services

{
    public class ClasificacionService : IClasificacionService
    {
        private readonly IClasificacionRepository _repository;
        private readonly IResult<Clasificacion> _validador;

        public ClasificacionService(
            IClasificacionRepository repository,
            IResult<Clasificacion> validador)
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

        public Clasificacion? ObtenerPorId(int id)
        {
            return _repository.GetById(id);
        }

        public Result Crear(string nombre, string origen, string descripcion, int idUsuario)
        {
            Clasificacion clasificacion = ConstruirClasificacion(0, nombre, origen, descripcion);
            clasificacion.IdUsuario = idUsuario;

            Result validacion = _validador.Validar(clasificacion);
            if (!validacion.IsSuccess)
                return validacion;
            
            if (_repository.ExisteNombreActivo(clasificacion.Nombre))
                return Result.Fail("Ya existe una clasificación activa con ese nombre.");

            if (_repository.Insert(clasificacion) <= 0)
                return Result.Fail("No se pudo registrar la clasificación.");

            return Result.Ok();
        }

        public Result Actualizar(int id, string nombre, string origen, string descripcion, int idUsuario)
        {
            Clasificacion clasificacion = ConstruirClasificacion(id, nombre, origen, descripcion);
            clasificacion.IdUsuario = idUsuario;

            Result validacion = _validador.Validar(clasificacion);
            if (!validacion.IsSuccess)
                return validacion;

            if (_repository.ExisteNombreActivoExcluyendoId(id, clasificacion.Nombre))
                return Result.Fail("Ya existe otra clasificación activa con ese nombre.");

            if (_repository.Update(clasificacion) <= 0)
                return Result.Fail("No se pudo actualizar la clasificación.");

            return Result.Ok();
        }

        public Result EliminarLogicamente(int id, int idUsuario)
        {
            if (_repository.TieneMedicamentosActivosAsociados(id))
                return Result.Fail("No se puede eliminar la clasificación porque tiene medicamentos activos asociados.");

            Clasificacion clasificacion = new Clasificacion
            {
                Id = id,
                IdUsuario = idUsuario
            };

            if (_repository.Delete(clasificacion) <= 0)
                return Result.Fail("No se pudo eliminar la clasificación.");

            return Result.Ok();
        }

        private Clasificacion ConstruirClasificacion(int id, string nombre, string origen, string descripcion)
        {
            return new Clasificacion
            {
                Id = id,
                Nombre = StringHelper.LimpiarEspacios(nombre),
                Origen = StringHelper.LimpiarEspacios(origen),
                Descripcion = StringHelper.LimpiarEspacios(descripcion)
            };
        }
    }
}


