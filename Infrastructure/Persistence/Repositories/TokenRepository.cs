using Npgsql;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Infrastructure.Persistence.Connection;

namespace ProyectoArqSoft.Infrastructure.Persistence.Repositories
{
    public class UsuarioTokenRepository : IUsuarioTokenRepository
    {
        private readonly PostgresDatabase database;

        public UsuarioTokenRepository(PostgresDatabase database)
        {
            this.database = database;
        }

        public int Insert(UsuarioToken token)
        {
            string query = @"INSERT INTO usuario_token
                            (
                                usuario_idUsuario,
                                token_hash,
                                tipo_token,
                                fecha_creacion,
                                fecha_expiracion,
                                revocado,
                                usado,
                                fecha_uso,
                                fecha_revocacion
                            )
                            VALUES
                            (
                                @usuario_idUsuario,
                                @token_hash,
                                @tipo_token,
                                @fecha_creacion,
                                @fecha_expiracion,
                                @revocado,
                                @usado,
                                @fecha_uso,
                                @fecha_revocacion
                            )";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@usuario_idUsuario", token.UsuarioIdUsuario);
            command.Parameters.AddWithValue("@token_hash", token.TokenHash);
            command.Parameters.AddWithValue("@tipo_token", token.TipoToken);
            command.Parameters.AddWithValue("@fecha_creacion", token.FechaCreacion);
            command.Parameters.AddWithValue("@fecha_expiracion", token.FechaExpiracion);
            command.Parameters.AddWithValue("@revocado", token.Revocado);
            command.Parameters.AddWithValue("@usado", token.Usado);
            command.Parameters.AddWithValue("@fecha_uso", token.FechaUso.HasValue ? token.FechaUso.Value : DBNull.Value);
            command.Parameters.AddWithValue("@fecha_revocacion", token.FechaRevocacion.HasValue ? token.FechaRevocacion.Value : DBNull.Value);

            return RepositoryDbHelper.ExecuteNonQuery(database, command);
        }

        public UsuarioToken? GetByTokenHash(string tokenHash)
        {
            string query = @"SELECT *
                             FROM usuario_token
                             WHERE token_hash = @token_hash
                             LIMIT 1";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@token_hash", tokenHash);

            return RepositoryDbHelper.ExecuteReaderSingle(database, command, MapearUsuarioToken);
        }

        public UsuarioToken? GetTokenActivo(string tokenHash, string tipoToken)
        {
            string query = @"SELECT *
                             FROM usuario_token
                             WHERE token_hash = @token_hash
                               AND tipo_token = @tipo_token
                               AND revocado = 0
                               AND usado = 0
                             LIMIT 1";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@token_hash", tokenHash);
            command.Parameters.AddWithValue("@tipo_token", tipoToken);

            return RepositoryDbHelper.ExecuteReaderSingle(database, command, MapearUsuarioToken);
        }

        public int MarcarComoUsado(int idUsuarioToken)
        {
            string query = @"UPDATE usuario_token
                             SET usado = 1,
                                 fecha_uso = NOW()
                             WHERE id = @id";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@id", idUsuarioToken);

            return RepositoryDbHelper.ExecuteNonQuery(database, command);
        }

        public int RevocarTokensActivos(int idUsuario, string tipoToken)
        {
            string query = @"UPDATE usuario_token
                             SET revocado = 1,
                                 fecha_revocacion = NOW()
                             WHERE usuario_idUsuario = @usuario_idUsuario
                               AND tipo_token = @tipo_token
                               AND revocado = 0
                               AND usado = 0";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@usuario_idUsuario", idUsuario);
            command.Parameters.AddWithValue("@tipo_token", tipoToken);

            return RepositoryDbHelper.ExecuteNonQuery(database, command);
        }

        public int EliminarTokensObsoletos(int dias)
        {
            if (dias <= 0)
                return 0;

            string query = @"DELETE FROM usuario_token
                             WHERE fecha_expiracion < NOW() - (@dias * INTERVAL '1 day')
                                OR (usado = 1 AND fecha_uso IS NOT NULL AND fecha_uso < NOW() - (@dias * INTERVAL '1 day'))
                                OR (revocado = 1 AND fecha_revocacion IS NOT NULL AND fecha_revocacion < NOW() - (@dias * INTERVAL '1 day'))";

            NpgsqlCommand command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@dias", dias);

            return RepositoryDbHelper.ExecuteNonQuery(database, command);
        }

        private UsuarioToken MapearUsuarioToken(NpgsqlDataReader reader)
        {
            return new UsuarioToken
            {
                IdUsuarioToken = reader.GetInt32(reader.GetOrdinal("id")),
                UsuarioIdUsuario = reader.GetInt32(reader.GetOrdinal("usuario_idUsuario")),
                TokenHash = reader.GetString(reader.GetOrdinal("token_hash")),
                TipoToken = reader.GetString(reader.GetOrdinal("tipo_token")),
                FechaCreacion = DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("fecha_creacion")), DateTimeKind.Utc),
                FechaExpiracion = DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("fecha_expiracion")), DateTimeKind.Utc),
                Revocado = checked((sbyte)reader.GetInt16(reader.GetOrdinal("revocado"))),
                Usado = checked((sbyte)reader.GetInt16(reader.GetOrdinal("usado"))),
                FechaUso = reader.IsDBNull(reader.GetOrdinal("fecha_uso"))
                    ? (DateTime?)null
                    : DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("fecha_uso")), DateTimeKind.Utc),
                FechaRevocacion = reader.IsDBNull(reader.GetOrdinal("fecha_revocacion"))
                    ? (DateTime?)null
                    : DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("fecha_revocacion")), DateTimeKind.Utc)
            };
        }
    }
}

