# Analisis local con SonarQube

1. Inicia SonarQube en http://localhost:9000 y abre una terminal CMD en la raiz del proyecto.

2. Instala SonarScanner una sola vez (si ya lo instalaste, omite este paso):

```bat
dotnet tool install --global dotnet-sonarscanner
```

3. Para cada analisis, reemplaza TU_TOKEN por tu token vigente y ejecuta estos comandos uno por uno. Continua solo si el anterior termina correctamente:

```bat
dotnet sonarscanner begin /k:"VitalCare-farmacia-SIS312-Sept" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="TU_TOKEN"

dotnet build

dotnet sonarscanner end /d:sonar.token="TU_TOKEN"
```

No guardes el token real en este documento. Estos comandos no requieren cargar SonarQube.Analysis.xml.

4. Cuando SonarQube termine de procesar el informe, consulta el [dashboard del proyecto](http://localhost:9000/dashboard?id=VitalCare-farmacia-SIS312-Sept).
