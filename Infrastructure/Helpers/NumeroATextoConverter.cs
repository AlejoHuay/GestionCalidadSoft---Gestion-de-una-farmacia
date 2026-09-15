namespace ProyectoArqSoft.Application.Helpers
{
    public static class NumeroATextoConverter
    {
        private static readonly string[] Unidades =
        {
            "", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
            "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete",
            "dieciocho", "diecinueve"
        };

        private static readonly string[] Decenas =
        {
            "", "", "veinte", "treinta", "cuarenta", "cincuenta",
            "sesenta", "setenta", "ochenta", "noventa"
        };

        private static readonly string[] Centenas =
        {
            "", "ciento", "doscientos", "trescientos", "cuatrocientos",
            "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos"
        };

        public static string ConvertirDecimalATexto(decimal cantidad)
        {
            int numero = (int)cantidad;
            string texto = "";

            if (numero == 0)
                return "cero";

            if (numero >= 1000)
            {
                texto += ConvertirDecimalATexto(numero / 1000) + " mil ";
                numero %= 1000;
            }

            if (numero == 100)
                return (texto + "cien").Trim();

            if (numero >= 100)
            {
                texto += Centenas[numero / 100] + " ";
                numero %= 100;
            }

            if (numero >= 20)
            {
                int decena = numero / 10;
                int unidad = numero % 10;

                texto += Decenas[decena];

                if (unidad > 0)
                    texto += " y " + Unidades[unidad];

                return texto.Trim();
            }

            if (numero > 0)
            {
                texto += Unidades[numero];
            }

            return texto.Trim();
        }
    }
}