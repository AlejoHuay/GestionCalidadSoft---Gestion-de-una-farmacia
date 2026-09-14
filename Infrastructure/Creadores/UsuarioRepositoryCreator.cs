using ProyectoArqSoft.Infrastructure.Persistence.Connection;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Persistence.Repositories;


namespace ProyectoArqSoft.Infrastructure.Creadores
{
    public class UsuarioRepositoryCreator
    {
        private readonly PostgresDatabase database;
        public UsuarioRepositoryCreator(PostgresDatabase database) => this.database = database;

        public IUsuarioRepository CreateRepo()
        {
            return new UsuarioRepository(database);
        }
    }
}
