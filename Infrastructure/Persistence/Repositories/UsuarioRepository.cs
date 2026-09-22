using System.Data;
using Npgsql;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Persistence.Connection;

namespace ProyectoArqSoft.Infrastructure.Persistence.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly PostgresDatabase database;

        public UsuarioRepository(PostgresDatabase database)
        {
            this.database = database;
        }

        public int Insert(Usuario t)
        {
            string query = @"INSERT INTO usuario
                            (
                                nombres,
                                apellido_materno,
                                apellido_paterno,
                                ci,
                                telefono,
                                activo,
                                ci_extencion,
                                email,
                                user_name,
                                password_hash,
                                role,
                                must_change_password,
                                id_usuario
                            )
                            VALUES
                            (
                                @nombres,
                                @apellido_materno,
                                @apellido_paterno,
                                @ci,
                                @telefono,
                                @activo,
                                @ci_extencion,
                                @email,
                                @user_name,
                                @password_hash,
                                @role,
                                @must_change_password,
                                @id_usuario
                            )";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@nombres", t.Nombres);
            command.Parameters.AddWithValue("@apellido_materno", (object?)t.ApellidoMaterno ?? DBNull.Value);
            command.Parameters.AddWithValue("@apellido_paterno", t.ApellidoPaterno);
            command.Parameters.AddWithValue("@ci", t.Ci);
            command.Parameters.AddWithValue("@telefono", t.Telefono);
            command.Parameters.AddWithValue("@activo", t.Activo);
            command.Parameters.AddWithValue("@ci_extencion", t.CiExtencion);
            command.Parameters.AddWithValue("@email", t.Email);
            command.Parameters.AddWithValue("@user_name", t.UserName);
            command.Parameters.AddWithValue("@password_hash", t.PasswordHash);
            command.Parameters.AddWithValue("@role", t.Role);
            command.Parameters.AddWithValue("@must_change_password", t.MustChangePassword);
            command.Parameters.AddWithValue("@id_usuario", (object?)t.IdUsuarioCreador ?? DBNull.Value);

            return RepositoryDbHelper.ExecuteNonQuery(database, command);
        }

        public int Update(Usuario t)
        {
            return Update(t, null);
        } 

        public int Delete(Usuario usuario)
        {
            return SoftDelete(usuario, null);
        }

        public Usuario? GetById(int id)
        {
            string query = @"SELECT *
                             FROM usuario
                             WHERE id = @id";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@id", id);

            return RepositoryDbHelper.ExecuteReaderSingle(database, command, MapearUsuario);
        }

        public Usuario? GetByEmail(string email)
        {
            string query = @"SELECT *
                             FROM usuario
                             WHERE farmacia.normalizar_texto(email) = farmacia.normalizar_texto(@email)
                             LIMIT 1";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@email", email);

            return RepositoryDbHelper.ExecuteReaderSingle(database, command, MapearUsuario);
        }

        public Usuario? GetByUserName(string userName)
        {
            string query = @"SELECT *
                             FROM usuario
                             WHERE farmacia.normalizar_texto(user_name) = farmacia.normalizar_texto(@user_name)
                             LIMIT 1";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@user_name", userName);

            return RepositoryDbHelper.ExecuteReaderSingle(database, command, MapearUsuario);
        }

        public bool ExisteEmail(string email)
        {
            string query = @"SELECT COUNT(*)
                             FROM usuario
                             WHERE farmacia.normalizar_texto(email) = farmacia.normalizar_texto(@email)";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@email", email);

            var result = RepositoryDbHelper.ExecuteScalar(database, command);
            return Convert.ToInt32(result) > 0;
        }

        public bool ExisteUserName(string userName)
        {
            string query = @"SELECT COUNT(*)
                             FROM usuario
                             WHERE farmacia.normalizar_texto(user_name) = farmacia.normalizar_texto(@user_name)";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@user_name", userName);

            var result = RepositoryDbHelper.ExecuteScalar(database, command);
            return Convert.ToInt32(result) > 0;
        }

        public int CambiarPassword(int idUsuario, string nuevoPasswordHash, bool mustChangePassword)
        {
            string query = @"UPDATE usuario
                             SET password_hash = @password_hash,
                                 must_change_password = @must_change_password,
                                 ultima_actualizacion = NOW()
                             WHERE id = @id";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@id", idUsuario);
            command.Parameters.AddWithValue("@password_hash", nuevoPasswordHash);
            command.Parameters.AddWithValue("@must_change_password", (short)(mustChangePassword ? 1 : 0));

            return RepositoryDbHelper.ExecuteNonQuery(database, command);
        }

        public DataTable GetAll()
        {
            return GetAll(string.Empty);
        }

        public int ActivarCuentaConToken(int idUsuario, int idToken, string passwordHash)
        {
            using var command = new NpgsqlCommand(@"WITH consumido AS (
                UPDATE usuario_token SET usado=1, fecha_uso=NOW()
                WHERE id=@token AND usuario_idusuario=@usuario AND tipo_token='ACTIVATION_CUENTA'
                  AND usado=0 AND revocado=0 AND fecha_expiracion>NOW()
                  AND EXISTS(SELECT 1 FROM usuario WHERE id=@usuario AND activo=1)
                RETURNING usuario_idusuario)
                UPDATE usuario SET password_hash=@hash,must_change_password=0,ultima_actualizacion=NOW()
                WHERE id IN (SELECT usuario_idusuario FROM consumido)");
            command.Parameters.AddWithValue("token", idToken);
            command.Parameters.AddWithValue("usuario", idUsuario);
            command.Parameters.AddWithValue("hash", passwordHash);
            return database.ExecuteNonQuery(command);
        }

        public DataTable GetAll(string filtro)
        {
            DataTable tabla = new DataTable();

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                connection.Open();

                string query = ConstruirQuery(filtro);
                NpgsqlCommand command = new NpgsqlCommand(query, connection);

                FiltroSqlHelper.AgregarParametrosLike(command, filtro);

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command);
                adapter.Fill(tabla);
            }

            return tabla;
        }

        private static string ConstruirQuery(string filtro)
        {
            string query = @"SELECT id,
                                    nombres,
                                    apellido_paterno,
                                    apellido_materno,
                                    ci,
                                    telefono,
                                    ci_extencion,
                                    email,
                                    user_name,
                                    role,
                                    activo
                             FROM usuario
                             WHERE activo = 1";

            query += FiltroSqlHelper.ConstruirCondicionLike(
                filtro,
                "nombres",
                "apellido_paterno",
                "apellido_materno",
                "ci",
                "telefono",
                "ci_extencion",
                "email",
                "user_name",
                "role"
            );

            query += " ORDER BY nombres, apellido_paterno, apellido_materno";

            return query;
        }

        private Usuario MapearUsuario(NpgsqlDataReader reader)
        {
            return new Usuario
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("id")),
                Nombres = reader.GetString(reader.GetOrdinal("nombres")),
                ApellidoMaterno = reader.IsDBNull(reader.GetOrdinal("apellido_materno"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("apellido_materno")),
                ApellidoPaterno = reader.GetString(reader.GetOrdinal("apellido_paterno")),
                Ci = reader.GetString(reader.GetOrdinal("ci")),
                Telefono = reader.GetString(reader.GetOrdinal("telefono")),
                Activo = checked((sbyte)reader.GetInt16(reader.GetOrdinal("activo"))),
                FechaRegistro = reader.GetDateTime(reader.GetOrdinal("fecha_registro")),
                UltimaActualizacion = reader.IsDBNull(reader.GetOrdinal("ultima_actualizacion"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("ultima_actualizacion")),
                IdUsuarioCreador = reader.IsDBNull(reader.GetOrdinal("id_usuario"))
                    ? (int?)null
                    : reader.GetInt32(reader.GetOrdinal("id_usuario")),
                CiExtencion = reader.GetString(reader.GetOrdinal("ci_extencion")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                UserName = reader.GetString(reader.GetOrdinal("user_name")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                Role = reader.GetString(reader.GetOrdinal("role")),
                MustChangePassword = checked((sbyte)reader.GetInt16(reader.GetOrdinal("must_change_password")))
            };
        }

        public int UpdateDatosEdicion(Usuario usuario, int? idUsuarioSesion)
        {
            string query = @"UPDATE usuario
                            SET
                                email = @email,
                                user_name = @user_name,
                                role = @role,
                                activo = @activo,
                                Id_usuario = @idUsuarioSesion,
                                ultima_actualizacion = NOW()
                             WHERE id = @id";
                            

            using NpgsqlConnection connection = database.CreateConnection();
            connection.Open();

            using NpgsqlCommand command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@idUsuarioSesion", (object?)idUsuarioSesion ?? DBNull.Value);
            command.Parameters.AddWithValue("@email", usuario.Email);
            command.Parameters.AddWithValue("@user_name", usuario.UserName);
            command.Parameters.AddWithValue("@role", usuario.Role);
            command.Parameters.AddWithValue("@activo", usuario.Activo);
            command.Parameters.AddWithValue("@id", usuario.IdUsuario);

            return database.ExecuteNonQuery(command);
        }
        public int SoftDelete(Usuario usuario, int? idUsuarioSesion)
        {
            string query = @"UPDATE usuario
                            SET activo = 0,
                                ultima_actualizacion = NOW(),
                                id_usuario = @idUsuarioSesion
                            WHERE id = @id";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@idUsuarioSesion", (object?)idUsuarioSesion ?? DBNull.Value);
            command.Parameters.AddWithValue("@id", usuario.IdUsuario);

            return RepositoryDbHelper.ExecuteNonQuery(database, command);
        }

        public int Update(Usuario usuario, int? idUsuarioSesion)
        {
            string query = @"UPDATE usuario
                            SET nombres = @nombres,
                                apellido_materno = @apellido_materno,
                                apellido_paterno = @apellido_paterno,
                                ci = @ci,
                                telefono = @telefono,
                                ci_extencion = @ci_extencion,
                                email = @email,
                                user_name = @user_name,
                                role = @role,
                                activo = @activo,
                                id_usuario = @idUsuarioSesion,
                                ultima_actualizacion = NOW()
                            WHERE id = @id";

            using NpgsqlConnection connection = database.CreateConnection();
            connection.Open();

            using NpgsqlCommand command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@nombres", usuario.Nombres);
            command.Parameters.AddWithValue("@apellido_materno", (object?)usuario.ApellidoMaterno ?? DBNull.Value);
            command.Parameters.AddWithValue("@apellido_paterno", usuario.ApellidoPaterno);
            command.Parameters.AddWithValue("@ci", usuario.Ci);
            command.Parameters.AddWithValue("@telefono", usuario.Telefono);
            command.Parameters.AddWithValue("@ci_extencion", usuario.CiExtencion);
            command.Parameters.AddWithValue("@email", usuario.Email);
            command.Parameters.AddWithValue("@user_name", usuario.UserName);
            command.Parameters.AddWithValue("@role", usuario.Role);
            command.Parameters.AddWithValue("@activo", usuario.Activo);
            command.Parameters.AddWithValue("@idUsuarioSesion", (object?)idUsuarioSesion ?? DBNull.Value);
            command.Parameters.AddWithValue("@id", usuario.IdUsuario);

            return database.ExecuteNonQuery(command);
        }
    
        public int Count()
        {
            string query = "SELECT COUNT(*) FROM usuario";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                connection.Open();

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
    }
}

