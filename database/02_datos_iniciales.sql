-- Ejecutar DESPUES de 01, en la MISMA conexion donde se define @admin_password_hash.
-- Generar un bcrypt propio con tools/Generar-HashAdmin.ps1; nunca SHA2/MD5 ni texto plano.
-- Ejemplo de variables (el hash se obtiene con la herramienta):
-- SET @admin_password_hash = '$2a$12$...';
-- SET @admin_user_name = 'admin';
-- SET @admin_email = 'admin@vitalcare.local';
USE farmacia;
SET NAMES utf8mb4;
SET @admin_user_name = COALESCE(@admin_user_name, 'admin');
SET @admin_email = COALESCE(@admin_email, 'admin@vitalcare.local');

DELIMITER $$
CREATE PROCEDURE sp_instalar_datos_iniciales()
SQL SECURITY INVOKER
BEGIN
  DECLARE v_admin_id INT;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    SET @audit_usuario_id = NULL, @audit_solicitud_id = NULL, @audit_motivo = NULL;
    RESIGNAL;
  END;

  IF @admin_password_hash IS NULL OR CHAR_LENGTH(@admin_password_hash) <> 60
     OR NOT REGEXP_LIKE(@admin_password_hash, '^[$]2[aby][$][0-9]{2}[$][./A-Za-z0-9]{53}$', 'c') THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Define @admin_password_hash con un bcrypt valido de 60 caracteres';
  END IF;
  -- No sobrescribir credenciales ni convertir una cuenta existente en administrador.
  IF EXISTS (SELECT 1 FROM usuario WHERE user_name = @admin_user_name OR email = @admin_email) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya existe el usuario o email inicial; no se modificaron sus credenciales';
  END IF;

  START TRANSACTION;
  SET @audit_usuario_id = NULL, @audit_solicitud_id = UUID(), @audit_motivo = 'Instalacion inicial VitalCare';
  INSERT INTO usuario
    (nombres, apellido_paterno, apellido_materno, ci, ci_extencion, telefono,
     email, user_name, password_hash, role, activo, must_change_password, id_usuario)
  VALUES
    ('Administrador', 'Local', NULL, '10000000', 'LP', '70000000',
     @admin_email, @admin_user_name, @admin_password_hash, 'Admin', 1, 0, NULL);
  SET v_admin_id = LAST_INSERT_ID();

  -- Datos administrativos de arranque; sin medicamentos ni ventas ficticias.
  INSERT INTO clasificacion (nombre, origen, descripcion, id_usuario)
  SELECT s.nombre, 'General', s.descripcion, v_admin_id
  FROM (
    SELECT 'Antibiotico' AS nombre, 'Clasificacion de antibioticos' AS descripcion
    UNION ALL SELECT 'Analgesico', 'Clasificacion de analgesicos'
    UNION ALL SELECT 'Antiinflamatorio', 'Clasificacion de antiinflamatorios'
    UNION ALL SELECT 'Antialergico', 'Clasificacion de antialergicos'
    UNION ALL SELECT 'Antipiretico', 'Clasificacion de antipireticos'
    UNION ALL SELECT 'Vitaminas', 'Clasificacion de vitaminas'
    UNION ALL SELECT 'Antiseptico', 'Clasificacion de antisepticos'
  ) AS s
  WHERE NOT EXISTS (SELECT 1 FROM clasificacion c WHERE c.nombre = s.nombre AND c.estado = 1);

  INSERT INTO cliente (nit, razon_social, correo_electronico, id_usuario)
  SELECT 'CF', 'Consumidor Final', NULL, v_admin_id
  WHERE NOT EXISTS (SELECT 1 FROM cliente WHERE nit = 'CF' AND estado = 1);
  COMMIT;
  SET @audit_usuario_id = NULL, @audit_solicitud_id = NULL, @audit_motivo = NULL;
  SELECT v_admin_id AS id_admin, @admin_user_name AS usuario, 'Datos iniciales creados' AS resultado;
END$$
DELIMITER ;

CALL sp_instalar_datos_iniciales();
DROP PROCEDURE sp_instalar_datos_iniciales;
SET @admin_password_hash = NULL;
-- Si CALL falla y tu cliente se detiene, corrige el motivo y ejecuta CALL de nuevo;
-- al terminar elimina la rutina con DROP PROCEDURE sp_instalar_datos_iniciales.
