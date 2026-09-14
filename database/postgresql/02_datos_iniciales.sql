-- SOLO para una instalacion nueva SIN datos que importar de MySQL.
-- Genera tu bcrypt con database/tools/Generar-HashAdmin.ps1.
-- En esta misma sesion, antes del script:
-- SELECT set_config('app.admin_password_hash', 'TU_HASH_BCRYPT', false);
BEGIN;
SET LOCAL search_path=farmacia,pg_catalog;
DO $$ DECLARE hash text := current_setting('app.admin_password_hash',true); BEGIN
 IF EXISTS(SELECT 1 FROM usuario) THEN RAISE EXCEPTION 'Ya existen usuarios. No se inserto el administrador.'; END IF;
 IF hash IS NULL OR hash !~ '^\$2[aby]\$[0-9]{2}\$[./A-Za-z0-9]{53}$' THEN
   RAISE EXCEPTION 'Define app.admin_password_hash con el bcrypt generado a partir de TU password';
 END IF;
 INSERT INTO usuario(nombres,apellido_paterno,ci,ci_extencion,telefono,email,user_name,password_hash,role,must_change_password)
 VALUES('Administrador','Sistema','10000000','LP','70000000','admin@vitalcare.local','admin',hash,'Admin',0);
END $$;
INSERT INTO clasificacion(nombre,origen,descripcion) VALUES
 ('Analgesicos','General','Tratamiento del dolor'),
 ('Antibioticos','General','Medicamentos antibacterianos'),
 ('Antiinflamatorios','General','Tratamiento de inflamacion'),
 ('Antihistaminicos','General','Medicamentos antialergicos'),
 ('Vitaminas','General','Suplementos vitaminicos'),
 ('Digestivos','General','Medicamentos del aparato digestivo'),
 ('Otros','General','Otras clasificaciones');
INSERT INTO cliente(nit,razon_social) VALUES('CF','Consumidor Final');
COMMIT;
