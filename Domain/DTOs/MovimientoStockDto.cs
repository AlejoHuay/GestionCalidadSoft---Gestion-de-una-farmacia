namespace ProyectoArqSoft.Domain.DTOs
{
    public class MovimientoStockDto
    {
        public int IdMedicamento { get; set; }
        public int Cantidad { get; set; }
        public bool EsEntrada { get; set; } // true = suma, false = resta
        public int IdUsuario { get; set; }
        public int StockActual { get; set; }
    }

}