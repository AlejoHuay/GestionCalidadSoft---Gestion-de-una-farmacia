using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Domain.Models;

namespace ProyectoArqSoft.Application.Ports.Output
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        int Update(Usuario usuario, int? idUsuarioSesion);

        Usuario? GetByEmail(string email);
        Usuario? GetByUserName(string userName);
        bool ExisteEmail(string email);
        bool ExisteUserName(string userName);
        int CambiarPassword(int idUsuario, string nuevoPasswordHash, bool mustChangePassword);
        int ActivarCuentaConToken(int idUsuario, int idToken, string passwordHash);

        int UpdateDatosEdicion(Usuario usuario, int? idUsuarioSesion);
        int Count();
        int SoftDelete(Usuario usuario, int? idUsuarioSesion);

    }
}
