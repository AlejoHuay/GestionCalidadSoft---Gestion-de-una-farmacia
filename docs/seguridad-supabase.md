# Configuración segura de Supabase

## Preparar el entorno

1. Coordinar con el administrador qué aplicaciones usan la contraseña expuesta y rotarla en Supabase. Actualizar los entornos autorizados y comprobar que la contraseña anterior ya no conecta. No registrar ninguna de las dos contraseñas en Git, capturas o tarjetas.
2. Si no existe `.env`, copiar `.env.example` a `.env` en la raíz. No sobrescribir una configuración local existente.
3. Completar `ConnectionStrings__PostgresConnection` con la conexión completa y la contraseña nueva, solo localmente. Completar también `JWT_KEY`, `JWT_ISSUER` y `JWT_AUDIENCE` según el entorno. En despliegues, usar variables de entorno o el almacén seguro del servicio.
4. Ejecutar `dotnet build` y `dotnet run --launch-profile http`.

`Program.cs` carga `.env` mediante `Env.NoClobber().Load()` antes de crear el builder. Las variables ya definidas en el proceso prevalecen. La aplicación reutiliza `GetConnectionString("PostgresConnection")`; no tiene contraseña predeterminada. Los archivos appsettings deben conservar la conexión vacía.

`.env` y sus variantes locales están ignorados; `.env.example` sí se versiona y no contiene secretos. Antes de cada commit, revisar los archivos preparados sin compartir diffs que puedan mostrar secretos eliminados. `git ls-files .env` no debe devolver nada.

## Verificación funcional y cierre

- Confirmar conexión con la contraseña nueva y rechazo de la anterior mediante un cliente que no registre credenciales.
- Probar lectura, creación, edición y eliminación con datos de prueba autorizados. Registrar resultados y limpiar los datos de prueba.
- Para verificar ausencia de configuración, retirar temporalmente la variable del proceso y la entrada de `.env`; comprobar que los appsettings tampoco aportan una conexión. El arranque debe detenerse con el mensaje de configuración requerida, sin mostrar una cadena de conexión. Restaurar después la configuración local.
- Repetir el análisis SonarQube con la clave del proyecto al que pertenece el issue. La tarjeta indica `VitalCare-farmacia-SIS312-Sept`, mientras que la captura indica `Sistema-Farmacia`: no crear por error un proyecto diferente.
- Usar un token obtenido del almacén seguro del entorno, nunca guardarlo en comandos versionados ni capturas. Con SonarScanner instalado y el proyecto confirmado, ejecutar begin, build y end según la configuración del equipo.
- Comprobar que `secrets:S6703` deja de estar abierto y que no aparecen nuevas credenciales. No marcarlo como Accepted o False Positive ni desactivar la regla.
- Adjuntar evidencias del cambio, compilación, pruebas y nuevo análisis con los valores sensibles ocultos.

La rotación es obligatoria aunque la contraseña permanezca en commits antiguos. No reescribir el historial compartido ni hacer force push sin autorización del equipo. Si el repositorio es público, registrar el incidente y notificar al responsable sin incluir el secreto.
