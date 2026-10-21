# ADR-002 — Modular Monolith y Clean Architecture pragmática

Fecha: 2026-10-17. Estado: Propuesto.

Contexto: trece áreas funcionales, crecimiento gradual y equipo inicial pequeño. Se solicitan seis proyectos técnicos base.

Propuesta: un backend desplegable; capas Domain/Application/Contracts/Infrastructure/Api y Web independiente. Módulos internos por carpeta y propiedad de datos/contratos; dependencias según MODULES.md. Contracts sin entidades EF. Pruebas de arquitectura y revisión protegen límites; carpeta por sí sola no aísla ensamblados.

Alternativas: microservicios iniciales multiplican operación y consistencia distribuida sin carga demostrada; proyecto por cada capa y módulo endurece límites pero aumenta estructura antes de aprender; monolito sin límites dificulta evolución. Extraer ensamblados o servicios cuando existan necesidades medibles, mediante otro ADR.

Consecuencias: menor costo operativo inicial y transacciones locales; disciplina requerida para evitar acoplamiento y ciclos. Evitar SharedKernel de negocio masivo y llamadas al DbContext ajeno. Infraestructura de agentes queda fuera del producto.

Validación: matriz de dependencias, análisis de referencias y pruebas que rechacen dependencias prohibidas. Ratificar límites al detallar invariantes; todavía no hay implementación que verificar.
