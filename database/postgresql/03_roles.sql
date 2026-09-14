-- Ejecutar como propietario de farmacia. Las contraseñas se crean fuera de este archivo.
BEGIN;
DO $$ BEGIN
 IF NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname='vitalcare_app') THEN CREATE ROLE vitalcare_app NOLOGIN; END IF;
 IF NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname='vitalcare_auditor') THEN CREATE ROLE vitalcare_auditor NOLOGIN; END IF;
END $$;
GRANT USAGE ON SCHEMA farmacia TO vitalcare_app,vitalcare_auditor;
GRANT SELECT,INSERT,UPDATE ON farmacia.usuario,farmacia.usuario_token,farmacia.cliente,
 farmacia.clasificacion,farmacia.medicamento,farmacia.venta,farmacia.detalle_venta TO vitalcare_app;
GRANT DELETE ON farmacia.usuario_token,farmacia.detalle_venta TO vitalcare_app;
GRANT USAGE ON SEQUENCE farmacia.usuario_id_seq,farmacia.usuario_token_id_seq,farmacia.cliente_id_seq,
 farmacia.clasificacion_id_seq,farmacia.medicamento_id_seq,farmacia.venta_id_seq TO vitalcare_app;
GRANT EXECUTE ON FUNCTION farmacia.normalizar_texto(text) TO vitalcare_app;
GRANT SELECT ON farmacia.auditoria,farmacia.auditoria_legacy_mysql,
 farmacia.vw_auditoria_stock,farmacia.vw_ventas_descuadradas TO vitalcare_auditor;
COMMIT;
