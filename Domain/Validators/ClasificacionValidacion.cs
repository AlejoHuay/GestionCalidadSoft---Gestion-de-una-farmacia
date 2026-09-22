using System.Text.RegularExpressions;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Application.Interfaces;

namespace ProyectoArqSoft.Domain.Validators
{
    public class ClasificacionValidacion : IResult<Clasificacion>
    {
        public Result Validar(Clasificacion clasificacion)
        {
            return ValidarNombre(clasificacion.Nombre)
                ?? ValidarOrigen(clasificacion.Origen)
                ?? ValidarDescripcion(clasificacion.Descripcion)
                ?? Result.Ok();
        }

        private static Result? ValidarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return Result.Fail("El nombre de la clasificación es obligatorio.");

            nombre = nombre.Trim();

            if (nombre.Length < 3 || nombre.Length > 45)
                return Result.Fail("El nombre debe tener entre 3 y 45 caracteres.");

            string patron = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$";
            if (!Regex.IsMatch(nombre, patron, RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("El nombre contiene caracteres inválidos.");

            if (Regex.IsMatch(nombre, @"^(.)\1+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("El nombre no puede estar compuesto por un único carácter repetido.");

            return null;
        }

        private static Result? ValidarOrigen(string origen)
        {
            if (string.IsNullOrWhiteSpace(origen))
                return Result.Fail("El origen es obligatorio.");

            origen = origen.Trim();

            if (origen.Length < 3 || origen.Length > 45)
                return Result.Fail("El origen debe tener entre 3 y 45 caracteres.");

            string patron = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$";
            if (!Regex.IsMatch(origen, patron, RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("El origen contiene caracteres inválidos.");

            return null;
        }

        private static Result? ValidarDescripcion(string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                return Result.Fail("La descripción es obligatoria.");

            descripcion = descripcion.Trim();

            if (descripcion.Length < 5 || descripcion.Length > 100)
                return Result.Fail("La descripción debe tener entre 5 y 100 caracteres.");

            return null;
        }
    }
}
