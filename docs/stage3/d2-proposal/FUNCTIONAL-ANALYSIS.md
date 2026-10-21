# D2 — Análisis funcional para decisión humana

Estado: PROPUESTA; autorización actual exclusivamente documental. D2 no iniciado. Perfil responsable: analyst-architect; agente `/root/d2_functional_proposal`. Ninguna regla propuesta equivale a aprobación humana.

## Fuentes y trazabilidad

Fuentes locales: `docs/source/kdi/REQUIREMENTS.md`, `MODULES.md`, `ARCHITECTURE.md`, ADR-003 y ADR-004, `docs/stage3/DECISIONS.md` y `docs/stage3/d1/acceptance/`. D1 aprobado se identifica por manifiesto SHA-256 `e2bb22b739b5924e8c2ead3af421e56ac97dc6e6b0209a7d0b7b5da8d1f51193`; esta propuesta no lo modifica ni reabre. Los documentos fuente describen varias decisiones como propuestas. Aquí se concretan para ratificación, sin cambiar su estado.

## Reglas propuestas y preguntas de aprobación

| ID | Propuesta concreta | Alternativa o pregunta humana |
|---|---|---|
| F01 | Un usuario puede pertenecer a varias organizaciones; selecciona una activa entre sus membresías vigentes. Cada petición se autoriza para esa organización. | ¿Piloto multi-organización desde D2 o usuario limitado a una organización? Recomendada la primera para verificar aislamiento real. |
| F02 | Organizations posee membresía empresarial Active/Suspended. Identity posee identidad y concesiones. Se necesitan ambas vigentes para acceder. Suspensión bloquea peticiones nuevas y no elimina historial. | Ratificar responsable empresarial de altas, suspensiones y bajas. No hay alta pública automática. |
| F03 | Cliente y proyecto pertenecen exactamente a una organización; un cliente no se comparte entre organizaciones. Proyecto requiere cliente activo de la misma organización. | ¿Admitir clientes compartidos? Excluir del piloto. |
| F04 | Todos los proyectos, incluidos internos, requieren cliente. Un proyecto interno usa un cliente interno explícito de su organización, con código único y sujeto a las mismas reglas. No se crea automáticamente un cliente ficticio. | Alternativa: proyecto con cliente opcional y tipo Internal. Esta cambia reglas/documentos y requiere aprobación explícita. ¿Hay necesidad real de proyectos internos? |
| F05 | Código normalizado sin distinguir mayúsculas, máximo 32 caracteres; nombre recortado obligatorio máximo 200. Código único por organización y tipo de entidad, incluso archivados. | Ratificar normalización Unicode, caracteres permitidos y si se permite reutilizar código archivado. Propuesta: Unicode NFC y código alfanumérico ASCII más guion/guion bajo. |
| F06 | Active/Archived; sin borrado físico, reactivación, traslado de organización ni cambio de cliente de un proyecto en D2. Archivar cliente se rechaza si existen proyectos activos. Crear proyecto y archivar cliente compiten de forma atómica. | ¿Se necesita reactivación o transferencia en D2? Si sí, ampliar criterios y presupuesto. |
| F07 | Cada mutación exige permiso; edición/archivo exige versión vigente. Conflicto preserva los datos existentes y permite recargar. No hay sobrescritura automática. | Ratificar UX de resolución manual; fusión de cambios excluida. |
| F08 | Auditoría registra actor estable, organización, acción, recurso, fecha UTC y correlación, en la misma transacción. No guarda tokens, contraseñas ni cuerpos completos. | Aprobar retención, acceso y campos redactados antes de operación fuera de pruebas. |
| F09 | Listados con paginación, orden estable por código e ID, filtros por estado y búsqueda código/nombre. Conteos limitados al alcance autorizado. | Aprobar límites propuestos: 25 por defecto y máximo 100; sin SLA inventado. |
| F10 | Sin administrador global implícito. Primer administrador se provisiona mediante procedimiento explícito autorizado, con auditoría. No se concede acceso por dominio de correo. | Identificar responsable y procedimiento bootstrap; impedir eliminar/suspender la última administración efectiva sin reemplazo. |

## Matriz propuesta rol × acción × alcance

Los roles son plantillas de permisos locales, no nombres confiados directamente al IdP. Permisos explícitos `clients.read/manage`, `projects.read/manage`, `members.read/manage`, `access.manage`, `audit.read`. Un rol no concede facultades fuera de su organización.

| Rol propuesto | Clientes | Proyectos | Miembros/concesiones | Auditoría | Alcance |
|---|---|---|---|---|---|
| Administrador de organización | Leer/crear/editar/archivar | Leer/crear/editar/archivar | Leer/gestionar ambos | Leer | Organización asignada; no otras organizaciones |
| Gestor | Leer/crear/editar/archivar | Leer/crear/editar/archivar | Ninguno | Ninguno | Organización asignada |
| Lector | Leer | Leer | Ninguno | Ninguno | Organización asignada |
| Auditor | Ninguno salvo permiso adicional | Ninguno salvo permiso adicional | Ninguno | Leer | Organización asignada |
| Miembro restringido a proyectos | Ningún listado/detalle de clientes | Leer proyectos asignados | Ninguno | Ninguno | Lista explícita de ProjectId dentro de su organización |

Para el miembro restringido, el DTO de proyecto incluye solamente identificador de cliente, no sus datos; editar/crear/archivar proyectos queda excluido de ese rol mínimo. Alternativa: posponer alcances de proyecto y permitir únicamente roles de organización; requiere eliminar expresamente del alcance sus pruebas/criterios, no simular protección. Propuesta recomendada: incluir lector restringido para demostrar acceso horizontal. No hay comodines de proyecto ni autoescalamiento; toda concesión verifica membresía y alcance.

## D2-A — SQL Server y base aislada

Alcance funcional: persistencia técnica verificable para módulos iniciales, sin publicar casos de negocio ni autenticación provisional. Exclusiones: producción, importación de datos, IdP, UI de negocio, datos de IDI y CI D3.

Aprobaciones: instancia/versión/edición/compatibility level, base aislada y dueño, esquema/DbContext/historial por módulo, autorización de migración y limpieza. Dependencias: recursos SQL realmente disponibles y permisos diferenciados; riesgos de privilegio excesivo, limpieza fuera del destino y diferencias de collation. Bloqueo: ausencia de destino aislado autorizado, permisos, mecanismo seguro de conexión o estrategia aceptada.

Criterios A1–A4: migrar base vacía al estado esperado; segunda aplicación no cambia esquema; aplicación funciona con cuenta sin DDL; restricciones y separación modular se verifican en SQL Server real. Repetir creación de base efímera únicamente si fue autorizado. Unitarias: configuración/políticas relevantes; integración: migraciones, restricciones y rollback; API: arranque y salud sin filtrar conexión; E2E negocio N/A, porque aún no existen casos publicados. D1 smoke sigue siendo regresión, no sustituto de SQL.

Infra/permisos: servidor/DB aislada nombrados, cuenta migradora DDL acotada, cuenta aplicación DML por esquemas, responsables de provisión/limpieza y canal seguro de secretos. Presupuesto consolidado propuesto: 16–27 horas-agente acumuladas, según `PROPOSAL.md`, para ejecución completa con QA/review. La hipótesis funcional preliminar de 80–140 minutos no constituye presupuesto de ejecución. Se contemplan fases de diseño, implementación/verificación y revisión independiente; un ciclo de corrección incluido. Esperas humanas/provisión excluidas; nuevas decisiones requieren reestimar. No autoriza ejecución.

## D2-B — Identity & Access

Alcance: login/logout OIDC, selección de organización, membresías y concesiones mínimas, control de acceso servidor y navegación acorde; bootstrap controlado. Exclusiones: contraseñas locales, registro público, federación entre múltiples IdP, facturación, sincronización masiva de directorio y administración global de plataforma.

Aprobaciones: proveedor/tenant de pruebas; flujo BFF o SPA; F01/F02/F10 y matriz anterior; creación de organizaciones por procedimiento controlado, no autoservicio; administración mínima por API y UI de membresías/concesiones o procedimiento administrado documentado; duración/revocación de sesiones. Dependencias: D2-A y tenant/cuentas/client OIDC reales; riesgos: autoescalamiento, sujeto confundido por correo, permisos obsoletos y tenant incorrecto. Bloqueo: IdP/cuentas/redirects ausentes, flujo sin decidir o política de revocación sin tiempo verificable.

Criterios B1–B7: identidad por `(issuer, subject)`; login/logout real; usuario sin membresía no ve organización; usuario multi-org cambia contexto sin fuga; lector no escribe; administrador A no modifica B; suspensión/concesión retirada deniega la siguiente petición protegida según política propuesta de comprobación local por petición. Expiración/revocación IdP requieren límite humano concreto y prueba, no promesa de revocación instantánea.

Unitarias: tabla de permisos/alcances y estados de membresía; integración: persistencia de concesiones y suspensión; API: 401/403, manipulación OrgId/ProjectId, escalamiento y revocación; E2E: IdP de pruebas real, cambio organización, acceso denegado y logout. Mocks permiten aislamiento, nunca acreditan integración OIDC real.

Infra/permisos: propietario IdP, tenant aislado, registro de cliente, redirects HTTPS, cuentas de cada rol, secretos fuera del repositorio y permiso para administrarlos en el almacén acordado. Presupuesto consolidado propuesto: 30–51 horas-agente acumuladas, según `PROPOSAL.md`, para ejecución completa con QA/review. La hipótesis funcional preliminar de 140–240 minutos no constituye presupuesto de ejecución. Las rondas son fases de trabajo, no compromisos de rondas de corrección; un ciclo de corrección incluido. Esperas/provisión excluidas; nuevas decisiones o UI administrativa ampliada requieren reestimar.

## D2-C — Clientes/Proyectos, auditoría y concurrencia

Alcance: CRUD sin borrado físico, archivo, listados y UI de clientes/proyectos; cliente interno explícito si se ratifica; auditoría consultable por auditor; restricciones, versiones y coordinación cliente/proyecto. Exclusiones: portafolios, tareas, requisitos, horas, releases, reportes analíticos, importación/exportación y workflows adicionales.

Aprobaciones: F03–F09, campos y validaciones, matriz, estrategia transaccional y tratamiento de errores; no se asume cliente opcional. Dependencias: A y B aceptados; riesgos: carrera alta/archivo, duplicados concurrentes, fuga de conteos, exposición auditada de datos sensibles. Bloqueo: regla interna/cliente sin resolver, transacción sin prueba SQL, auditoría/retención sin decisión aplicable o aislamiento no demostrado.

Criterios C1–C9: alta/consulta válidas; entradas inválidas no escriben; duplicado concurrente produce una sola entidad; cliente ajeno/archivado no admite proyecto; archivo con proyecto activo falla; en carrera alta/archivo nunca queda proyecto activo con cliente archivado; actualización con versión vieja rechaza sin sobrescribir; auditoría y mutación se confirman o revierten juntas; listados y consulta por ID no revelan recursos ajenos. UI ofrece carga/vacío/error, teclado, confirmación de archivo y recuperación de conflicto sin perder entrada.

Unitarias: validaciones/estados y autorización; integración SQL: unicidad, rowversion, rollback auditoría y carrera repetible sincronizada; API: validación, ETag/conflicto, paginación, acceso horizontal/vertical; E2E: gestor crea cliente/proyecto, lector no modifica, proyecto interno usa cliente explícito, dos sesiones generan conflicto y archivo bloqueado, auditor consulta solo su organización. Infra/permisos: base aislada, IdP de B, usuarios y datos de dos organizaciones, capacidad de dos conexiones SQL concurrentes y dos contextos browser. Presupuesto consolidado propuesto: 42–70 horas-agente acumuladas, según `PROPOSAL.md`, para ejecución completa con QA/review. La hipótesis funcional preliminar de 180–300 minutos no constituye presupuesto de ejecución. Las rondas representan fases de trabajo; un ciclo de corrección incluido. Esperas excluidas; nuevas decisiones requieren reestimar.

## Escenarios transversales mínimos

| Escenario | Resultado esperado |
|---|---|
| Usuario miembro de A y B con rol diferente en cada una | El cambio de contexto cambia permisos y datos; no acumula privilegios entre organizaciones. |
| Cliente A con ID conocido utilizado desde B | No aparece ni puede vincularse; error no revela existencia ni contenido. |
| Proyecto interno aprobado | Se crea bajo cliente interno explícito de la misma organización; sin excepciones de autorización/auditoría. |
| Administrador intenta concederse alcance en otra organización | Denegación y ausencia de cambios. |
| Suspensión durante sesión activa | Próxima petición protegida denegada según política aprobada; UI vuelve a estado permitido. |
| Dos gestores crean código equivalente o editan misma versión | Una creación; segunda edición rechazada; sin auditoría de éxito para cambios no confirmados. |
| Fallo de escritura de auditoría | Rollback de mutación y respuesta genérica correlacionada. |

## Puerta de aceptación y prioridad

Recomendación: autorizar D2-A primero, después B y finalmente C, cada uno con TaskInput separado y recursos/permisos/presupuesto explícitos. Presupuesto consolidado de ejecución propuesto en `PROPOSAL.md`: 88–148 horas-agente acumuladas (5.280–8.880 minutos), A 16–27, B 30–51 y C 42–70 horas; no equivale a tiempo de pared. La hipótesis funcional preliminar de 400–680 minutos solo cubría una estimación inicial acotada y no es presupuesto de ejecución ni debe sumarse al consolidado. El presupuesto incluye QA/review y un ciclo de corrección; fases de trabajo no equivalen a rondas repetidas de corrección. Nuevas decisiones o cambios de alcance requieren reestimar. Máximo dos especialistas simultáneos y QA/review sin modificar candidato. Evidencia obligatoria por bloque: comandos/resultados reales, pruebas pendientes/bloqueadas, manifiesto/hash congelado y revisión independiente. Prueba obligatoria bloqueada impide PASS; la autoridad final permanece en Human Orchestrator.
