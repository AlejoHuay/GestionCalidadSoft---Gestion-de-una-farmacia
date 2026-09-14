# PostgreSQL / Supabase

El proyecto utiliza Npgsql 10.0.3 con .NET 9. Los scripts MySQL de la carpeta superior se conservan como referencia del origen. No se ejecutan en Supabase ni se usan desde la aplicación actual.

## Configuración y arranque

En `.env`, junto a `Program.cs`, configura lo siguiente sin subir contraseñas a Git:

```dotenv
ConnectionStrings__PostgresConnection="Host=db.TU_PROYECTO.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=TU_PASSWORD;Search Path=farmacia;SSL Mode=VerifyFull;"
JWT_KEY=TU_CLAVE_LARGA_ALEATORIA
JWT_ISSUER=http://localhost:5081
JWT_AUDIENCE=http://localhost:5081
```

Se conserva la clave JWT local existente. `appsettings.json` tiene la conexión vacía; ASP.NET obtiene el valor real del entorno después de cargar `.env`. La fábrica anterior que leía siempre `appsettings.Development.json` fue reemplazada por configuración e inyección de dependencias.

```powershell
dotnet restore
dotnet run --launch-profile http
```

Abre http://localhost:5081. Detén la instancia anterior con Ctrl+C antes de levantar otra en el mismo puerto.

La conexión directa de Supabase puede requerir IPv6. Para una red solo IPv4, copia la conexión **Session pooler** del panel Connect; conserva exactamente su host y usuario. La aplicación usa un pool de Npgsql y transacciones locales.

Las CA públicas 2021 y 2025 se incluyen en `certs/supabase-ca.crt`, descargadas del [repositorio oficial de Supabase CLI](https://github.com/supabase/cli/tree/develop/apps/cli-go/internal/gen/types/templates). La aplicación y la herramienta configuran ese archivo y `VerifyFull` para hosts `*.supabase.co`. Para un pooler con certificado distinto, configura `Root Certificate` según su panel. No se desactiva la validación del servidor. Certificado incluido: SHA256 `2C3FB535B6689F307B32E418D09902D98D67B48D6AC5D612CD908189D5FA9C4D`.

## Conservar MySQL

Detén las aplicaciones que escriben en MySQL antes de la copia y mantenlas detenidas durante el cambio. La exportación usa una transacción consistente sobre tablas InnoDB y no cambia MySQL. La importación usa una transacción PostgreSQL, conserva IDs, hashes BCrypt, tokens, ventas, precios y stock; difiere las claves foráneas para conservar referencias entre usuarios. Ajusta las secuencias al siguiente ID.

Agrega al `.env`:

```dotenv
MYSQL_SOURCE_CONNECTION="Server=127.0.0.1;Port=3306;Database=farmacia;User ID=root;Password=TU_PASSWORD_MYSQL;"
```

Ejecuta desde la raíz en PowerShell:

```powershell
.\database\postgresql\Gestionar-Base.ps1 -Accion Inspect
.\database\postgresql\Gestionar-Base.ps1 -Accion Export
# Usa la ruta JSON que devuelve Export en los dos comandos siguientes:
.\database\postgresql\Gestionar-Base.ps1 -Accion Import -Archivo '.sandbox-appdata\mysql-FECHA.json'
.\database\postgresql\Gestionar-Base.ps1 -Accion Verify -Archivo '.sandbox-appdata\mysql-FECHA.json'
```

El JSON y su SHA256 quedan en `.sandbox-appdata`, ignorada por Git. Contienen información privada y hashes; conserva la copia en un lugar controlado. `Verify` compara cada campo de cada registro, además de los recuentos. Se aborta ante columnas incompatibles, restricciones incumplidas, una copia modificada o un destino con datos; no se sobrescribe ni se mezclan registros. Cualquier error durante Import revierte la transacción completa. Las inconsistencias de totales del origen se reportan, nunca se corrigen silenciosamente.

Las fechas de negocio MySQL se convierten de la zona del servidor al UTC de PostgreSQL. Las fechas de tokens ya son UTC. La copia registra el desplazamiento del servidor al exportar: si históricamente cambiaste su zona o utilizaste horario de verano, hay que revisar esa conversión antes de importar. En Bolivia el desplazamiento es estable. Las pantallas de ventas muestran la hora local del servidor de la aplicación.

El historial MySQL, si existe, se conserva completo en `auditoria_legacy_mysql`. Los nuevos eventos se escriben en `auditoria`; no se mezclan identificadores ni se reinterpretan eventos históricos. Las inserciones de la migración generan además su auditoría PostgreSQL.

## Scripts

| Archivo | Uso |
|---|---|
| `01_esquema_y_auditoria.sql` | Instalación atómica en esquema nuevo. Aborta si ya existen objetos. |
| `02_datos_iniciales.sql` | Solo instalación nueva sin datos que importar. Requiere bcrypt propio; no trae contraseña predeterminada. |
| `03_roles.sql` | Roles sin login para aplicación y auditor. Otórgalos a cuentas dedicadas; no dan acceso público al esquema. |
| `04_consultas_verificacion.sql` | Listado de tablas, conciliación y consultas de auditoría. |
| `05_actualizar_instalacion_vacia.sql` | Conserva una instalación previa vacía como `farmacia_respaldo_pre_npgsql` y crea el esquema compatible. Aborta si cualquier tabla contiene datos. |

Usa el SQL Editor de Supabase, psql o pgAdmin para estos scripts. MySQL Workbench no administra PostgreSQL. `Gestionar-Base.ps1 -Accion Schema` ejecuta 01; `-Accion UpgradeEmpty` ejecuta 05. La herramienta usa la conexión de `.env`; `PG_TARGET_CONNECTION` permite indicar otra conexión administrativa durante una operación.

## Auditoría y concurrencia

Cada escritura de repositorio abre una transacción e informa el usuario desde los claims autenticados, el identificador de solicitud y la ruta. `set_config(..., true)` limita el contexto a esa transacción; no se arrastra a la siguiente conexión del pool. Una operación anónima no inventa al actor a partir del último editor de la fila.

Siete triggers registran INSERT, UPDATE, DELETE, bajas y reactivaciones; otros cinco mantienen las fechas de modificación. La auditoría evita copiar `password_hash` y `token_hash`. Dos triggers bloquean UPDATE, DELETE y TRUNCATE del historial. El propietario de la base puede alterar estas protecciones; una cuenta de aplicación debe usar `vitalcare_app`, sin permisos directos sobre auditoría. El esquema no concede acceso a PUBLIC ni a `anon`/`authenticated` de la Data API.

Ventas bloquea la cabecera al editar/anular y los medicamentos en orden de ID. Conserva transacciones de stock, detalles y auditoría, y los precios históricos al editar. La activación consume el token y cambia el password de forma atómica. La edición de acceso del usuario conserva sus datos personales.

## Pruebas

`Verificar-Base.ps1` requiere un servidor **local** con una base nueva llamada `farmacia_pruebas` y el esquema 01. Rechaza hosts externos, otras bases o una base con usuarios/auditoría previos. La conexión se entrega mediante `VITALCARE_TEST_CONNECTION`; nunca se imprime.

Comprueba los repositorios reales, BCrypt/JWT, búsquedas con tildes y mayúsculas, restricciones, tokens UTC, rollback, privacidad y permisos de auditoría, aislamiento del actor y ventas/anulaciones/activaciones concurrentes. No envía correos.

SMTP es una configuración independiente. Cambiar a PostgreSQL no resuelve credenciales de Gmail incorrectas ni el error 5.7.0. Los registros que ya se guardaron en MySQL antes de un fallo SMTP también se conservan.

Fuentes: [Npgsql y conexiones](https://www.npgsql.org/doc/basic-usage.html), [fechas UTC](https://www.npgsql.org/doc/types/datetime.html), [TLS en Supabase](https://supabase.com/docs/guides/platform/ssl-enforcement), [contexto transaccional PostgreSQL](https://www.postgresql.org/docs/current/functions-admin.html).
