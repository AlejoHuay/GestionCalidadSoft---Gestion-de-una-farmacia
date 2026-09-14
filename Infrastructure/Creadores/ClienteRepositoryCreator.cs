using ProyectoArqSoft.Infrastructure.Persistence.Connection;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Infrastructure.Persistence.Repositories;

namespace ProyectoArqSoft.Infrastructure.Creadores
{
    public class ClienteRepositoryCreator : RepositoryCreator<Cliente>
    {
        private readonly PostgresDatabase database;
        public ClienteRepositoryCreator(PostgresDatabase database) => this.database = database;

        public override IRepository<Cliente> CreateRepo()
        {
            return new ClienteRepository(database);
        }
    }
}

