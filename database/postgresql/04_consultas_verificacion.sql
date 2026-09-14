SELECT version(),current_database(),current_schema();
SELECT table_name FROM information_schema.tables WHERE table_schema='farmacia' ORDER BY table_name;
SELECT * FROM farmacia.vw_ventas_descuadradas;
SELECT id,nombre,stock FROM farmacia.medicamento WHERE stock<0;
SELECT id,fecha_utc,tabla,operacion,usuario_app_id,solicitud_id,motivo
FROM farmacia.auditoria ORDER BY id DESC LIMIT 100;
SELECT * FROM farmacia.vw_auditoria_stock ORDER BY id_auditoria DESC LIMIT 100;
-- Debe devolver 0: el historial nuevo no almacena hashes de autenticación.
SELECT count(*) AS registros_con_secretos FROM farmacia.auditoria
WHERE datos_anteriores ?| ARRAY['password_hash','token_hash']
   OR datos_nuevos ?| ARRAY['password_hash','token_hash'];
