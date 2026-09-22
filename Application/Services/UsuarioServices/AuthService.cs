using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.Validators;

namespace ProyectoArqSoft.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ITokenService _tokenService;

        public AuthService(
            IUsuarioRepository usuarioRepository,
            ITokenService tokenService)
        {
            _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public Result IniciarSesion(UsuarioLoginRequestDto dto, out UsuarioLoginResponseDto? respuesta)
        {
            respuesta = null;

            Result validacion = ValidarLoginDto(dto);
            if (!validacion.IsSuccess)
                return validacion;

            string emailOUserName = dto.EmailOUserName.Trim();
            string password = dto.Password.Trim();

            Usuario? usuario = BuscarPorEmailOUserName(emailOUserName);
            if (usuario == null)
                return Result.Fail("Las credenciales son incorrectas.");

            if (usuario.Activo == 0)
                return Result.Fail("El usuario se encuentra inactivo.");

            bool passwordValido = PasswordHelper.Verify(password, usuario.PasswordHash);
            if (!passwordValido)
                return Result.Fail("Las credenciales son incorrectas.");

            var tokenGeneracionDto = new UsuarioTokenGeneracionDto
            {
                IdUsuario = usuario.IdUsuario,
                TipoToken = "INICIO_SESION",  
                MinutosExpiracion = 60,
                UserName = usuario.UserName,
                Role = usuario.Role
            };

            (Result resultado, string token) = _tokenService.GenerarToken(tokenGeneracionDto, out _);

            if (!resultado.IsSuccess) return resultado;

            respuesta = new UsuarioLoginResponseDto
            {
                IdUsuario = usuario.IdUsuario,
                UserName = usuario.UserName ?? string.Empty,
                Role = usuario.Role ?? string.Empty,
                MustChangePassword = usuario.MustChangePassword == 1,
                Token = token,
                ExpiraEn = 60  // Esto se ajusta según lo que se define en el servicio
            };

            return Result.Ok();
        }

        private static Result ValidarLoginDto(UsuarioLoginRequestDto? dto)
        {
            if (dto == null)
                return Result.Fail("Los datos de acceso no pueden ser nulos.");

            if (string.IsNullOrWhiteSpace(dto.EmailOUserName))
                return Result.Fail("El email o nombre de usuario es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                return Result.Fail("La contraseña es obligatoria.");

            if (dto.Password.Length < 8)
                return Result.Fail("La contraseña debe tener al menos 8 caracteres.");

            return Result.Ok();
        }

        private Usuario? BuscarPorEmailOUserName(string emailOUserName)
        {
            Usuario? usuario = _usuarioRepository.GetByEmail(emailOUserName);

            if (usuario == null)
                usuario = _usuarioRepository.GetByUserName(emailOUserName);

            return usuario;
        }
    }
}
