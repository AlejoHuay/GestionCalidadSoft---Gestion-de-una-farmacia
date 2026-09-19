using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace ProyectoArqSoft.Infrastructure.Helpers
{
    public static class CredencialesHelper
    {
        public static string GenerarUserName(string nombres, string apellidoPaterno, string ci)
        {
            string nombre = ObtenerPrimerNombre(nombres);
            string apellido = apellidoPaterno;
            string ciNormalizado = NormalizarCi(ci);

            string baseUser = $"{nombre}.{apellido}.{ciNormalizado}".ToLower();

            baseUser = QuitarTildes(baseUser);
            baseUser = Regex.Replace(baseUser, @"[^a-z0-9\.]", "", RegexOptions.None, TimeSpan.FromSeconds(1));

            return baseUser;
        }

        public static string GenerarPasswordTemporal(int longitud = 12)
        {
            const string mayus = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string minus = "abcdefghijklmnopqrstuvwxyz";
            const string numeros = "0123456789";
            const string simbolos = "!@#$%^&*()_-+=<>?";

            string todos = mayus + minus + numeros + simbolos;

            StringBuilder password = new StringBuilder();

            password.Append(GetRandomChar(mayus));
            password.Append(GetRandomChar(minus));
            password.Append(GetRandomChar(numeros));
            password.Append(GetRandomChar(simbolos));

            for (int i = password.Length; i < longitud; i++)
            {
                password.Append(GetRandomChar(todos));
            }

            return Mezclar(password.ToString());
        }

        private static char GetRandomChar(string caracteres)
        {
            int index = RandomNumberGenerator.GetInt32(caracteres.Length);
            return caracteres[index];
        }

        private static string Mezclar(string input)
        {
            char[] array = input.ToCharArray();

            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }

            return new string(array);
        }

        private static string ObtenerPrimerNombre(string nombres)
        {
            if (string.IsNullOrWhiteSpace(nombres))
                return string.Empty;

            return nombres.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        }

        private static string NormalizarCi(string ci)
        {
            if (string.IsNullOrWhiteSpace(ci))
                return string.Empty;

            ci = ci.Trim().ToUpper();

            // Quita guiones, espacios y cualquier cosa rara
            // Deja solo letras y números
            return Regex.Replace(ci, @"[^A-Z0-9]", "", RegexOptions.None, TimeSpan.FromSeconds(1));
        }

        private static string QuitarTildes(string texto)
        {
            return texto
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u")
                .Replace("Á", "A").Replace("É", "E").Replace("Í", "I")
                .Replace("Ó", "O").Replace("Ú", "U");
        }
    }
}
