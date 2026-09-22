using System.Data;

namespace ProyectoArqSoft.Domain.DTOs
{
    public class DashboardDto
    {
        public EstadisticasDto Estadisticas { get; set; } = new();
        public DataTable MedicamentosDestacados { get; set; } = new();
    }
}
