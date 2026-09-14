using System.Data;
using System.Security.Claims;
using Npgsql;

namespace ProyectoArqSoft.Infrastructure.Persistence.Connection;

// Pool compartido; contexto de auditoría local a cada transacción.
public sealed class PostgresDatabase(NpgsqlDataSource dataSource, IHttpContextAccessor httpContextAccessor)
{
    public NpgsqlConnection CreateConnection() => dataSource.CreateConnection();

    public NpgsqlTransaction BeginTransaction(NpgsqlConnection connection)
    {
        var transaction = connection.BeginTransaction();
        try
        {
            var context = httpContextAccessor.HttpContext;
            var claim = context?.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            var actor = int.TryParse(claim, out var id) && id > 0 ? id.ToString() : "";
            using var command = new NpgsqlCommand("SELECT set_config('app.usuario_id', @actor, true), set_config('app.solicitud_id', @solicitud, true), set_config('app.motivo', @motivo, true)", connection, transaction);
            command.Parameters.AddWithValue("actor", actor);
            command.Parameters.AddWithValue("solicitud", context?.TraceIdentifier ?? Guid.NewGuid().ToString("N"));
            command.Parameters.AddWithValue("motivo", context?.Request.Path.Value ?? "Operacion de repositorio");
            command.ExecuteNonQuery();
            return transaction;
        }
        catch { transaction.Dispose(); throw; }
    }

    public int ExecuteNonQuery(NpgsqlCommand command)
    {
        using var ownedConnection = command.Connection == null ? CreateConnection() : null;
        command.Connection ??= ownedConnection;
        if (command.Connection!.State != ConnectionState.Open) command.Connection.Open();
        NormalizeParameters(command);
        using var transaction = BeginTransaction(command.Connection);
        command.Transaction = transaction;
        var affected = command.ExecuteNonQuery();
        transaction.Commit();
        return affected;
    }

    public static void NormalizeParameters(NpgsqlCommand command)
    {
        foreach (NpgsqlParameter parameter in command.Parameters)
        {
            if (parameter.Value == null) parameter.Value = DBNull.Value;
            if (parameter.Value is sbyte signed) parameter.Value = (short)signed;
            if (parameter.Value is byte unsigned) parameter.Value = (short)unsigned;
        }
    }
}
