using ProyectoArqSoft.Infrastructure.Persistence.Connection;
using System;
using Npgsql;

namespace ProyectoArqSoft.Infrastructure.Helpers
{
    public static class RepositoryDbHelper
    {
        public static int ExecuteNonQuery(PostgresDatabase database, NpgsqlCommand command)
        {
            using var ownedCommand = command;
            using var connection = database.CreateConnection();
            command.Connection = connection;
            connection.Open();
            return database.ExecuteNonQuery(command);
        }

        public static object? ExecuteScalar(PostgresDatabase database, NpgsqlCommand command)
        {
            using var ownedCommand = command;
            using var connection = database.CreateConnection();
            command.Connection = connection;
            connection.Open();
            return command.ExecuteScalar();
        }

        public static T? ExecuteReaderSingle<T>(
            PostgresDatabase database,
            NpgsqlCommand command,
            Func<NpgsqlDataReader, T> mapper)
        {
            using var ownedCommand = command;
            using var connection = database.CreateConnection();
            command.Connection = connection;
            connection.Open();

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return mapper(reader);
            }

            return default;
        }
    }
}
