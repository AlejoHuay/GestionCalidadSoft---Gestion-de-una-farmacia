# Validación de secrets:S6703

Estado: corrección local implementada; issue pendiente de cierre.

## Verificado el 18 de septiembre de 2026

- Se retiró la conexión expuesta de `appsettings.json`. La conexión en `appsettings.Development.json` también está vacía.
- Se conserva el mecanismo existente de configuración y la validación de conexión requerida.
- `.env` no existe en este checkout y no está rastreado. `.env` y `.env.local` coinciden con las reglas de exclusión; el ejemplo de la raíz está versionado con valores vacíos.
- Se revisaron los archivos rastreados buscando la contraseña expuesta: se encontró en `appsettings.json` antes de corregirlo. El historial de las referencias locales contiene 27 commits con ese valor. No se reescribió el historial.
- `dotnet build --verbosity quiet`: compilación correcta, 0 advertencias y 0 errores. El primer intento restringido no pudo acceder a NuGet; la compilación con acceso autorizado completó la restauración.
- `dotnet run --no-build --launch-profile http`, sin conexión configurada: termina con código 1 y el mensaje `Configura ConnectionStrings__PostgresConnection en el entorno o en .env.`. No se utiliza una conexión predeterminada ni se imprime una contraseña.
- SonarQube local responde con estado UP. No hay `SONAR_TOKEN` en el entorno del proceso ni SonarScanner registrado como herramienta global. No se ejecutó un nuevo análisis.

## Pendiente: no afirmar que el issue está resuelto

- El usuario confirmó que la contraseña todavía no fue rotada. El administrador debe rotarla, actualizar consumidores y verificar que la anterior ya no permite conexión.
- Configurar la conexión nueva y las variables JWT en el entorno local o almacén seguro.
- Verificar arranque con configuración válida, lectura y CRUD con datos de prueba autorizados.
- Ejecutar SonarQube contra la clave correcta del proyecto y verificar que S6703 ya no está abierto, sin nuevas credenciales detectadas.
- Adjuntar confirmación de rotación y evidencias funcionales y de SonarQube sin valores sensibles.

La compilación y la eliminación del secreto del archivo no prueban su revocación. Este registro no certifica la ausencia de cualquier otro secreto en todo el repositorio.
