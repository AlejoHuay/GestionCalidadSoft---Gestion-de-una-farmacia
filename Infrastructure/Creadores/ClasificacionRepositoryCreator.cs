using ProyectoArqSoft.Infrastructure.Persistence.Connection;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Persistence.Repositories;

namespace ProyectoArqSoft.Infrastructure.Creadores
{
    public class ClasificacionRepositoryCreator : RepositoryCreator<Clasificacion>
    {
        private readonly PostgresDatabase database;
        public ClasificacionRepositoryCreator(PostgresDatabase database) => this.database = database;

        public override IRepository<Clasificacion> CreateRepo()
        {
            return new ClasificacionRepository(database);
        }
    }
}
