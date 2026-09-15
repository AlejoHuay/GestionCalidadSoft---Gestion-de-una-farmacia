using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Domain.Validators;
using System.Data;

namespace ProyectoArqSoft.Application.Interfaces
{
    public interface IMedicamentoService
    {
        DataTable ObtenerTodos();
        DataTable ObtenerTodos(string filtro);
        Medicamento? ObtenerPorId(int id);
        DataTable ObtenerDestacados();

        Result Crear(
            string nombre,
            string presentacion,
            int idClasificacion,
            string concentracion,
            decimal precio,
            int stock,
            int idUsuario);

        Result Actualizar(MedicamentoActualizarDto medicamento);

        Result EliminarLogicamente(int id, int idUsuario);

        Result UpdateStock(int idMedicamento, int cantidad, bool esEntrada, int idUsuario);   
    }
}
