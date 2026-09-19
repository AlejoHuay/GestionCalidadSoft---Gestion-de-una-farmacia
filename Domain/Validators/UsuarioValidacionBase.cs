using System.Text.RegularExpressions;

namespace ProyectoArqSoft.Domain.Validators
{
    public abstract class UsuarioValidacionBase
    {
        protected static readonly HashSet<string> ExtensionesValidas = new(StringComparer.OrdinalIgnoreCase)
        {
            "LP", "CB", "SC", "OR", "PT", "TJ", "CH", "BE", "PD"
        };

        protected static Result? Requerido(string? valor, string mensaje)
        {
            return string.IsNullOrWhiteSpace(valor) ? Result.Fail(mensaje) : null;
        }

        protected static Result? Maximo(string valor, int max, string mensaje)
        {
            return valor.Length > max ? Result.Fail(mensaje) : null;
        }

        protected static Result? RegexValido(string valor, string patron, string mensaje)
        {
            return Regex.IsMatch(valor, patron, RegexOptions.None, TimeSpan.FromSeconds(1)) ? null : Result.Fail(mensaje);
        }

        protected static Result? EmailValido(string email, bool obligatorio = true)
        {
            email = email?.Trim() ?? string.Empty;

            if (obligatorio && string.IsNullOrWhiteSpace(email))
                return Result.Fail("El correo electrónico es obligatorio.");

            if (string.IsNullOrWhiteSpace(email))
                return null;

            if (email.Length > 100)
                return Result.Fail("El correo electrónico no puede tener más de 100 caracteres.");

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("El formato del correo electrónico no es válido.");

            return null;
        }

        protected static Result? TextoRequerido(string valor, string campo, int maximo)
        {
            return Requerido(valor, $"El campo {campo} es obligatorio.")
                ?? Maximo(valor, maximo, $"El campo {campo} no puede tener más de {maximo} caracteres.");
        }

        protected static Result? TextoOpcional(string valor, string campo, int maximo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return Maximo(valor, maximo, $"El campo {campo} no puede tener más de {maximo} caracteres.");
        }
    }
}
