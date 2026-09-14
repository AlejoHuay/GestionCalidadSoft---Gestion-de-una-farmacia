using Npgsql;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Infrastructure.Helpers;
using System.Data;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Persistence.Connection;

namespace ProyectoArqSoft.Infrastructure.Persistence.Repositories
{
    public class ClasificacionRepository : IClasificacionRepository
    {
        private readonly PostgresDatabase database;

        public ClasificacionRepository(PostgresDatabase database)
        {
            this.database = database;
        }

        public int Insert(Clasificacion t)
        {
            string query = @"INSERT INTO clasificacion
                             (nombre, origen, descripcion, id_usuario)
                             VALUES
                             (@nombre, @origen, @descripcion, @id_usuario)";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@nombre", t.Nombre);
                command.Parameters.AddWithValue("@origen", t.Origen);
                command.Parameters.AddWithValue("@descripcion", t.Descripcion);
                command.Parameters.AddWithValue("@id_usuario", (object?)t.IdUsuario ?? DBNull.Value);

                connection.Open();
                return database.ExecuteNonQuery(command);
            }
        }

        public int Update(Clasificacion t)
        {
            string query = @"UPDATE clasificacion
                             SET nombre = @nombre,
                                 origen = @origen,
                                 descripcion = @descripcion,
                                 id_usuario = @id_usuario,
                                 ultima_actualizacion = NOW()
                             WHERE id = @id";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", t.Id);
                command.Parameters.AddWithValue("@nombre", t.Nombre);
                command.Parameters.AddWithValue("@origen", t.Origen);
                command.Parameters.AddWithValue("@id_usuario", (object?)t.IdUsuario ?? DBNull.Value);
                command.Parameters.AddWithValue("@descripcion", t.Descripcion);

                connection.Open();
                return database.ExecuteNonQuery(command);
            }
        }

        public int Delete(Clasificacion t)
        {
            string query = @"UPDATE clasificacion
                             SET estado = 0,
                                 id_usuario = @id_usuario,
                                 ultima_actualizacion = NOW()
                             WHERE id = @id";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", t.Id);
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

        public Clasificacion? GetById(int id)
        {
            string query = @"SELECT id,
                                    nombre,
                                    origen,
                                    descripcion,
                                    estado,
                                    fecha_registro,
                                    ultima_actualizacion
                             FROM clasificacion
                             WHERE id = @id";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                connection.Open();

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Clasificacion
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Nombre = StringHelper.LimpiarEspacios(reader["nombre"].ToString()),
                            Origen = StringHelper.LimpiarEspacios(reader["origen"].ToString()),
                            Descripcion = StringHelper.LimpiarEspacios(reader["descripcion"].ToString()),
                            Estado = Convert.ToInt16(reader["estado"]),
                            FechaRegistro = Convert.ToDateTime(reader["fecha_registro"]),
                            UltimaActualizacion = reader["ultima_actualizacion"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["ultima_actualizacion"])
                        };
                    }
                }
            }

            return null;
        }

        public bool TieneMedicamentosActivosAsociados(int idClasificacion)
        {
            string query = @"SELECT COUNT(*)
                             FROM medicamento
                             WHERE id_clasificacion = @id_clasificacion
                               AND estado = 1";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_clasificacion", idClasificacion);

                connection.Open();
                int cantidad = Convert.ToInt32(command.ExecuteScalar());

                return cantidad > 0;
            }
        }

        private string ConstruirQuery(string filtro)
        {
            string query = @"SELECT id,
                                    nombre,
                                    origen,
                                    descripcion
                             FROM clasificacion
                             WHERE estado = 1";

            query += FiltroSqlHelper.ConstruirCondicionLike(
                filtro,
                "nombre",
                "origen",
                "descripcion"
            );

            query += " ORDER BY nombre";

            return query;
        }

        public int Count()
        {
            string query = "SELECT COUNT(*) FROM clasificacion WHERE estado = 1";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                connection.Open();

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public bool ExisteNombreActivo(string nombre)
        {
            string query = @"SELECT COUNT(*)
                            FROM clasificacion
                            WHERE farmacia.normalizar_texto(nombre) = farmacia.normalizar_texto(@nombre)
                            AND estado = 1";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@nombre", nombre);

                connection.Open();
                int cantidad = Convert.ToInt32(command.ExecuteScalar());

                return cantidad > 0;
            }
        }

        public bool ExisteNombreActivoExcluyendoId(int idClasificacion, string nombre)
        {
            string query = @"SELECT COUNT(*)
                            FROM clasificacion
                            WHERE farmacia.normalizar_texto(nombre) = farmacia.normalizar_texto(@nombre)
                            AND estado = 1
                            AND id <> @id";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@nombre", nombre);
                command.Parameters.AddWithValue("@id", idClasificacion);

                connection.Open();
                int cantidad = Convert.ToInt32(command.ExecuteScalar());

                return cantidad > 0;
            }
        }
    }
}

