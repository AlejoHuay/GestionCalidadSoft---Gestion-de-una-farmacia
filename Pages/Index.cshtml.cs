using Microsoft.AspNetCore.Mvc.RazorPages;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.DTOs;
using System.Data;

namespace ProyectoArqSoft.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IDashboardFacade _dashboardFacade;

        public string? Usuario { get; set; }
        public DataTable MedicamentoDataTable { get; set; } = new DataTable();
        public EstadisticasDto TotalFarmacia { get; set; } = new EstadisticasDto();

        public IndexModel(IDashboardFacade dashboardFacade)
        {
            _dashboardFacade = dashboardFacade;
        }

        public void OnGet()
        {
            Usuario = HttpContext.Session.GetString("UserName");

            DashboardDto dashboard = _dashboardFacade.ObtenerDashboardCompleto();

            TotalFarmacia = dashboard.Estadisticas;

            MedicamentoDataTable = dashboard.MedicamentosDestacados;
        }
    }
}
