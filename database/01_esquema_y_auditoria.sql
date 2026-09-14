-- VITALCARE | Esquema reconstruido desde los repositorios C# (2026-09-12).
-- Objetivo: MySQL 8.4 LTS. No compatible con MySQL 5.7 ni MariaDB.
-- INSTALACION NUEVA: ejecutar una vez en una base vacia, deteniendose al primer error.
-- No es una migracion: no borra tablas ni adapta estructuras preexistentes.
-- Si cambias el nombre, cambia CREATE DATABASE y USE en los scripts relacionados.
-- DDL produce commits implicitos: un error puede dejar una instalacion parcial.
CREATE DATABASE IF NOT EXISTS farmacia
  CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE farmacia;
SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;
SET SESSION sql_mode = 'STRICT_TRANS_TABLES,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';
SET @audit_usuario_id = NULL, @audit_solicitud_id = NULL, @audit_motivo = NULL;

CREATE TABLE usuario (
  id INT NOT NULL AUTO_INCREMENT,
  nombres VARCHAR(100) NOT NULL,
  apellido_paterno VARCHAR(100) NOT NULL,
  apellido_materno VARCHAR(100) NULL,
  ci VARCHAR(20) NOT NULL,
  ci_extencion VARCHAR(10) NOT NULL,
  telefono VARCHAR(20) NOT NULL,
  email VARCHAR(255) NOT NULL,
  user_name VARCHAR(100) NOT NULL,
  password_hash VARCHAR(255) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  role VARCHAR(20) COLLATE utf8mb4_0900_as_cs NOT NULL DEFAULT 'Bioquimico',
  activo TINYINT NOT NULL DEFAULT 1,
  must_change_password TINYINT NOT NULL DEFAULT 1,
  fecha_registro DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  ultima_actualizacion DATETIME(6) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  id_usuario INT NULL COMMENT 'Creador o ultimo editor informado por la aplicacion; no es una bitacora',
  PRIMARY KEY (id),
  UNIQUE KEY uq_usuario_email (email),
  UNIQUE KEY uq_usuario_user_name (user_name),
  KEY ix_usuario_activo_nombre (activo, nombres, apellido_paterno),
  KEY ix_usuario_rol_activo (role, activo),
  KEY ix_usuario_documento (ci, ci_extencion),
  CONSTRAINT fk_usuario_editor FOREIGN KEY (id_usuario) REFERENCES usuario(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT ck_usuario_activo CHECK (activo IN (0,1)),
  CONSTRAINT ck_usuario_cambio_password CHECK (must_change_password IN (0,1)),
  CONSTRAINT ck_usuario_rol CHECK (role IN ('Admin','Bioquimico')),
  CONSTRAINT ck_usuario_email CHECK (CHAR_LENGTH(TRIM(email)) > 0),
  CONSTRAINT ck_usuario_nombre_login CHECK (CHAR_LENGTH(TRIM(user_name)) > 0),
  CONSTRAINT ck_usuario_password CHECK (CHAR_LENGTH(password_hash) >= 60)
) ENGINE=InnoDB;

CREATE TABLE usuario_token (
  id INT NOT NULL AUTO_INCREMENT,
  usuario_idUsuario INT NOT NULL,
  token_hash CHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  tipo_token VARCHAR(45) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  fecha_creacion DATETIME(6) NOT NULL COMMENT 'UTC: la aplicacion envia DateTime.UtcNow',
  fecha_expiracion DATETIME(6) NOT NULL COMMENT 'UTC',
  revocado TINYINT NOT NULL DEFAULT 0,
  usado TINYINT NOT NULL DEFAULT 0,
  fecha_uso DATETIME(6) NULL COMMENT 'UTC',
  fecha_revocacion DATETIME(6) NULL COMMENT 'UTC',
  PRIMARY KEY (id),
  UNIQUE KEY uq_usuario_token_hash (token_hash),
  KEY ix_token_usuario_tipo_estado (usuario_idUsuario, tipo_token, revocado, usado),
  KEY ix_token_expiracion (fecha_expiracion),
  KEY ix_token_uso (usado, fecha_uso),
  KEY ix_token_revocacion (revocado, fecha_revocacion),
  CONSTRAINT fk_token_usuario FOREIGN KEY (usuario_idUsuario) REFERENCES usuario(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT ck_token_revocado CHECK (revocado IN (0,1)),
  CONSTRAINT ck_token_usado CHECK (usado IN (0,1)),
  CONSTRAINT ck_token_expiracion CHECK (fecha_expiracion > fecha_creacion),
  CONSTRAINT ck_token_hash CHECK (CHAR_LENGTH(token_hash) = 64),
  CONSTRAINT ck_token_tipo CHECK (CHAR_LENGTH(TRIM(tipo_token)) > 0)
) ENGINE=InnoDB;

CREATE TABLE clasificacion (
  id INT NOT NULL AUTO_INCREMENT,
  nombre VARCHAR(45) NOT NULL,
  origen VARCHAR(45) NOT NULL,
  descripcion VARCHAR(100) NOT NULL,
  estado TINYINT NOT NULL DEFAULT 1,
  fecha_registro DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  ultima_actualizacion DATETIME(6) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  id_usuario INT NULL,
  nombre_activo VARCHAR(45) GENERATED ALWAYS AS (CASE WHEN estado = 1 THEN nombre ELSE NULL END) STORED,
  PRIMARY KEY (id),
  UNIQUE KEY uq_clasificacion_nombre_activo (nombre_activo),
  KEY ix_clasificacion_estado_nombre (estado, nombre),
  CONSTRAINT fk_clasificacion_usuario FOREIGN KEY (id_usuario) REFERENCES usuario(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT ck_clasificacion_estado CHECK (estado IN (0,1)),
  CONSTRAINT ck_clasificacion_nombre CHECK (CHAR_LENGTH(TRIM(nombre)) BETWEEN 3 AND 45)
) ENGINE=InnoDB;

CREATE TABLE cliente (
  id INT NOT NULL AUTO_INCREMENT,
  nit VARCHAR(20) NOT NULL COMMENT 'Texto: conserva ceros iniciales y permite CF',
  razon_social VARCHAR(45) NOT NULL,
  correo_electronico VARCHAR(45) NULL,
  estado TINYINT NOT NULL DEFAULT 1,
  fecha_registro DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  ultima_actualizacion DATETIME(6) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  id_usuario INT NULL,
  nit_activo VARCHAR(20) GENERATED ALWAYS AS
    (CASE WHEN estado = 1 AND UPPER(nit) <> 'CF' THEN nit ELSE NULL END) STORED,
  PRIMARY KEY (id),
  UNIQUE KEY uq_cliente_nit_activo (nit_activo),
  KEY ix_cliente_estado_razon (estado, razon_social),
  CONSTRAINT fk_cliente_usuario FOREIGN KEY (id_usuario) REFERENCES usuario(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT ck_cliente_estado CHECK (estado IN (0,1)),
  CONSTRAINT ck_cliente_nit CHECK (CHAR_LENGTH(TRIM(nit)) > 0),
  CONSTRAINT ck_cliente_razon CHECK (CHAR_LENGTH(TRIM(razon_social)) > 0)
) ENGINE=InnoDB;

CREATE TABLE medicamento (
  id INT NOT NULL AUTO_INCREMENT,
  nombre VARCHAR(100) NOT NULL,
  presentacion VARCHAR(100) NOT NULL,
  id_clasificacion INT NOT NULL,
  concentracion VARCHAR(100) NOT NULL,
  precio DECIMAL(12,2) NOT NULL,
  stock INT NOT NULL DEFAULT 0,
  estado TINYINT NOT NULL DEFAULT 1,
  fecha_registro DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  ultima_actualizacion DATETIME(6) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  id_usuario INT NULL,
  PRIMARY KEY (id),
  KEY ix_medicamento_estado_nombre (estado, nombre),
  KEY ix_medicamento_clasificacion_estado (id_clasificacion, estado),
  CONSTRAINT fk_medicamento_clasificacion FOREIGN KEY (id_clasificacion) REFERENCES clasificacion(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT fk_medicamento_usuario FOREIGN KEY (id_usuario) REFERENCES usuario(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT ck_medicamento_precio CHECK (precio > 0 AND precio <= 1000),
  -- No limitar el stock total a 100000: las entradas acumuladas y devoluciones pueden superarlo.
  CONSTRAINT ck_medicamento_stock CHECK (stock >= 0),
  CONSTRAINT ck_medicamento_estado CHECK (estado IN (0,1)),
  CONSTRAINT ck_medicamento_nombre CHECK (CHAR_LENGTH(TRIM(nombre)) BETWEEN 3 AND 100)
) ENGINE=InnoDB;

CREATE TABLE venta (
  id INT NOT NULL AUTO_INCREMENT,
  fecha_hora DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  total DECIMAL(18,2) NOT NULL,
  metodo_pago VARCHAR(45) NOT NULL,
  Cliente_idCliente INT NOT NULL,
  usuario_idUsuario INT NOT NULL,
  nit VARCHAR(20) NOT NULL COMMENT 'Copia del NIT al registrar/editar la venta',
  razon_social VARCHAR(45) NOT NULL COMMENT 'Copia del nombre al registrar/editar la venta',
  estado TINYINT NOT NULL DEFAULT 1,
  fecha_registro DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  ultima_actualizacion DATETIME(6) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  Id_usuario_editor INT NULL,
  PRIMARY KEY (id),
  KEY ix_venta_estado_fecha (estado, fecha_hora),
  KEY ix_venta_cliente_fecha (Cliente_idCliente, fecha_hora),
  KEY ix_venta_usuario_fecha (usuario_idUsuario, fecha_hora),
  CONSTRAINT fk_venta_cliente FOREIGN KEY (Cliente_idCliente) REFERENCES cliente(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT fk_venta_usuario FOREIGN KEY (usuario_idUsuario) REFERENCES usuario(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT fk_venta_editor FOREIGN KEY (Id_usuario_editor) REFERENCES usuario(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT ck_venta_total CHECK (total > 0),
  CONSTRAINT ck_venta_estado CHECK (estado IN (0,1)),
  CONSTRAINT ck_venta_pago CHECK (CHAR_LENGTH(TRIM(metodo_pago)) > 0)
) ENGINE=InnoDB;

CREATE TABLE detalle_venta (
  id_venta INT NOT NULL,
  id_medicamento INT NOT NULL,
  cantidad INT NOT NULL,
  precio_unitario DECIMAL(12,2) NOT NULL,
  PRIMARY KEY (id_venta, id_medicamento),
  KEY ix_detalle_medicamento (id_medicamento),
  CONSTRAINT fk_detalle_venta FOREIGN KEY (id_venta) REFERENCES venta(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT fk_detalle_medicamento FOREIGN KEY (id_medicamento) REFERENCES medicamento(id) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT ck_detalle_cantidad CHECK (cantidad > 0),
  CONSTRAINT ck_detalle_precio CHECK (precio_unitario > 0)
) ENGINE=InnoDB;

CREATE TABLE auditoria (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  fecha_utc DATETIME(6) NOT NULL,
  tabla VARCHAR(64) NOT NULL,
  operacion VARCHAR(16) NOT NULL,
  clave_registro JSON NOT NULL,
  usuario_app_id INT NULL COMMENT 'Solo contexto explicito; NULL si el backend no lo proporciona',
  origen_actor VARCHAR(24) NOT NULL,
  usuario_mysql VARCHAR(288) NOT NULL COMMENT 'USER(): identidad de la conexion, no CURRENT_USER() del definer',
  conexion_id BIGINT UNSIGNED NOT NULL,
  solicitud_id VARCHAR(64) NULL,
  motivo VARCHAR(255) NULL,
  datos_anteriores JSON NULL,
  datos_nuevos JSON NULL,
  credencial_modificada TINYINT NOT NULL DEFAULT 0,
  PRIMARY KEY (id),
  KEY ix_auditoria_tabla_fecha (tabla, fecha_utc),
  KEY ix_auditoria_actor_fecha (usuario_app_id, fecha_utc),
  KEY ix_auditoria_solicitud (solicitud_id),
  KEY ix_auditoria_fecha (fecha_utc),
  CONSTRAINT ck_auditoria_operacion CHECK (operacion IN ('INSERT','UPDATE','DELETE','BAJA_LOGICA','REACTIVACION')),
  CONSTRAINT ck_auditoria_origen CHECK (origen_actor IN ('CONTEXTO_SESION','SIN_CONTEXTO')),
  CONSTRAINT ck_auditoria_actor CHECK (usuario_app_id IS NULL OR usuario_app_id > 0)
  -- Sin FK al actor ni al registro: la historia debe sobrevivir a su eliminacion.
) ENGINE=InnoDB;

-- Los triggers AFTER de abajo solo registran cambios. No modifican stock ni totales.
-- No hay CASCADE: los borrados directos de detalle/token deben activar su auditoria.
DELIMITER $$

-- El repositorio usa NOW() para tokens, pero los interpreta como UTC al leerlos.
CREATE TRIGGER tr_usuario_token_normalizar_utc BEFORE UPDATE ON usuario_token
FOR EACH ROW
BEGIN
  IF OLD.usado = 0 AND NEW.usado = 1 THEN
    SET NEW.fecha_uso = UTC_TIMESTAMP(6);
  END IF;
  IF OLD.revocado = 0 AND NEW.revocado = 1 THEN
    SET NEW.fecha_revocacion = UTC_TIMESTAMP(6);
  END IF;
END$$

CREATE TRIGGER tr_auditoria_no_update BEFORE UPDATE ON auditoria
FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La auditoria no permite UPDATE'$$
CREATE TRIGGER tr_auditoria_no_delete BEFORE DELETE ON auditoria
FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La auditoria no permite DELETE'$$

CREATE TRIGGER tr_usuario_audit_insert AFTER INSERT ON usuario
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'usuario', 'INSERT',
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     NULL,
     JSON_OBJECT('id', NEW.id, 'nombres', NEW.nombres, 'apellido_paterno', NEW.apellido_paterno, 'apellido_materno', NEW.apellido_materno, 'ci', NEW.ci, 'ci_extencion', NEW.ci_extencion, 'telefono', NEW.telefono, 'email', NEW.email, 'user_name', NEW.user_name, 'role', NEW.role, 'activo', NEW.activo, 'must_change_password', NEW.must_change_password, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'id_usuario', NEW.id_usuario),
     0);
END$$

CREATE TRIGGER tr_usuario_audit_update AFTER UPDATE ON usuario
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'usuario', CASE WHEN OLD.activo = 1 AND NEW.activo = 0 THEN 'BAJA_LOGICA'
      WHEN OLD.activo = 0 AND NEW.activo = 1 THEN 'REACTIVACION' ELSE 'UPDATE' END,
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'nombres', OLD.nombres, 'apellido_paterno', OLD.apellido_paterno, 'apellido_materno', OLD.apellido_materno, 'ci', OLD.ci, 'ci_extencion', OLD.ci_extencion, 'telefono', OLD.telefono, 'email', OLD.email, 'user_name', OLD.user_name, 'role', OLD.role, 'activo', OLD.activo, 'must_change_password', OLD.must_change_password, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'id_usuario', OLD.id_usuario),
     JSON_OBJECT('id', NEW.id, 'nombres', NEW.nombres, 'apellido_paterno', NEW.apellido_paterno, 'apellido_materno', NEW.apellido_materno, 'ci', NEW.ci, 'ci_extencion', NEW.ci_extencion, 'telefono', NEW.telefono, 'email', NEW.email, 'user_name', NEW.user_name, 'role', NEW.role, 'activo', NEW.activo, 'must_change_password', NEW.must_change_password, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'id_usuario', NEW.id_usuario),
     NOT (OLD.password_hash <=> NEW.password_hash));
END$$

CREATE TRIGGER tr_usuario_audit_delete AFTER DELETE ON usuario
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'usuario', 'DELETE',
     JSON_OBJECT('id', OLD.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'nombres', OLD.nombres, 'apellido_paterno', OLD.apellido_paterno, 'apellido_materno', OLD.apellido_materno, 'ci', OLD.ci, 'ci_extencion', OLD.ci_extencion, 'telefono', OLD.telefono, 'email', OLD.email, 'user_name', OLD.user_name, 'role', OLD.role, 'activo', OLD.activo, 'must_change_password', OLD.must_change_password, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'id_usuario', OLD.id_usuario),
     NULL,
     0);
END$$

CREATE TRIGGER tr_usuario_token_audit_insert AFTER INSERT ON usuario_token
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'usuario_token', 'INSERT',
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     NULL,
     JSON_OBJECT('id', NEW.id, 'usuario_idUsuario', NEW.usuario_idUsuario, 'tipo_token', NEW.tipo_token, 'fecha_creacion', NEW.fecha_creacion, 'fecha_expiracion', NEW.fecha_expiracion, 'revocado', NEW.revocado, 'usado', NEW.usado, 'fecha_uso', NEW.fecha_uso, 'fecha_revocacion', NEW.fecha_revocacion),
     0);
END$$

CREATE TRIGGER tr_usuario_token_audit_update AFTER UPDATE ON usuario_token
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'usuario_token', 'UPDATE',
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'usuario_idUsuario', OLD.usuario_idUsuario, 'tipo_token', OLD.tipo_token, 'fecha_creacion', OLD.fecha_creacion, 'fecha_expiracion', OLD.fecha_expiracion, 'revocado', OLD.revocado, 'usado', OLD.usado, 'fecha_uso', OLD.fecha_uso, 'fecha_revocacion', OLD.fecha_revocacion),
     JSON_OBJECT('id', NEW.id, 'usuario_idUsuario', NEW.usuario_idUsuario, 'tipo_token', NEW.tipo_token, 'fecha_creacion', NEW.fecha_creacion, 'fecha_expiracion', NEW.fecha_expiracion, 'revocado', NEW.revocado, 'usado', NEW.usado, 'fecha_uso', NEW.fecha_uso, 'fecha_revocacion', NEW.fecha_revocacion),
     NOT (OLD.token_hash <=> NEW.token_hash));
END$$

CREATE TRIGGER tr_usuario_token_audit_delete AFTER DELETE ON usuario_token
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'usuario_token', 'DELETE',
     JSON_OBJECT('id', OLD.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'usuario_idUsuario', OLD.usuario_idUsuario, 'tipo_token', OLD.tipo_token, 'fecha_creacion', OLD.fecha_creacion, 'fecha_expiracion', OLD.fecha_expiracion, 'revocado', OLD.revocado, 'usado', OLD.usado, 'fecha_uso', OLD.fecha_uso, 'fecha_revocacion', OLD.fecha_revocacion),
     NULL,
     0);
END$$

CREATE TRIGGER tr_clasificacion_audit_insert AFTER INSERT ON clasificacion
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'clasificacion', 'INSERT',
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     NULL,
     JSON_OBJECT('id', NEW.id, 'nombre', NEW.nombre, 'origen', NEW.origen, 'descripcion', NEW.descripcion, 'estado', NEW.estado, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'id_usuario', NEW.id_usuario),
     0);
END$$

CREATE TRIGGER tr_clasificacion_audit_update AFTER UPDATE ON clasificacion
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'clasificacion', CASE WHEN OLD.estado = 1 AND NEW.estado = 0 THEN 'BAJA_LOGICA'
      WHEN OLD.estado = 0 AND NEW.estado = 1 THEN 'REACTIVACION' ELSE 'UPDATE' END,
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'nombre', OLD.nombre, 'origen', OLD.origen, 'descripcion', OLD.descripcion, 'estado', OLD.estado, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'id_usuario', OLD.id_usuario),
     JSON_OBJECT('id', NEW.id, 'nombre', NEW.nombre, 'origen', NEW.origen, 'descripcion', NEW.descripcion, 'estado', NEW.estado, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'id_usuario', NEW.id_usuario),
     0);
END$$

CREATE TRIGGER tr_clasificacion_audit_delete AFTER DELETE ON clasificacion
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'clasificacion', 'DELETE',
     JSON_OBJECT('id', OLD.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'nombre', OLD.nombre, 'origen', OLD.origen, 'descripcion', OLD.descripcion, 'estado', OLD.estado, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'id_usuario', OLD.id_usuario),
     NULL,
     0);
END$$

CREATE TRIGGER tr_cliente_audit_insert AFTER INSERT ON cliente
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'cliente', 'INSERT',
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     NULL,
     JSON_OBJECT('id', NEW.id, 'nit', NEW.nit, 'razon_social', NEW.razon_social, 'correo_electronico', NEW.correo_electronico, 'estado', NEW.estado, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'id_usuario', NEW.id_usuario),
     0);
END$$

CREATE TRIGGER tr_cliente_audit_update AFTER UPDATE ON cliente
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'cliente', CASE WHEN OLD.estado = 1 AND NEW.estado = 0 THEN 'BAJA_LOGICA'
      WHEN OLD.estado = 0 AND NEW.estado = 1 THEN 'REACTIVACION' ELSE 'UPDATE' END,
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'nit', OLD.nit, 'razon_social', OLD.razon_social, 'correo_electronico', OLD.correo_electronico, 'estado', OLD.estado, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'id_usuario', OLD.id_usuario),
     JSON_OBJECT('id', NEW.id, 'nit', NEW.nit, 'razon_social', NEW.razon_social, 'correo_electronico', NEW.correo_electronico, 'estado', NEW.estado, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'id_usuario', NEW.id_usuario),
     0);
END$$

CREATE TRIGGER tr_cliente_audit_delete AFTER DELETE ON cliente
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'cliente', 'DELETE',
     JSON_OBJECT('id', OLD.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'nit', OLD.nit, 'razon_social', OLD.razon_social, 'correo_electronico', OLD.correo_electronico, 'estado', OLD.estado, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'id_usuario', OLD.id_usuario),
     NULL,
     0);
END$$

CREATE TRIGGER tr_medicamento_audit_insert AFTER INSERT ON medicamento
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'medicamento', 'INSERT',
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     NULL,
     JSON_OBJECT('id', NEW.id, 'nombre', NEW.nombre, 'presentacion', NEW.presentacion, 'id_clasificacion', NEW.id_clasificacion, 'concentracion', NEW.concentracion, 'precio', NEW.precio, 'stock', NEW.stock, 'estado', NEW.estado, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'id_usuario', NEW.id_usuario),
     0);
END$$

CREATE TRIGGER tr_medicamento_audit_update AFTER UPDATE ON medicamento
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'medicamento', CASE WHEN OLD.estado = 1 AND NEW.estado = 0 THEN 'BAJA_LOGICA'
      WHEN OLD.estado = 0 AND NEW.estado = 1 THEN 'REACTIVACION' ELSE 'UPDATE' END,
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'nombre', OLD.nombre, 'presentacion', OLD.presentacion, 'id_clasificacion', OLD.id_clasificacion, 'concentracion', OLD.concentracion, 'precio', OLD.precio, 'stock', OLD.stock, 'estado', OLD.estado, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'id_usuario', OLD.id_usuario),
     JSON_OBJECT('id', NEW.id, 'nombre', NEW.nombre, 'presentacion', NEW.presentacion, 'id_clasificacion', NEW.id_clasificacion, 'concentracion', NEW.concentracion, 'precio', NEW.precio, 'stock', NEW.stock, 'estado', NEW.estado, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'id_usuario', NEW.id_usuario),
     0);
END$$

CREATE TRIGGER tr_medicamento_audit_delete AFTER DELETE ON medicamento
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'medicamento', 'DELETE',
     JSON_OBJECT('id', OLD.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'nombre', OLD.nombre, 'presentacion', OLD.presentacion, 'id_clasificacion', OLD.id_clasificacion, 'concentracion', OLD.concentracion, 'precio', OLD.precio, 'stock', OLD.stock, 'estado', OLD.estado, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'id_usuario', OLD.id_usuario),
     NULL,
     0);
END$$

CREATE TRIGGER tr_venta_audit_insert AFTER INSERT ON venta
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'venta', 'INSERT',
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     NULL,
     JSON_OBJECT('id', NEW.id, 'fecha_hora', NEW.fecha_hora, 'total', NEW.total, 'metodo_pago', NEW.metodo_pago, 'Cliente_idCliente', NEW.Cliente_idCliente, 'usuario_idUsuario', NEW.usuario_idUsuario, 'nit', NEW.nit, 'razon_social', NEW.razon_social, 'estado', NEW.estado, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'Id_usuario_editor', NEW.Id_usuario_editor),
     0);
END$$

CREATE TRIGGER tr_venta_audit_update AFTER UPDATE ON venta
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'venta', CASE WHEN OLD.estado = 1 AND NEW.estado = 0 THEN 'BAJA_LOGICA'
      WHEN OLD.estado = 0 AND NEW.estado = 1 THEN 'REACTIVACION' ELSE 'UPDATE' END,
     JSON_OBJECT('id', NEW.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'fecha_hora', OLD.fecha_hora, 'total', OLD.total, 'metodo_pago', OLD.metodo_pago, 'Cliente_idCliente', OLD.Cliente_idCliente, 'usuario_idUsuario', OLD.usuario_idUsuario, 'nit', OLD.nit, 'razon_social', OLD.razon_social, 'estado', OLD.estado, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'Id_usuario_editor', OLD.Id_usuario_editor),
     JSON_OBJECT('id', NEW.id, 'fecha_hora', NEW.fecha_hora, 'total', NEW.total, 'metodo_pago', NEW.metodo_pago, 'Cliente_idCliente', NEW.Cliente_idCliente, 'usuario_idUsuario', NEW.usuario_idUsuario, 'nit', NEW.nit, 'razon_social', NEW.razon_social, 'estado', NEW.estado, 'fecha_registro', NEW.fecha_registro, 'ultima_actualizacion', NEW.ultima_actualizacion, 'Id_usuario_editor', NEW.Id_usuario_editor),
     0);
END$$

CREATE TRIGGER tr_venta_audit_delete AFTER DELETE ON venta
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'venta', 'DELETE',
     JSON_OBJECT('id', OLD.id),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id', OLD.id, 'fecha_hora', OLD.fecha_hora, 'total', OLD.total, 'metodo_pago', OLD.metodo_pago, 'Cliente_idCliente', OLD.Cliente_idCliente, 'usuario_idUsuario', OLD.usuario_idUsuario, 'nit', OLD.nit, 'razon_social', OLD.razon_social, 'estado', OLD.estado, 'fecha_registro', OLD.fecha_registro, 'ultima_actualizacion', OLD.ultima_actualizacion, 'Id_usuario_editor', OLD.Id_usuario_editor),
     NULL,
     0);
END$$

CREATE TRIGGER tr_detalle_venta_audit_insert AFTER INSERT ON detalle_venta
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'detalle_venta', 'INSERT',
     JSON_OBJECT('id_venta', NEW.id_venta, 'id_medicamento', NEW.id_medicamento),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     NULL,
     JSON_OBJECT('id_venta', NEW.id_venta, 'id_medicamento', NEW.id_medicamento, 'cantidad', NEW.cantidad, 'precio_unitario', NEW.precio_unitario),
     0);
END$$

CREATE TRIGGER tr_detalle_venta_audit_update AFTER UPDATE ON detalle_venta
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'detalle_venta', 'UPDATE',
     JSON_OBJECT('id_venta', NEW.id_venta, 'id_medicamento', NEW.id_medicamento),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id_venta', OLD.id_venta, 'id_medicamento', OLD.id_medicamento, 'cantidad', OLD.cantidad, 'precio_unitario', OLD.precio_unitario),
     JSON_OBJECT('id_venta', NEW.id_venta, 'id_medicamento', NEW.id_medicamento, 'cantidad', NEW.cantidad, 'precio_unitario', NEW.precio_unitario),
     0);
END$$

CREATE TRIGGER tr_detalle_venta_audit_delete AFTER DELETE ON detalle_venta
FOR EACH ROW
BEGIN
  INSERT INTO auditoria
    (fecha_utc, tabla, operacion, clave_registro, usuario_app_id, origen_actor,
     usuario_mysql, conexion_id, solicitud_id, motivo, datos_anteriores, datos_nuevos, credencial_modificada)
  VALUES
    (UTC_TIMESTAMP(6), 'detalle_venta', 'DELETE',
     JSON_OBJECT('id_venta', OLD.id_venta, 'id_medicamento', OLD.id_medicamento),
     NULLIF(@audit_usuario_id, 0),
     IF(NULLIF(@audit_usuario_id, 0) IS NULL, 'SIN_CONTEXTO', 'CONTEXTO_SESION'),
     USER(), CONNECTION_ID(), LEFT(@audit_solicitud_id, 64), LEFT(@audit_motivo, 255),
     JSON_OBJECT('id_venta', OLD.id_venta, 'id_medicamento', OLD.id_medicamento, 'cantidad', OLD.cantidad, 'precio_unitario', OLD.precio_unitario),
     NULL,
     0);
END$$

DELIMITER ;

CREATE SQL SECURITY INVOKER VIEW vw_auditoria_stock AS
SELECT id AS auditoria_id, fecha_utc, usuario_app_id, origen_actor, solicitud_id,
       CAST(JSON_UNQUOTE(JSON_EXTRACT(clave_registro, '$.id')) AS SIGNED) AS id_medicamento,
       COALESCE(CAST(JSON_UNQUOTE(JSON_EXTRACT(datos_anteriores, '$.stock')) AS SIGNED), 0) AS stock_anterior,
       CAST(JSON_UNQUOTE(JSON_EXTRACT(datos_nuevos, '$.stock')) AS SIGNED) AS stock_nuevo,
       CAST(JSON_UNQUOTE(JSON_EXTRACT(datos_nuevos, '$.stock')) AS SIGNED)
         - COALESCE(CAST(JSON_UNQUOTE(JSON_EXTRACT(datos_anteriores, '$.stock')) AS SIGNED), 0) AS variacion,
       operacion, motivo
FROM auditoria
WHERE tabla = 'medicamento' AND datos_nuevos IS NOT NULL
  AND (datos_anteriores IS NULL OR
       JSON_EXTRACT(datos_anteriores, '$.stock') <> JSON_EXTRACT(datos_nuevos, '$.stock'));

-- Informativa: no recalcula ni modifica una venta. Consultar tras COMMIT;
-- la propia transaccion escritora puede ver estados intermedios antes de confirmarse.
CREATE SQL SECURITY INVOKER VIEW vw_ventas_descuadradas AS
SELECT v.id, v.estado, v.total AS total_cabecera,
       COALESCE(SUM(d.cantidad * d.precio_unitario), 0) AS total_detalles,
       COUNT(d.id_medicamento) AS lineas,
       v.total - COALESCE(SUM(d.cantidad * d.precio_unitario), 0) AS diferencia
FROM venta v LEFT JOIN detalle_venta d ON d.id_venta = v.id
GROUP BY v.id, v.estado, v.total
HAVING COUNT(d.id_medicamento) = 0 OR v.total <> COALESCE(SUM(d.cantidad * d.precio_unitario), 0);

SELECT 'Esquema creado: 7 tablas de negocio, 1 de auditoria, 24 triggers y 2 vistas.' AS resultado;
