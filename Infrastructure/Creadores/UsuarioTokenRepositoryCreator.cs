using ProyectoArqSoft.Infrastructure.Persistence.Connection;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Persistence.Repositories;

namespace ProyectoArqSoft.Infrastructure.Creadores
{
    public class UsuarioTokenRepositoryCreator
    {
        private readonly PostgresDatabase database;
        public UsuarioTokenRepositoryCreator(PostgresDatabase database) => this.database = database;

        public IUsuarioTokenRepository CreateRepo()
        {
            return new UsuarioTokenRepository(database);
        }
    }
}
