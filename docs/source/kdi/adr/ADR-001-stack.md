# ADR-001 — Stack base

Fecha: 2026-10-17. Estado: Propuesto. Responsable de aceptación: Human Orchestrator.

Contexto: producto empresarial con dominio transaccional, API y experiencia web; dirección tecnológica solicitada por el usuario.

Propuesta: .NET 10, C#, ASP.NET Core Web API, SQL Server, EF Core 10 con proveedor compatible; React, TypeScript y Vite. Dapper únicamente para lecturas justificadas por complejidad o medición, mediante ADR adicional y SQL parametrizado, respetando alcance y propiedad de datos.

Alternativas: EF Core para todas las consultas inicialmente (opción preferida); Dapper generalizado añade mantenimiento y duplicación; otro runtime/framework exigiría cambiar la dirección solicitada sin evidencia actual que lo justifique.

Consecuencias: stack uniforme y tipado; requiere fijar versiones compatibles, runtime soportado y tooling. EF Core 10 requiere .NET 10 según [Microsoft](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew). Versión/edición de SQL Server, SDK exacto, Node, React, TypeScript y Vite pendientes de matriz al fundar. Sin instalaciones ahora.

Validación futura: build reproducible con versiones fijadas; persistencia SQL real; build web y contrato API. Aceptación pendiente; no se ha probado compatibilidad local del stack.
