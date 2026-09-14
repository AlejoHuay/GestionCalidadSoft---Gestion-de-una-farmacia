-- Opcional: ejecutar como administrador MySQL. No crea cuentas con passwords publicas.
-- Los roles son objetos del servidor; estos nombres se reservan para esta aplicacion.
USE farmacia;
CREATE ROLE IF NOT EXISTS 'vitalcare_app', 'vitalcare_auditor';

GRANT SELECT, INSERT, UPDATE ON farmacia.usuario TO 'vitalcare_app';
GRANT SELECT, INSERT, UPDATE, DELETE ON farmacia.usuario_token TO 'vitalcare_app';
GRANT SELECT, INSERT, UPDATE ON farmacia.clasificacion TO 'vitalcare_app';
GRANT SELECT, INSERT, UPDATE ON farmacia.cliente TO 'vitalcare_app';
GRANT SELECT, INSERT, UPDATE ON farmacia.medicamento TO 'vitalcare_app';
GRANT SELECT, INSERT, UPDATE ON farmacia.venta TO 'vitalcare_app';
GRANT SELECT, INSERT, UPDATE, DELETE ON farmacia.detalle_venta TO 'vitalcare_app';
-- Los triggers escriben en auditoria con los privilegios de quien los creo.
-- La cuenta de la aplicacion no necesita INSERT/UPDATE/DELETE/TRIGGER/DDL sobre auditoria.
GRANT SELECT ON farmacia.auditoria TO 'vitalcare_auditor';
GRANT SELECT ON farmacia.vw_auditoria_stock TO 'vitalcare_auditor';
GRANT SELECT ON farmacia.vw_ventas_descuadradas TO 'vitalcare_auditor';
-- La vista de conciliacion es SQL SECURITY INVOKER y necesita leer sus tablas base.
GRANT SELECT ON farmacia.venta TO 'vitalcare_auditor';
GRANT SELECT ON farmacia.detalle_venta TO 'vitalcare_auditor';

-- Completar y ejecutar manualmente, usando una cuenta NUEVA y host apropiado:
-- CREATE USER 'farmacia_app'@'localhost' IDENTIFIED BY '<CLAVE_PROPIA>';
-- GRANT 'vitalcare_app' TO 'farmacia_app'@'localhost';
-- SET DEFAULT ROLE 'vitalcare_app' TO 'farmacia_app'@'localhost';
-- SHOW GRANTS FOR 'farmacia_app'@'localhost' USING 'vitalcare_app';
-- No conceder farmacia.*: eso permitiria modificar directamente la auditoria.
