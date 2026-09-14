# Resultado de la implementación

- Aplicación migrada de MySql.Data a Npgsql 10.0.3; compilación sin errores ni advertencias.
- Conexión directa a Supabase PostgreSQL 17.6 verificada con TLS `VerifyFull` y las CA oficiales.
- Las siete tablas de negocio existentes en Supabase estaban vacías. Se respaldaron en `.sandbox-appdata/supabase-antes-postgresql.sql` y se conservó su estructura anterior como `farmacia_respaldo_pre_npgsql`. La nueva estructura compatible quedó en `farmacia`.
- Suite de **71 comprobaciones** aprobada en PostgreSQL local 18.4. Evidencia en `VALIDACION.txt`.
- Login HTTP real y siete pantallas autenticadas comprobados en una instancia local independiente: inicio, medicamentos, clientes, clasificaciones, usuarios, bioquímicos y ventas. No se enviaron correos.
- Ensayo de importación y comparación completa aprobado con datos ficticios: 1 usuario, 4 tokens, 9 clasificaciones, 7 clientes, 2 medicamentos, 3 ventas, 3 detalles y 62 registros de historial. Se probaron conversiones de booleanos MySQL y fechas de negocio UTC-04:00, además de conservación de hashes y secuencias.
- El cambio de esquema se ensayó primero sobre una copia local de la estructura previa de Supabase.

## Pendiente para los datos reales

No se ha exportado ni importado el MySQL del usuario: falta `MYSQL_SOURCE_CONNECTION` en `.env`. Supabase conserva las tablas de negocio nuevas vacías; no se sembró un administrador que interfiera con la importación. La conexión de aplicación y el proyecto ya arrancan contra Supabase.

Cuando se configure el origen, detén las escrituras y ejecuta Export, Import y Verify según [LEEME.md](LEEME.md). Los fallos de envío SMTP son independientes y requieren las credenciales correctas del proveedor de correo.
