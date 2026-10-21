# Estructura técnica propuesta

Destino futuro `E:\INSSOFT\KDI`; solución `Kulani.KDI.slnx`. Árbol declarativo, no creado en esta etapa.

```text
KDI/
├── Kulani.KDI.slnx
├── src/
│   ├── Kulani.KDI.Domain/           Modules/<Module>/
│   ├── Kulani.KDI.Application/      Modules/<Module>/
│   ├── Kulani.KDI.Contracts/        Modules/<Module>/V1/
│   ├── Kulani.KDI.Infrastructure/   Modules/<Module>/Persistence/
│   ├── Kulani.KDI.Api/              Endpoints/<Module>/
│   └── Kulani.KDI.Web/              src/features/<module>/
├── tests/
│   ├── Kulani.KDI.UnitTests/
│   ├── Kulani.KDI.IntegrationTests/
│   ├── Kulani.KDI.ApiTests/
│   ├── Kulani.KDI.ArchitectureTests/
│   └── Kulani.KDI.E2E/              proyecto de pruebas web
├── docs/
│   ├── product/
│   ├── requirements/
│   ├── architecture/adr/
│   ├── data/
│   ├── api/
│   ├── testing/
│   └── releases/
└── ci/                             proveedor pendiente
```

`Kulani.KDI.Web` es el frontend React/TypeScript/Vite, no una biblioteca C#. Evaluar inclusión como carpeta de solución o proyecto JS compatible con el IDE; no crear `.csproj` vacío para forzarla. Build web y backend se coordinan en CI. Elegir versiones exactas, gestor de paquetes, lockfile y herramientas de pruebas durante fundación; no están instalados por esta propuesta.

Referencias permitidas: Domain ninguna infraestructura; Contracts DTO sin dependencias de capas; Application→Domain/Contracts; Infrastructure→Application/Domain/Contracts cuando implementa adaptadores; Api→Application/Contracts e Infrastructure para composición. Web→API HTTP. Tests referencian solo lo necesario; arquitectura comprueba límites internos por módulos además del grafo de proyectos.

Propuesta: .NET/EF Core/provider SQL Server en major 10 compatibles, versiones exactas fijadas al fundar. EF Core 10 requiere .NET 10, según [Microsoft](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew). SQL Server versión y compatibility level deben decidirse antes de migraciones; no asumir características de SQL Server 2025.
