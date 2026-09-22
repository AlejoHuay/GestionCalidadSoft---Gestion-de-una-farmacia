using System.Text.RegularExpressions;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Application.Interfaces;

namespace ProyectoArqSoft.Domain.Validators
{
    public class MedicamentoValidacion : IResult<Medicamento>
    {
        public Result Validar(Medicamento medicamento)
        {
            return ValidarNombre(medicamento.Nombre)
                ?? ValidarPresentacion(medicamento.Presentacion)
                ?? ValidarIdClasificacion(medicamento.IdClasificacion)
                ?? ValidarConcentracion(medicamento.Concentracion)
                ?? ValidarPrecio(medicamento.Precio)
                ?? ValidarStock(medicamento.Stock)
                ?? Result.Ok();
        }

        private Result? ValidarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return Result.Fail("El nombre es obligatorio.");

            if (!EsNombreValido(nombre))
                return Result.Fail("El nombre contiene caracteres inválidos o no tiene un formato correcto.");

            return null;
        }

        private static Result? ValidarPresentacion(string presentacion)
        {
            return string.IsNullOrWhiteSpace(presentacion)
                ? Result.Fail("La presentación es obligatoria.")
                : null;
        }

        private static Result? ValidarIdClasificacion(int idClasificacion)
        {
            if (idClasificacion <= 0)
                return Result.Fail("La clasificación es obligatoria.");

            return null;
        }

        private Result? ValidarConcentracion(string concentracion)
        {
            if (string.IsNullOrWhiteSpace(concentracion))
                return Result.Fail("La concentración es obligatoria.");

            if (!EsConcentracionValida(concentracion))
                return Result.Fail("La concentración no tiene un formato válido. Ejemplos: 500 mg, 250 mg/5ml, 0.9 %.");

            return null;
        }

        private static Result? ValidarPrecio(decimal precio)
        {
            if (precio <= 0)
                return Result.Fail("El precio debe ser mayor a 0 Bs.");

            if (precio > 1000)
                return Result.Fail("El precio no puede ser mayor a 1000 Bs.");

            return null;
        }

        private static Result? ValidarStock(int stock)
        {
            if (stock < 0)
                return Result.Fail("El stock no puede ser negativo.");

            if (stock > 100000)
                return Result.Fail("El stock no puede ser mayor a 100000 items.");

            return null;
        }

        private static bool EsConcentracionValida(string concentracion)
        {
            string patron = @"^\d+(\.\d+)?\s?(mg|g|mcg|ml|%)\s*(\/\s*(\d+(\.\d+)?)?\s?(ml|l))?$";
            return Regex.IsMatch(concentracion.Trim(), patron, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }

        private static bool EsNombreValido(string nombre)
        {
            nombre = nombre.Trim();

            string patron = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ0-9\s]+$";

            if (!Regex.IsMatch(nombre, patron, RegexOptions.None, TimeSpan.FromSeconds(1)))
                return false;

            if (nombre.Length < 3 || nombre.Length > 100)
                return false;

            if (Regex.IsMatch(nombre, @"^(.)\1+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
                return false;

            return true;
        }
    }
}
