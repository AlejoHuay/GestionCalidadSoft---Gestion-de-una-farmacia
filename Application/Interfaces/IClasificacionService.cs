using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Domain.Validators;
using System.Data;

namespace ProyectoArqSoft.Application.Interfaces

{
    public interface IClasificacionService
    {
        DataTable ObtenerTodos();
        DataTable ObtenerTodos(string filtro);
        Clasificacion? ObtenerPorId(int id);

        Result Crear(string nombre, string origen, string descripcion, int idUsuario);
        Result Actualizar(int id, string nombre, string origen, string descripcion, int idUsuario);
        Result EliminarLogicamente(int id, int idUsuario);
    }
}
