using ProyectoArqSoft.Infrastructure.Persistence.Connection;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Persistence.Repositories;

namespace ProyectoArqSoft.Infrastructure.Creadores
{
    public class VentaRepositoryCreator
    {
        private readonly PostgresDatabase database;
        public VentaRepositoryCreator(PostgresDatabase database) => this.database = database;

        public IVentaRepository CreateRepo()
        {
            return new VentaRepository(database);
        }
    }
}
