using System.Data;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Domain.Validators;

namespace ProyectoArqSoft.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _repository;
        private readonly UsuarioValidacionGeneral _validacionGeneral;  // Usamos el nuevo validador general
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;

        public UsuarioService(
            IUsuarioRepository repository,
            UsuarioValidacionGeneral validacionGeneral,
            ITokenService tokenService,
            IEmailService emailService)
        {
            _repository = repository;
            _validacionGeneral = validacionGeneral;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        public Result CrearUsuario(UsuarioRegistroDto dto, string role, int? idUsuarioSesion)
        {
            Result validacion = _validacionGeneral.ValidarRegistro(dto);
            if (!validacion.IsSuccess)
                return validacion;

            string passwordTemporal = StringHelper.Limpiar(dto.Password);
            string passwordHash = PasswordHelper.Hash(passwordTemporal);

            Usuario usuario = ConstruirUsuarioNuevo(dto, role, passwordHash, idUsuarioSesion);

            int filasAfectadas = _repository.Insert(usuario);
            if (filasAfectadas <= 0)
                return Result.Fail("No se pudo registrar el usuario.");

            Usuario? usuarioRegistrado = _repository.GetByEmail(usuario.Email);
            if (usuarioRegistrado == null)
                return Result.Fail("El usuario fue registrado, pero no se pudo recuperar su información.");

            // Generar token de activación y guardarlo en la base de datos
            UsuarioTokenGeneracionDto tokenDto = new UsuarioTokenGeneracionDto
            {
                IdUsuario = usuarioRegistrado.IdUsuario,
                TipoToken = TipoTokenConstantes.ActivacionCuenta,
                MinutosExpiracion = 60
            };

            var (resultadoToken, tokenParaUrl) = _tokenService.GenerarToken(tokenDto, out string _);
            if (!resultadoToken.IsSuccess)
                return resultadoToken;

            string tokenSeguro = Uri.EscapeDataString(tokenParaUrl);
            string enlaceActivacion = $"http://localhost:5081/Auth/ActivarCuenta?token={tokenSeguro}";

            return _emailService.EnviarCorreoActivacionCuenta(
                usuarioRegistrado.Email,
                usuarioRegistrado.Nombres,
                usuarioRegistrado.UserName,
                passwordTemporal,
                enlaceActivacion
            );
        }

        public Result ActualizarUsuario(UsuarioActualizarDto dto, int? idUsuarioSesion)
        {
            // Usamos el validador general para validar la actualización
            Result validacion = UsuarioValidacionGeneral.ValidarActualizacion(dto);
            if (!validacion.IsSuccess)
                return validacion;

            Usuario? usuarioActual = _repository.GetById(dto.IdUsuario);
            if (usuarioActual == null)
                return Result.Fail("El usuario no existe.");

            AplicarActualizacion(usuarioActual, dto);

            int filasAfectadas = _repository.Update(usuarioActual, idUsuarioSesion);
            return filasAfectadas > 0
                ? Result.Ok()
                : Result.Fail("No se pudo actualizar el usuario.");
        }

        public Result ActualizarAccesoUsuario(UsuarioActualizarDto dto, int? idUsuarioSesion)
        {
            var actual = _repository.GetById(dto.IdUsuario);
            if (actual == null) return Result.Fail("El usuario no existe.");
            var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
            if (email.Length > 255 || !System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email)
                return Result.Fail("El correo electrónico no es válido.");
            if (dto.Role is not ("Admin" or "Bioquimico") || dto.Activo > 1)
                return Result.Fail("El rol o estado no es válido.");
            var otro = _repository.GetByEmail(email);
            if (otro != null && otro.IdUsuario != actual.IdUsuario)
                return Result.Fail("El correo ya está registrado.");
            actual.Email = email;
            actual.Role = dto.Role;
            actual.Activo = (sbyte)dto.Activo;
            return _repository.UpdateDatosEdicion(actual, idUsuarioSesion) > 0
                ? Result.Ok() : Result.Fail("No se pudo actualizar el usuario.");
        }

        public Result EliminarUsuario(int idUsuario, int? idUsuarioSesion)
        {
            Result validacion = _validacionGeneral.ValidarEliminacion(idUsuario);
            if (!validacion.IsSuccess)
                return validacion;

            Usuario? usuario = _repository.GetById(idUsuario);
            if (usuario == null)
                return Result.Fail("El usuario no existe.");

            int filasAfectadas = _repository.SoftDelete(usuario, idUsuarioSesion);
            return filasAfectadas > 0
                ? Result.Ok()
                : Result.Fail("No se pudo eliminar el usuario.");
        }

        public UsuarioDto? ObtenerUsuarioPorId(int idUsuario)
        {
            if (idUsuario <= 0)
                return null;

            return ObtenerYMapear(() => _repository.GetById(idUsuario));
        }

        public UsuarioDto? ObtenerUsuarioPorEmail(string email)
        {
            email = StringHelper.LimpiarTextoMinus(email);
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return ObtenerYMapear(() => _repository.GetByEmail(email));
        }

        public UsuarioDto? ObtenerUsuarioPorUserName(string userName)
        {
            userName = StringHelper.LimpiarTexto(userName);
            if (string.IsNullOrWhiteSpace(userName))
                return null;

            return ObtenerYMapear(() => _repository.GetByUserName(userName));
        }

        public DataTable ObtenerTodos()
        {
            return _repository.GetAll();
        }

        public DataTable ObtenerTodos(string filtro)
        {
            return _repository.GetAll(StringHelper.LimpiarTexto(filtro));
        }

        public Result ValidarActivacionCuenta(string token)
        {
            token = token?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(token))
                return Result.Fail("El token de activación es inválido.");

            UsuarioToken? usuarioToken = _tokenService.ValidarToken(token, TipoTokenConstantes.ActivacionCuenta);
            if (usuarioToken == null)
                return Result.Fail("El token ha expirado o es inválido.");

            return Result.Ok();
        }

        public Result ActivarCuenta(string token, string nuevaPassword)
        {
            token = token?.Trim() ?? string.Empty;
            nuevaPassword = nuevaPassword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(token))
                return Result.Fail("El token de activación es inválido.");

            if (string.IsNullOrWhiteSpace(nuevaPassword))
                return Result.Fail("La nueva contraseña es obligatoria.");

            if (nuevaPassword.Length < 8)
                return Result.Fail("La contraseña debe tener al menos 8 caracteres.");

            UsuarioToken? usuarioToken = _tokenService.ValidarToken(token, TipoTokenConstantes.ActivacionCuenta);
            if (usuarioToken == null)
                return Result.Fail("El token ha expirado o es inválido.");

            Usuario? usuario = _repository.GetById(usuarioToken.UsuarioIdUsuario);
            if (usuario == null)
                return Result.Fail("El usuario no existe.");

            string passwordHash = PasswordHelper.Hash(nuevaPassword);
            int filasAfectadas = _repository.ActivarCuentaConToken(usuario.IdUsuario, usuarioToken.IdUsuarioToken, passwordHash);
            if (filasAfectadas <= 0)
                return Result.Fail("No se pudo actualizar la contraseña.");

            return Result.Ok();
        }

        private Usuario ConstruirUsuarioNuevo(UsuarioRegistroDto dto, string role, string passwordHash, int? idUsuarioSesion)
        {
            return new Usuario
            {
                Nombres = dto.Nombres,
                ApellidoPaterno = dto.ApellidoPaterno,
                ApellidoMaterno = dto.ApellidoMaterno,
                Ci = dto.Ci,
                CiExtencion = dto.CiExtencion,
                Telefono = dto.Telefono,
                Email = dto.Email,
                UserName = dto.UserName,
                PasswordHash = passwordHash,
                Role = StringHelper.LimpiarTexto(role),
                Activo = 1,
                MustChangePassword = 1,
                IdUsuarioCreador = idUsuarioSesion
            };
        }

        private static void AplicarActualizacion(Usuario usuario, UsuarioActualizarDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Nombres))
                usuario.Nombres = dto.Nombres;
            usuario.ApellidoPaterno = dto.ApellidoPaterno;
            usuario.ApellidoMaterno = dto.ApellidoMaterno;
            usuario.Ci = dto.Ci;
            usuario.CiExtencion = dto.CiExtencion;
            usuario.Telefono = dto.Telefono;
            usuario.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.Role))
                usuario.Role = dto.Role;

        }

        private UsuarioDto? ObtenerYMapear(Func<Usuario?> obtenerUsuario)
        {
            Usuario? usuario = obtenerUsuario();
            return usuario == null ? null : MapearDto(usuario);
        }

        private UsuarioDto MapearDto(Usuario usuario)
        {
            return new UsuarioDto
            {
                IdUsuario = usuario.IdUsuario,
                Nombres = usuario.Nombres,
                ApellidoPaterno = usuario.ApellidoPaterno,
                ApellidoMaterno = usuario.ApellidoMaterno,
                Ci = usuario.Ci,
                CiExtencion = usuario.CiExtencion,
                Telefono = usuario.Telefono,
                Activo = usuario.Activo,
                Email = usuario.Email,
                UserName = usuario.UserName,
                Role = usuario.Role,
                MustChangePassword = usuario.MustChangePassword
            };
        }
    }
}
