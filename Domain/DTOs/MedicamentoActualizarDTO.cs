namespace ProyectoArqSoft.Domain.DTOs
{
    public class MedicamentoActualizarDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Presentacion { get; set; } = string.Empty;
        public int IdClasificacion { get; set; }
        public string Concentracion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int IdUsuario { get; set; }
    }
}
