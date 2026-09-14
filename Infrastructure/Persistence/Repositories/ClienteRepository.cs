using Npgsql;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Domain.Models;
using System.Data;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Persistence.Connection;

namespace ProyectoArqSoft.Infrastructure.Persistence.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly PostgresDatabase database;

        public ClienteRepository(PostgresDatabase database)
        {
            this.database = database;
        }

        public int Insert(Cliente t)
        {
            string query = @"INSERT INTO cliente
                            (nit, razon_social, correo_electronico, id_usuario, estado)
                            VALUES
                            (@nit, @razon_social, @correo_electronico, @id_usuario, @estado)";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);

                command.Parameters.AddWithValue("@nit", t.Nit);
                command.Parameters.AddWithValue("@razon_social", t.RazonSocial);
                command.Parameters.AddWithValue("@id_usuario", (object?)t.IdUsuario ?? DBNull.Value);
                command.Parameters.AddWithValue("@estado", t.Estado);
                command.Parameters.AddWithValue(
                    "@correo_electronico",
                    string.IsNullOrWhiteSpace(t.CorreoElectronico) ? DBNull.Value : t.CorreoElectronico);

                connection.Open();
                return database.ExecuteNonQuery(command);
            }
        }

        public int Update(Cliente t)
        {
            string query = @"UPDATE cliente
                             SET nit = @nit,
                                 razon_social = @razon_social,
                                 correo_electronico = @correo_electronico,
                                 id_usuario = @id_usuario,
                                 ultima_actualizacion = NOW()
                             WHERE id = @id";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);

                command.Parameters.AddWithValue("@id", t.IdCliente);
                command.Parameters.AddWithValue("@nit", t.Nit);
                command.Parameters.AddWithValue("@razon_social", t.RazonSocial);
                command.Parameters.AddWithValue("@id_usuario", (object?)t.IdUsuario ?? DBNull.Value);
                command.Parameters.AddWithValue(
                    "@correo_electronico",
                    string.IsNullOrWhiteSpace(t.CorreoElectronico) ? DBNull.Value : t.CorreoElectronico);

                connection.Open();
                return database.ExecuteNonQuery(command);
            }
        }

        public int Delete(Cliente t)
        {
            string query = @"UPDATE cliente
                             SET estado = 0,
                                 id_usuario = @id_usuario,
                                 ultima_actualizacion = NOW()
                             WHERE id = @id";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", t.IdCliente);
                command.Parameters.AddWithValue("@id_usuario", (object?)t.IdUsuario ?? DBNull.Value);

                connection.Open();
                return database.ExecuteNonQuery(command);
            }
        }

        public DataTable GetAll()
        {
            return GetAll(string.Empty);
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

        public Cliente? GetById(int id)
        {
            string query = @"SELECT id, fecha_registro, ultima_actualizacion, nit, razon_social, correo_electronico, id_usuario, estado
                             FROM cliente
                             WHERE id = @id
                               AND estado = 1";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                connection.Open();

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Cliente
                        {
                            IdCliente = Convert.ToInt32(reader["id"]),
                            Nit = StringHelper.LimpiarEspacios(reader["nit"].ToString()),
                            RazonSocial = StringHelper.LimpiarEspacios(reader["razon_social"].ToString()),
                            CorreoElectronico = StringHelper.LimpiarEspacios(reader["correo_electronico"].ToString()),
                            FechaRegistro = Convert.ToDateTime(reader["fecha_registro"]),
                            Estado = Convert.ToInt16(reader["estado"]),
                            IdUsuario = reader["id_usuario"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(reader["id_usuario"]),
                            UltimaActualizacion = reader["ultima_actualizacion"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["ultima_actualizacion"])
                        };
                    }
                }
            }

            return null;
        }

        private static string ConstruirQuery(string filtro)
        {
            string query = @"SELECT id,
                                    nit,
                                    razon_social,
                                    correo_electronico,
                                    fecha_registro,
                                    ultima_actualizacion
                             FROM cliente
                             WHERE estado = 1";

            query += FiltroSqlHelper.ConstruirCondicionLike(
                filtro,
                "nit",
                "razon_social",
                "correo_electronico"
            );

            query += " ORDER BY razon_social";

            return query;
        }

        public int Count()
        {
            string query = "SELECT COUNT(*) FROM cliente";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                connection.Open();

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
    }
}

