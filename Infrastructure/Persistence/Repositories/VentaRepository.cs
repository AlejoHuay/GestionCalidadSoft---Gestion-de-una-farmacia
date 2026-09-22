using Npgsql;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Domain.Validators;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Infrastructure.Helpers;
using ProyectoArqSoft.Infrastructure.Persistence.Connection;
using System.Data;

namespace ProyectoArqSoft.Infrastructure.Persistence.Repositories
{
    public class VentaRepository : IVentaRepository
    {
        private readonly PostgresDatabase database;

        public VentaRepository(PostgresDatabase database)
        {
            this.database = database;
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

                string query = @"SELECT v.id,
                                        v.fecha_hora,
                                        v.total,
                                        v.metodo_pago,
                                        CONCAT(v.nit, ' - ', v.razon_social) AS cliente,
                                        v.razon_social,
                                        v.nit,
                                        u.user_name AS usuario
                                 FROM venta v
                                 INNER JOIN usuario u ON v.usuario_idUsuario = u.id
                                 WHERE v.estado = 1";

                query += FiltroSqlHelper.ConstruirCondicionLike(
                    filtro,
                    "v.metodo_pago",
                    "v.razon_social",
                    "v.nit",
                    "u.user_name"
                );

                query += " ORDER BY v.fecha_hora DESC";

                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                FiltroSqlHelper.AgregarParametrosLike(command, filtro);

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command);
                adapter.Fill(tabla);
            }

            return tabla;
        }

        public Venta? GetById(int id)
        {
            string query = @"SELECT id,
                                    fecha_hora,
                                    total,
                                    metodo_pago,
                                    Cliente_idCliente,
                                    usuario_idUsuario,
                                    estado,
                                    fecha_registro,
                                    ultima_actualizacion,
                                    Id_usuario_editor,
                                    nit,
                                    razon_social
                             FROM venta
                             WHERE id = @id";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                connection.Open();

                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new Venta
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        FechaHora = Convert.ToDateTime(reader["fecha_hora"]),
                        Total = Convert.ToDecimal(reader["total"]),
                        MetodoPago = StringHelper.LimpiarEspacios(reader["metodo_pago"]?.ToString()),
                        IdCliente = Convert.ToInt32(reader["Cliente_idCliente"]),
                        IdUsuario = Convert.ToInt32(reader["usuario_idUsuario"]),
                        Estado = Convert.ToInt16(reader["estado"]),
                        FechaRegistro = Convert.ToDateTime(reader["fecha_registro"]),
                        UltimaActualizacion = reader["ultima_actualizacion"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["ultima_actualizacion"]),
                        IdUsuarioEditor = reader["Id_usuario_editor"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(reader["Id_usuario_editor"]),
                        Nit = StringHelper.LimpiarEspacios(reader["nit"]?.ToString()),
                        RazonSocial = StringHelper.LimpiarEspacios(reader["razon_social"]?.ToString())
                    };
                }
            }
        }

        public List<DetalleVenta> GetDetallesByVentaId(int idVenta)
        {
            List<DetalleVenta> detalles = new();

            string query = @"SELECT cantidad,
                                    precio_unitario,
                                    id_venta,
                                    id_medicamento
                             FROM detalle_venta
                             WHERE id_venta = @id_venta
                             ORDER BY id_medicamento";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                connection.Open();

                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_venta", idVenta);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new DetalleVenta
                        {
                            Cantidad = Convert.ToInt32(reader["cantidad"]),
                            PrecioUnitario = Convert.ToDecimal(reader["precio_unitario"]),
                            IdVenta = Convert.ToInt32(reader["id_venta"]),
                            IdMedicamento = Convert.ToInt32(reader["id_medicamento"])
                        });
                    }
                }
            }

            return detalles;
        }

        public Result RegistrarVenta(Venta venta)
        {
            using (NpgsqlConnection connection = database.CreateConnection())
            {
                connection.Open();
                using NpgsqlTransaction transaction = database.BeginTransaction(connection);

                try
                {
                    Result validacionDb = ValidarVentaParaRegistro(connection, transaction, venta);
                    if (!validacionDb.IsSuccess)
                    {
                        transaction.Rollback();
                        return validacionDb;
                    }

                    string queryCliente = @"SELECT nit, razon_social
                                            FROM cliente
                                            WHERE id = @idCliente AND estado = 1";

                    NpgsqlCommand commandCliente = new NpgsqlCommand(queryCliente, connection, transaction);
                    commandCliente.Parameters.AddWithValue("@idCliente", venta.IdCliente);

                    using (NpgsqlDataReader reader = commandCliente.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            transaction.Rollback();
                            return Result.Fail("No se pudo obtener los datos del cliente.");
                        }

                        venta.Nit = reader["nit"].ToString()!;
                        venta.RazonSocial = reader["razon_social"].ToString()!;
                    }

                    string queryVenta = @"INSERT INTO venta
                                        (total, metodo_pago, Cliente_idCliente, usuario_idUsuario, nit, razon_social)
                                        VALUES
                                        (@total, @metodo_pago, @idCliente, @idUsuario, @nit, @razon_social) RETURNING id";

                    NpgsqlCommand commandVenta = new NpgsqlCommand(queryVenta, connection, transaction);
                    commandVenta.Parameters.AddWithValue("@total", venta.Total);
                    commandVenta.Parameters.AddWithValue("@metodo_pago", venta.MetodoPago);
                    commandVenta.Parameters.AddWithValue("@idCliente", venta.IdCliente);
                    commandVenta.Parameters.AddWithValue("@idUsuario", venta.IdUsuario);
                    commandVenta.Parameters.AddWithValue("@nit", venta.Nit);
                    commandVenta.Parameters.AddWithValue("@razon_social", venta.RazonSocial);

                    int idVenta = Convert.ToInt32(commandVenta.ExecuteScalar());

                    foreach (DetalleVenta detalle in venta.Detalles)
                    {
                        Result resultadoStock = DescontarStock(connection, transaction, detalle.IdMedicamento, detalle.Cantidad);
                        if (!resultadoStock.IsSuccess)
                        {
                            transaction.Rollback();
                            return resultadoStock;
                        }

                        string queryDetalle = @"INSERT INTO detalle_venta
                                                (cantidad, precio_unitario, id_venta, id_medicamento)
                                                VALUES
                                                (@cantidad, @precio_unitario, @id_venta, @id_medicamento)";

                        NpgsqlCommand commandDetalle = new NpgsqlCommand(queryDetalle, connection, transaction);
                        commandDetalle.Parameters.AddWithValue("@cantidad", detalle.Cantidad);
                        commandDetalle.Parameters.AddWithValue("@precio_unitario", detalle.PrecioUnitario);
                        commandDetalle.Parameters.AddWithValue("@id_venta", idVenta);
                        commandDetalle.Parameters.AddWithValue("@id_medicamento", detalle.IdMedicamento);

                        commandDetalle.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Result.Ok();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Result.Fail($"No se pudo registrar la venta. {ex.Message}");
                }
            }
        }

        public Result ActualizarVenta(Venta venta)
        {
            using (NpgsqlConnection connection = database.CreateConnection())
            {
                connection.Open();
                using NpgsqlTransaction transaction = database.BeginTransaction(connection);

                try
                {
                    Result estadoVenta = ValidarVentaEditable(connection, transaction, venta.Id);
                    if (!estadoVenta.IsSuccess)
                    {
                        transaction.Rollback();
                        return estadoVenta;
                    }

                    Result validacionBase = ValidarVentaParaActualizacion(connection, transaction, venta);
                    if (!validacionBase.IsSuccess)
                    {
                        transaction.Rollback();
                        return validacionBase;
                    }   

                    string queryCliente = @"SELECT nit, razon_social
                                    FROM cliente
                                    WHERE id = @idCliente AND estado = 1";

                    NpgsqlCommand commandCliente = new NpgsqlCommand(queryCliente, connection, transaction);
                    commandCliente.Parameters.AddWithValue("@idCliente", venta.IdCliente);

                    using (NpgsqlDataReader reader = commandCliente.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            transaction.Rollback();
                            return Result.Fail("No se pudo obtener los datos del cliente.");
                        }

                        venta.Nit = reader["nit"].ToString()!;
                        venta.RazonSocial = reader["razon_social"].ToString()!;
                    }

                    List<DetalleVenta> detallesActuales = GetDetallesByVentaIdTransaccional(connection, transaction, venta.Id);
                    BloquearMedicamentos(connection, transaction, detallesActuales.Concat(venta.Detalles));
                    foreach (var detalle in venta.Detalles)
                    {
                        var anterior = detallesActuales.Find(d => d.IdMedicamento == detalle.IdMedicamento);
                        if (anterior != null) detalle.PrecioUnitario = anterior.PrecioUnitario;
                    }
                    venta.Total = venta.Detalles.Sum(d => d.Subtotal);

                    foreach (DetalleVenta detalleActual in detallesActuales)
                    {
                        string queryRestore = @"UPDATE medicamento
                                                SET stock = stock + @cantidad
                                                WHERE id = @id_medicamento";

                        NpgsqlCommand commandRestore = new NpgsqlCommand(queryRestore, connection, transaction);
                        commandRestore.Parameters.AddWithValue("@cantidad", detalleActual.Cantidad);
                        commandRestore.Parameters.AddWithValue("@id_medicamento", detalleActual.IdMedicamento);
                        commandRestore.ExecuteNonQuery();
                    }

                    Result validacionStock = ValidarMedicamentosYStock(connection, transaction, venta.Detalles);
                    if (!validacionStock.IsSuccess)
                    {
                        transaction.Rollback();
                        return validacionStock;
                    }

                    string queryDeleteDetalles = @"DELETE FROM detalle_venta
                                                  WHERE id_venta = @id_venta";

                    NpgsqlCommand commandDeleteDetalles = new NpgsqlCommand(queryDeleteDetalles, connection, transaction);
                    commandDeleteDetalles.Parameters.AddWithValue("@id_venta", venta.Id);
                    commandDeleteDetalles.ExecuteNonQuery();

                    string queryUpdateVenta = @"UPDATE venta
                                                SET total = @total,
                                                    metodo_pago = @metodo_pago,
                                                    Cliente_idCliente = @idCliente,
                                                    nit = @nit,
                                                    razon_social = @razon_social,
                                                    ultima_actualizacion = NOW(),
                                                    Id_usuario_editor = @id_usuario_editor
                                                WHERE id = @id
                                                  AND estado = 1";

                    NpgsqlCommand commandUpdateVenta = new NpgsqlCommand(queryUpdateVenta, connection, transaction);
                    commandUpdateVenta.Parameters.AddWithValue("@total", venta.Total);
                    commandUpdateVenta.Parameters.AddWithValue("@metodo_pago", venta.MetodoPago);
                    commandUpdateVenta.Parameters.AddWithValue("@idCliente", venta.IdCliente);
                    commandUpdateVenta.Parameters.AddWithValue("@id_usuario_editor", (object?)venta.IdUsuarioEditor ?? DBNull.Value);
                    commandUpdateVenta.Parameters.AddWithValue("@id", venta.Id);
                    commandUpdateVenta.Parameters.AddWithValue("@nit", venta.Nit);
                    commandUpdateVenta.Parameters.AddWithValue("@razon_social", venta.RazonSocial);

                    int filasVenta = commandUpdateVenta.ExecuteNonQuery();
                    if (filasVenta <= 0)
                    {
                        transaction.Rollback();
                        return Result.Fail("No se pudo actualizar la venta.");
                    }

                    foreach (DetalleVenta detalle in venta.Detalles)
                    {
                        Result resultadoStock = DescontarStock(connection, transaction, detalle.IdMedicamento, detalle.Cantidad);
                        if (!resultadoStock.IsSuccess)
                        {
                            transaction.Rollback();
                            return resultadoStock;
                        }

                        string queryDetalle = @"INSERT INTO detalle_venta
                                                (cantidad, precio_unitario, id_venta, id_medicamento)
                                                VALUES
                                                (@cantidad, @precio_unitario, @id_venta, @id_medicamento)";

                        NpgsqlCommand commandDetalle = new NpgsqlCommand(queryDetalle, connection, transaction);
                        commandDetalle.Parameters.AddWithValue("@cantidad", detalle.Cantidad);
                        commandDetalle.Parameters.AddWithValue("@precio_unitario", detalle.PrecioUnitario);
                        commandDetalle.Parameters.AddWithValue("@id_venta", venta.Id);
                        commandDetalle.Parameters.AddWithValue("@id_medicamento", detalle.IdMedicamento);
                        commandDetalle.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Result.Ok();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Result.Fail($"No se pudo actualizar la venta. {ex.Message}");
                }
            }
        }

        public Result AnularVentaLogicamente(int idVenta, int idUsuarioEditor)
        {
            using (NpgsqlConnection connection = database.CreateConnection())
            {
                connection.Open();
                using NpgsqlTransaction transaction = database.BeginTransaction(connection);

                try
                {
                    string queryEstado = @"SELECT estado
                                           FROM venta
                                           WHERE id = @id FOR UPDATE";

                    NpgsqlCommand commandEstado = new NpgsqlCommand(queryEstado, connection, transaction);
                    commandEstado.Parameters.AddWithValue("@id", idVenta);

                    object? estadoObj = commandEstado.ExecuteScalar();

                    if (estadoObj == null)
                    {
                        transaction.Rollback();
                        return Result.Fail("La venta no existe.");
                    }

                    int estado = Convert.ToInt32(estadoObj);
                    if (estado == 0)
                    {
                        transaction.Rollback();
                        return Result.Fail("La venta ya está anulada lógicamente.");
                    }

                    if (!UsuarioActivo(connection, transaction, idUsuarioEditor))
                    {
                        transaction.Rollback();
                        return Result.Fail("El usuario editor no existe o está inactivo.");
                    }

                    List<DetalleVenta> detalles = GetDetallesByVentaIdTransaccional(connection, transaction, idVenta);

                    foreach (DetalleVenta detalle in detalles)
                    {
                        string queryRestore = @"UPDATE medicamento
                                                SET stock = stock + @cantidad
                                                WHERE id = @id_medicamento";

                        NpgsqlCommand commandRestore = new NpgsqlCommand(queryRestore, connection, transaction);
                        commandRestore.Parameters.AddWithValue("@cantidad", detalle.Cantidad);
                        commandRestore.Parameters.AddWithValue("@id_medicamento", detalle.IdMedicamento);
                        commandRestore.ExecuteNonQuery();
                    }

                    string queryAnular = @"UPDATE venta
                                           SET estado = 0,
                                               ultima_actualizacion = NOW(),
                                               Id_usuario_editor = @id_usuario_editor
                                           WHERE id = @id
                                             AND estado = 1";

                    NpgsqlCommand commandAnular = new NpgsqlCommand(queryAnular, connection, transaction);
                    commandAnular.Parameters.AddWithValue("@id_usuario_editor", idUsuarioEditor);
                    commandAnular.Parameters.AddWithValue("@id", idVenta);

                    int filas = commandAnular.ExecuteNonQuery();
                    if (filas <= 0)
                    {
                        transaction.Rollback();
                        return Result.Fail("No se pudo anular la venta.");
                    }

                    transaction.Commit();
                    return Result.Ok();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Result.Fail($"No se pudo anular la venta. {ex.Message}");
                }
            }
        }

        private Result ValidarVentaParaRegistro(NpgsqlConnection connection, NpgsqlTransaction transaction, Venta venta)
        {
            if (!ClienteActivo(connection, transaction, venta.IdCliente))
                return Result.Fail("El cliente no existe o está inactivo.");

            if (!UsuarioActivo(connection, transaction, venta.IdUsuario))
                return Result.Fail("El usuario que registra la venta no existe o está inactivo.");

            return ValidarMedicamentosYStock(connection, transaction, venta.Detalles);
        }

        private Result ValidarVentaParaActualizacion(NpgsqlConnection connection, NpgsqlTransaction transaction, Venta venta)
        {
            if (!ClienteActivo(connection, transaction, venta.IdCliente))
                return Result.Fail("El cliente no existe o está inactivo.");

            if (venta.IdUsuarioEditor == null || !UsuarioActivo(connection, transaction, venta.IdUsuarioEditor.Value))
                return Result.Fail("El usuario editor no existe o está inactivo.");

            return Result.Ok();
        }

        private Result ValidarVentaEditable(NpgsqlConnection connection, NpgsqlTransaction transaction, int idVenta)
        {
            string query = @"SELECT estado
                             FROM venta
                             WHERE id = @id FOR UPDATE";

            NpgsqlCommand command = new NpgsqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id", idVenta);

            object? estadoObj = command.ExecuteScalar();

            if (estadoObj == null)
                return Result.Fail("La venta no existe.");

            int estado = Convert.ToInt32(estadoObj);
            if (estado == 0)
                return Result.Fail("No se puede modificar una venta anulada.");

            return Result.Ok();
        }

        private static void BloquearMedicamentos(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<DetalleVenta> detalles)
        {
            using var command = new NpgsqlCommand("SELECT id FROM medicamento WHERE id = ANY(@ids) ORDER BY id FOR UPDATE", connection, transaction);
            command.Parameters.AddWithValue("ids", detalles.Select(d => d.IdMedicamento).Distinct().Order().ToArray());
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                // Consume todas las filas del SELECT FOR UPDATE antes de validar o modificar el stock.
                // Los bloqueos se mantienen hasta el commit o rollback de la transacción actual.
            }
        }

        private Result ValidarMedicamentosYStock(NpgsqlConnection connection, NpgsqlTransaction transaction, List<DetalleVenta> detalles)
        {
            BloquearMedicamentos(connection, transaction, detalles);
            foreach (DetalleVenta detalle in detalles)
            {
                string query = @"SELECT nombre, stock, estado
                                 FROM medicamento
                                 WHERE id = @id_medicamento";

                NpgsqlCommand command = new NpgsqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@id_medicamento", detalle.IdMedicamento);

                using NpgsqlDataReader reader = command.ExecuteReader();

                if (!reader.Read())
                    return Result.Fail($"El medicamento con id {detalle.IdMedicamento} no existe.");

                string nombreMedicamento = StringHelper.LimpiarEspacios(reader["nombre"]?.ToString());
                int stock = Convert.ToInt32(reader["stock"]);
                int estado = Convert.ToInt32(reader["estado"]);

                reader.Close();

                if (estado != 1)
                    return Result.Fail($"El medicamento {nombreMedicamento} esta inactivo.");

                if (stock <= 0)
                    return Result.Fail($"El medicamento {nombreMedicamento} no tiene stock disponible.");

                if (stock < detalle.Cantidad)
                    return Result.Fail($"Stock insuficiente para el medicamento {nombreMedicamento}.");

                if (estado != 1)
                    return Result.Fail($"El medicamento con id {detalle.IdMedicamento} está inactivo.");

                if (stock <= 0)
                    return Result.Fail($"El medicamento con id {detalle.IdMedicamento} no tiene stock disponible.");

                if (stock < detalle.Cantidad)
                    return Result.Fail($"Stock insuficiente para el medicamento con id {detalle.IdMedicamento}.");
            }

            return Result.Ok();
        }

        private Result DescontarStock(NpgsqlConnection connection, NpgsqlTransaction transaction, int idMedicamento, int cantidad)
        {
            string query = @"UPDATE medicamento
                             SET stock = stock - @cantidad
                             WHERE id = @id_medicamento
                               AND estado = 1
                               AND stock >= @cantidad";

            NpgsqlCommand command = new NpgsqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@cantidad", cantidad);
            command.Parameters.AddWithValue("@id_medicamento", idMedicamento);

            int filas = command.ExecuteNonQuery();

            if (filas <= 0)
                return Result.Fail($"No se pudo descontar stock del medicamento con id {idMedicamento}.");

            return Result.Ok();
        }

        private bool ClienteActivo(NpgsqlConnection connection, NpgsqlTransaction transaction, int idCliente)
        {
            string query = @"SELECT COUNT(*)
                             FROM cliente
                             WHERE id = @id
                               AND estado = 1";

            NpgsqlCommand command = new NpgsqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id", idCliente);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private bool UsuarioActivo(NpgsqlConnection connection, NpgsqlTransaction transaction, int idUsuario)
        {
            string query = @"SELECT COUNT(*)
                             FROM usuario
                             WHERE id = @id
                               AND activo = 1";

            NpgsqlCommand command = new NpgsqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id", idUsuario);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private List<DetalleVenta> GetDetallesByVentaIdTransaccional(NpgsqlConnection connection, NpgsqlTransaction transaction, int idVenta)
        {
            List<DetalleVenta> detalles = new();

            string query = @"SELECT cantidad,
                                    precio_unitario,
                                    id_venta,
                                    id_medicamento
                             FROM detalle_venta
                             WHERE id_venta = @id_venta";

            NpgsqlCommand command = new NpgsqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@id_venta", idVenta);

            using NpgsqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                detalles.Add(new DetalleVenta
                {
                    Cantidad = Convert.ToInt32(reader["cantidad"]),
                    PrecioUnitario = Convert.ToDecimal(reader["precio_unitario"]),
                    IdVenta = Convert.ToInt32(reader["id_venta"]),
                    IdMedicamento = Convert.ToInt32(reader["id_medicamento"])
                });
            }

            return detalles;
        }
        public int Count()
        {
            string query = "SELECT COUNT(*) FROM venta";

            using (NpgsqlConnection connection = database.CreateConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                connection.Open();

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }        
    }
}

