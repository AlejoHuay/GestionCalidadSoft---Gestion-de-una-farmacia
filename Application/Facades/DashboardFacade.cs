using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Application.Services;
using ProyectoArqSoft.Domain.DTOs;

namespace ProyectoArqSoft.Application.Facades
{
    public class DashboardFacade : IDashboardFacade
    {
        private readonly EstadisticasService _estadisticasService;
        private readonly IMedicamentoService _medicamentoService;

        public DashboardFacade(
            EstadisticasService estadisticasService,
            IMedicamentoService medicamentoService)
        {
            _estadisticasService = estadisticasService;
            _medicamentoService = medicamentoService;
        }

        public DashboardDto ObtenerDashboardCompleto()
        {
            return new DashboardDto
            {
                Estadisticas = _estadisticasService.ObtenerEstadisticas(),
                MedicamentosDestacados = _medicamentoService.ObtenerDestacados()
            };
        }
    }


}
