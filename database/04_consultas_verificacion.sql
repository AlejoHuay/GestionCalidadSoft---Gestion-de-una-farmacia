USE farmacia;
SELECT VERSION() AS version_mysql, DATABASE() AS base_actual,
       @@session.time_zone AS zona_sesion, NOW() AS hora_mysql, UTC_TIMESTAMP() AS hora_utc;

SELECT table_name, engine FROM information_schema.tables
WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE' ORDER BY table_name;
SELECT trigger_name, event_manipulation, event_object_table, action_timing
FROM information_schema.triggers WHERE trigger_schema = DATABASE()
ORDER BY event_object_table, action_timing, event_manipulation;
SELECT table_name, constraint_name, constraint_type
FROM information_schema.table_constraints WHERE constraint_schema = DATABASE()
ORDER BY table_name, constraint_type, constraint_name;

-- Debe quedar vacia despues de operaciones confirmadas.
SELECT * FROM vw_ventas_descuadradas;
SELECT * FROM vw_auditoria_stock ORDER BY auditoria_id DESC LIMIT 100;
SELECT id, fecha_utc, tabla, operacion, clave_registro, usuario_app_id,
       origen_actor, usuario_mysql, solicitud_id, credencial_modificada
FROM auditoria ORDER BY id DESC LIMIT 100;

-- Debe devolver 0: los hashes no se copian al historial.
SELECT COUNT(*) AS eventos_con_hash_expuesto FROM auditoria
WHERE JSON_CONTAINS_PATH(datos_anteriores, 'one', '$.password_hash', '$.token_hash') = 1
   OR JSON_CONTAINS_PATH(datos_nuevos, 'one', '$.password_hash', '$.token_hash') = 1;

-- Identifica huecos de atribucion; NO atribuirlos al ultimo editor guardado en la fila.
SELECT tabla, operacion, COUNT(*) AS eventos_sin_actor_aplicacion
FROM auditoria WHERE usuario_app_id IS NULL GROUP BY tabla, operacion;

-- Estas relaciones son validas fisicamente, pero pueden incumplir reglas de estado.
SELECT m.id, m.nombre FROM medicamento m
JOIN clasificacion c ON c.id = m.id_clasificacion WHERE m.estado = 1 AND c.estado = 0;

-- Ejemplo de historial de una venta (sustituir 1 por el identificador buscado).
SET @venta_consulta = 1;
SELECT id, fecha_utc, tabla, operacion, datos_anteriores, datos_nuevos
FROM auditoria
WHERE (tabla = 'venta' AND JSON_EXTRACT(clave_registro, '$.id') = @venta_consulta)
   OR (tabla = 'detalle_venta' AND JSON_EXTRACT(clave_registro, '$.id_venta') = @venta_consulta)
ORDER BY id;
