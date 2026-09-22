using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Infrastructure.Persistence.Repositories;

namespace ProyectoArqSoft.Application.Services
{
    public class EstadisticasService
    {
        private readonly IMedicamentoRepository _medicamentoRepo;
        private readonly IClienteRepository _clienteRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IVentaRepository _ventaRepo;

        public EstadisticasService(
            IMedicamentoRepository medicamentoRepo,
            IClienteRepository clienteRepo,
            IUsuarioRepository usuarioRepo,
            IVentaRepository ventaRepo)
        {
            _medicamentoRepo = medicamentoRepo;
            _clienteRepo = clienteRepo;
            _usuarioRepo = usuarioRepo;
            _ventaRepo = ventaRepo;
        }

        public EstadisticasDto ObtenerEstadisticas()
        {
            return new EstadisticasDto
            {
                TotalMedicamentos = _medicamentoRepo.Count(),
                TotalClientes = _clienteRepo.Count(),
                TotalUsuarios = _usuarioRepo.Count(),
                TotalVentas = _ventaRepo.Count()
            };
        }
    }
}
