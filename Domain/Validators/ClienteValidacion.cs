using System.Text.RegularExpressions;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Application.Interfaces;

namespace ProyectoArqSoft.Domain.Validators
{
    public class ClienteValidacion : IResult<Cliente>
    {
        public Result Validar(Cliente cliente)
        {
            if (EsConsumidorFinal(cliente))
                return Result.Ok();

            return ValidarNit(cliente.Nit)
                ?? ValidarRazonSocial(cliente.RazonSocial)
                ?? ValidarCorreoElectronico(cliente.CorreoElectronico)
                ?? Result.Ok();
        }

        private Result? ValidarNit(string nit)
        {
            Result? resultadoBasico = ValidarNitObligatorioYEspacios(nit);
            if (resultadoBasico != null)
                return resultadoBasico;

            Result? resultadoFormato = ValidarNitFormato(nit);
            if (resultadoFormato != null)
                return resultadoFormato;

            return ValidarNitLongitudYContenido(nit);
        }

        private static Result? ValidarNitObligatorioYEspacios(string nit)
        {
            if (nit == null || nit.Length == 0)
                return Result.Fail("El NIT es obligatorio.");

            if (string.IsNullOrWhiteSpace(nit))
                return Result.Fail("El NIT no debe contener solo espacios.");

            if (nit.Any(c => c == '\t' || c == '\n' || c == '\r'))
                return Result.Fail("El NIT no debe contener tabs ni saltos de linea.");

            if (!nit.Equals(nit.Trim(), StringComparison.Ordinal))
                return Result.Fail("El NIT no debe contener espacios al inicio o al final.");

            if (nit.Contains(' '))
                return Result.Fail("El NIT no debe contener espacios internos.");

            return null;
        }

        private static Result? ValidarNitFormato(string nit)
        {
            if (nit.StartsWith("+") || nit.StartsWith("-"))
                return Result.Fail("El NIT no debe contener signos positivos ni negativos.");

            if (Regex.IsMatch(nit, @"^\d+\.\d+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("El NIT no debe contener numeros decimales.");

            bool contieneLetras = nit.Any(char.IsLetter);
            bool contieneSimbolos = nit.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

            if (contieneLetras && contieneSimbolos)
                return Result.Fail("El NIT no debe mezclar letras con simbolos o caracteres especiales.");

            if (contieneLetras && nit.Any(char.IsWhiteSpace))
                return Result.Fail("El NIT no debe contener texto ni espacios.");

            if (contieneLetras)
                return Result.Fail("El NIT solo debe contener numeros, no letras.");

            if (contieneSimbolos)
                return Result.Fail("El NIT no debe contener simbolos ni caracteres especiales.");

            return null;
        }

        private static Result? ValidarNitLongitudYContenido(string nit)
        {
            if (nit.Length < 5)
                return Result.Fail("El NIT debe contener entre 5 y 12 digitos; faltan digitos.");

            if (nit.Length > 12)
                return Result.Fail("El NIT debe contener entre 5 y 12 digitos; sobran digitos.");

            if (!Regex.IsMatch(nit, @"^\d{5,12}$", RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("El NIT debe contener entre 5 y 12 digitos numericos.");

            if (nit.All(c => c == '0'))
                return Result.Fail("El NIT no puede estar compuesto solo por ceros.");

            return null;
        }

        private static Result? ValidarRazonSocial(string razonSocial)
        {
            if (string.IsNullOrWhiteSpace(razonSocial))
                return Result.Fail("La razon social es obligatoria.");

            if (!razonSocial.Equals(razonSocial.Trim(), StringComparison.Ordinal))
                return Result.Fail("La razon social no debe contener espacios al inicio o al final.");

            if (razonSocial.Any(c => c == '\t' || c == '\n' || c == '\r'))
                return Result.Fail("La razon social no debe contener tabs ni saltos de linea.");

            if (razonSocial.Length < 3 || razonSocial.Length > 45)
                return Result.Fail("La razon social debe tener entre 3 y 45 caracteres.");

            if (!Regex.IsMatch(razonSocial, @"^[\p{L}0-9\s\.\-&]+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("La razon social contiene caracteres no permitidos.");

            if (!razonSocial.Any(char.IsLetterOrDigit))
                return Result.Fail("La razon social debe contener al menos letras o numeros.");

            return null;
        }

        private static Result? ValidarCorreoElectronico(string? correoElectronico)
        {
            if (string.IsNullOrWhiteSpace(correoElectronico))
                return null;

            if (correoElectronico.Length > 45)
                return Result.Fail("El correo electronico no puede superar los 45 caracteres.");

            if (!Regex.IsMatch(correoElectronico, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("El correo electronico no tiene un formato valido.");

            return null;
        }

        private static bool EsConsumidorFinal(Cliente cliente)
        {
            string nit = cliente.Nit ?? string.Empty;
            string razonSocial = cliente.RazonSocial ?? string.Empty;

            return nit.Equals("CF", StringComparison.OrdinalIgnoreCase) &&
                   razonSocial.Equals("Consumidor Final", StringComparison.OrdinalIgnoreCase);
        }
    }
}
