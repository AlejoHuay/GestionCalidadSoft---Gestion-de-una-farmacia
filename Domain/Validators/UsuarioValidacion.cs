using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Application.Ports.Output;
using System.Text.RegularExpressions;

namespace ProyectoArqSoft.Domain.Validators
{
    public class UsuarioValidacionGeneral : UsuarioValidacionBase
    {
        private readonly IUsuarioRepository _repository;

        public UsuarioValidacionGeneral(IUsuarioRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Result ValidarRegistro(UsuarioRegistroDto dto)
        {
            if (dto == null)
                return Result.Fail("Los datos del usuario no pueden ser nulos.");

            string? nombres = LimpiarTexto(dto.Nombres);
            string? apellidoPaterno = LimpiarTexto(dto.ApellidoPaterno);
            string? apellidoMaterno = LimpiarTexto(dto.ApellidoMaterno);
            string? ci = LimpiarTexto(dto.Ci);
            string? telefono = LimpiarTexto(dto.Telefono);
            string? email = LimpiarTexto(dto.Email);
            string? userName = LimpiarTexto(dto.UserName);
            string? password = LimpiarTexto(dto.Password);

            Result? resultado = ValidarCamposObligatorios(nombres, apellidoPaterno, email)
                ?? ValidarCi(ci)
                ?? ValidarTelefono(telefono)
                ?? ValidarEmail(email)
                ?? ValidarPassword(password)
                ?? ValidarEmailDuplicado(email!)
                ?? ValidarUserNameDuplicado(userName!);

            return resultado ?? Result.Ok();
        }

        public Result ValidarActualizacion(UsuarioActualizarDto dto)
        {
            if (dto == null)
                return Result.Fail("Los datos del usuario no pueden ser nulos.");

            if (dto.IdUsuario <= 0)
                return Result.Fail("El identificador del usuario no es válido.");

            string? nombres = LimpiarTexto(dto.Nombres);
            string? apellidoPaterno = LimpiarTexto(dto.ApellidoPaterno);
            string? apellidoMaterno = LimpiarTexto(dto.ApellidoMaterno);
            string? ci = LimpiarTexto(dto.Ci);
            string? telefono = LimpiarTexto(dto.Telefono);
            string? email = LimpiarTexto(dto.Email);

            Result? resultado = ValidarCamposObligatorios(nombres, apellidoPaterno, email)
                ?? ValidarCi(ci)
                ?? ValidarTelefono(telefono)
                ?? ValidarEmail(email);

            return resultado ?? Result.Ok();
        }

        public Result ValidarEliminacion(int idUsuario)
        {
            if (idUsuario <= 0)
                return Result.Fail("El identificador del usuario no es válido.");

            var usuario = _repository.GetById(idUsuario);
            if (usuario == null)
                return Result.Fail("El usuario no existe.");

            return Result.Ok();
        }

        private Result? ValidarCamposObligatorios(string? nombres, string? apellidoPaterno, string? email)
        {
            if (string.IsNullOrWhiteSpace(nombres))
                return Result.Fail("El campo Nombres es obligatorio.");

            if (string.IsNullOrWhiteSpace(apellidoPaterno))
                return Result.Fail("El campo Apellido Paterno es obligatorio.");

            if (string.IsNullOrWhiteSpace(email))
                return Result.Fail("El campo Email es obligatorio.");

            if (nombres!.Length > 100)
                return Result.Fail("El campo Nombres no puede exceder 100 caracteres.");

            if (apellidoPaterno!.Length > 100)
                return Result.Fail("El campo Apellido Paterno no puede exceder 100 caracteres.");

            return null;
        }

        private Result? ValidarCi(string? ci)
        {
            if (string.IsNullOrWhiteSpace(ci))
                return Result.Fail("El número de carnet es obligatorio.");

            if (ci!.Contains(' '))
                return Result.Fail("El número de carnet no debe contener espacios.");

            if (!Regex.IsMatch(ci, @"^\d{8}(?:-[A-Za-z0-9]{1,2})?$", RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("El CI debe tener 8 dígitos y un complemento opcional de hasta dos caracteres (Ej. 10000000-1B).");

            return null;
        }

        private Result? ValidarTelefono(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return Result.Fail("El teléfono es obligatorio.");

            if (telefono!.Length != 8)
                return Result.Fail("El teléfono debe tener exactamente 8 dígitos.");

            if (!Regex.IsMatch(telefono, @"^\d{8}$", RegexOptions.None, TimeSpan.FromSeconds(1)))
                return Result.Fail("El teléfono debe contener solo dígitos numéricos.");

            return null;
        }

        private Result? ValidarEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result.Fail("El email es obligatorio.");

            if (!Regex.IsMatch(email!, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return Result.Fail("El formato del email no es válido.");

            if (email!.Length > 255)
                return Result.Fail("El email no puede exceder 255 caracteres.");

            return null;
        }

        private Result? ValidarPassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return Result.Fail("La contraseña es obligatoria.");

            if (password!.Length < 8)
                return Result.Fail("La contraseña debe tener al menos 8 caracteres.");

            if (password.Length > 128)
                return Result.Fail("La contraseña no puede exceder 128 caracteres.");

            return null;
        }

        private Result? ValidarEmailDuplicado(string email)
        {
            if (_repository.ExisteEmail(email))
                return Result.Fail("El email ya está registrado en el sistema.");

            return null;
        }

        private Result? ValidarUserNameDuplicado(string userName)
        {
            if (_repository.ExisteUserName(userName))
                return Result.Fail("El nombre de usuario ya está registrado en el sistema.");

            return null;
        }

        private string? LimpiarTexto(string? texto)
        {
            return texto?.Trim();
        }
    }
}