# 🏥 Gestor de Farmacia - Razor Pages

La versión actual utiliza **PostgreSQL / Supabase con Npgsql**. Consulta [configuración, migración desde MySQL y pruebas](database/postgresql/LEEME.md).

```powershell
dotnet run --launch-profile http
```

Configura `ConnectionStrings__PostgresConnection` y las variables JWT en `.env` antes de arrancar. Los documentos y scripts MySQL antiguos se conservan como referencia del origen.

## 📌 Descripción del Proyecto

Sistema web desarrollado con **ASP.NET Core Razor Pages** y **C#** que permite gestionar la información de una farmacia mediante operaciones CRUD.

El sistema permite administrar:

- Medicamentos  
- Clientes  
- Bioquímicos  

A diferencia de una arquitectura en capas tradicional, este proyecto está estructurado de forma **modular y práctica**, combinando **Pages, Services, Helpers y Factories**, manteniendo buenas prácticas como **SOLID y Clean Code**.

---

## 🚀 Tecnologías Utilizadas

- ASP.NET Core Razor Pages  
- C#  
- MySQL  
- MySql.Data  
- HTML / CSS / Bootstrap  
- Git / GitHub  

---

## 🗄️ Base de Datos

El sistema utiliza **MySQL** con las siguientes entidades principales:

- Cliente  
- Medicamento  
- Bioquímico  

Cada entidad cuenta con operaciones CRUD completas.

---

## 🧱 Enfoque de Arquitectura

El proyecto **NO utiliza una arquitectura en capas estricta**, sino una estructura simplificada enfocada en:

- Separación lógica por responsabilidades  
- Uso de servicios para lógica de negocio  
- Uso de helpers para reutilización de código  
- Uso de factories para creación de objetos  

Esto permite mantener el sistema:

✔ Fácil de entender  
✔ Rápido de desarrollar  
✔ Mantenible a nivel académico  

---

## 🧩 Patrón de Diseño Aplicado

### Factory Method (adaptado)

Se implementa el patrón Factory Method utilizando:

- **FactoryCreator** → decide qué tipo de objeto crear  
- **FactoryProducts** → clases concretas de medicamentos  

### Tipos de medicamentos implementados:

- Antibiótico  
- Analgésico  
- Antiinflamatorio  
- Antialérgico  
- Antipirético  
- Vitaminas  
- Antiséptico  

🔹 Esto permite crear objetos según su tipo sin acoplar la lógica directamente en las páginas.

---

## 🧠 Organización del Proyecto

```
ProyectoArqSoft
│
├── Pages
│   ├── Cliente
│   ├── Medicamento
│   └── Bioquimico
│
├── Services
│   └── Lógica de negocio
│
├── Factories
│   ├── FactoryCreator
│   └── FactoryProducts
│
├── Helpers
│   └── Funciones reutilizables
│
├── Models / Entities
│
├── Repositories (opcional según uso)
│
├── Base
└── Program.cs
```

---

## ⚙️ Componentes Clave

### 🔹 Pages (Razor Pages)
Gestionan la interacción con el usuario (UI + lógica básica de flujo).

### 🔹 Services
Contienen la lógica de negocio del sistema.

### 🔹 Factories
Encargadas de la creación de objetos de tipo **Medicamento** según su clasificación.

### 🔹 Helpers
Funciones auxiliares reutilizables para validaciones, conversiones o utilidades.

---

## 🧱 Principios SOLID Aplicados

### S — Single Responsibility Principle
Cada componente tiene una responsabilidad clara:

- Pages → UI  
- Services → lógica  
- Factories → creación de objetos  
- Helpers → utilidades  

---

### O — Open / Closed Principle
Se pueden agregar nuevos tipos de medicamentos sin modificar código existente.

---

### L — Liskov Substitution Principle
Los distintos tipos de medicamentos pueden usarse como la clase base sin afectar el sistema.

---

### I — Interface Segregation Principle
Se evita el uso de interfaces innecesarias, manteniendo el código simple.

---

### D — Dependency Inversion Principle
Se reduce el acoplamiento usando servicios y abstracciones cuando es necesario.

---

## 📊 Funcionalidades

✔ Registro de medicamentos  
✔ Edición de medicamentos  
✔ Eliminación lógica  
✔ Gestión de clientes  
✔ Gestión de bioquímicos  
✔ Búsqueda y filtrado  
✔ Validaciones de datos  

---

## ⚙️ Cómo ejecutar el proyecto

1. Clonar el repositorio

```bash
git clone https://github.com/RashLop/ArquitecturaSoft---Gestion-de-una-farmacia
```

2. Abrir en Visual Studio  

3. Configurar la cadena de conexión en:

```json
appsettings.json
```

4. Ejecutar el proyecto  

---

## 📌 Notas

- Proyecto orientado a fines académicos  
- Se prioriza claridad y comprensión sobre complejidad arquitectónica  
- Se implementan patrones de diseño de forma práctica  

*Proyecto académico para gestión de farmacia VITALCARE*
