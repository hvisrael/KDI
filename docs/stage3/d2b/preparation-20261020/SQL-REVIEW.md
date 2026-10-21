# Preparación SQL D2-B — 2026-10-20

TaskInput: D2B-PREP-20261020-SQL, attempt 1. Agente `/root/d2b_sql_prepare`, perfil efectivo `engineer`; implementación y preparación, no QA ni revisión independiente. Autorización: solicitud humana de continuar D2-B y `docs/stage3/d2b/AUTHORIZATION.md`. Presupuesto asignado: 15 minutos. Base aceptada D2-A: `c31fe8c14872ff88b6d0d3882f4585b36e6a3798788108ecdd8539841a312973`.

## Inventario revisado

| Migración aditiva | Tablas nuevas | Integridad principal |
|---|---|---|
| identity / 20261019040513_Accessidentity | Users, PermissionCatalog, Grants, Sessions, AuditEntries | PK en cada tabla; Users issuer/subject BIN2 y longitudes computadas con índice único; Grants FK a Users/PermissionCatalog y unicidad filtrada activa; Sessions FK a Users, handle SHA-256 único y CHECK temporal; rowversion en Users/Grants/Sessions |
| organizations / 20261019040514_Accessorganizations | Organizations, OrganizationMemberships, AuditEntries | PK en cada tabla; código ASCII BIN2 único con CHECK; membresía única organización/usuario y FK local a Organizations; rowversion en organizaciones/membresías |

Los historiales por módulo conservan `20261018000100_InitialIdentity` y `20261018000100_InitialOrganizations`. Son ocho tablas nuevas, además de los historiales existentes. No cambios en Portfolio ni esquemas compartidos. No hay FK entre módulos: UserId de membresía y OrganizationId de Grants se validan en aplicación/administración; SQL no impide por sí solo referencias cruzadas huérfanas. Requiere pruebas conectadas de invariantes y control de acceso.

Los roles son cinco conjuntos fijos de permisos implementados por `AccessAdministration`: Administrator, Manager, Reader, Auditor y RestrictedReader. No existe tabla Roles ni se propone añadirla sin decisión. PermissionCatalog inserta ocho códigos: access.manage, audit.read, clients.manage/read, members.manage/read, projects.manage/read. RestrictedReader materializa grants de projects.read limitados a proyecto.

## Paquete pendiente de autorización

Ruta del candidato: `tools/Kulani.KDI.Migrations/prepared-sql/`. `manifest.json` contiene tamaño y SHA-256 de los 14 artefactos SQL/hashes preparados. Para cada Test y Dev:

- `*-identity.sql`, `*-organizations.sql`: upgrade EF idempotente, guardas de servidor exacto DESKTOP-F25O3MF, base exacta, esquema existente, baseline existente, historia conocida y EF 10.0.13. No crea usuarios de negocio.
- `*.migration.sha256`: hash del cuerpo EF que compara la vía existente `--apply`; no equivale al hash del archivo SQL completo. Los hashes completos están en manifest.json.
- `*.rollback.sql`: reversión D2-B a baseline, con target exacto, historia completa conocida y aviso destructivo. No es idempotente: después de revertir, la guarda rechaza una segunda ejecución. No borra esquemas ni baseline.
- `*-runtime-grants.sql`: copia de la propuesta anterior, ahora con base exacta por archivo, `:ON ERROR EXIT` y `SET XACT_ABORT ON`. Solo permisos por objeto para el rol existente kdi_application. No se ejecutó ningún GRANT.

La propuesta concede Users SELECT/INSERT, PermissionCatalog SELECT, Grants SELECT, Sessions SELECT/INSERT/UPDATE/DELETE, identity.AuditEntries INSERT, Organizations SELECT y OrganizationMemberships SELECT. No concede acceso a organizations.AuditEntries, DDL, permisos de esquema, nuevos logins ni roles globales. Debe comprobarse la necesidad de DELETE en Sessions y el permiso efectivo con el runtime real antes de aprobar el mínimo definitivo. La repetición de GRANT de la misma autorización es idempotente, pero no demuestra permisos efectivos ni elimina derechos previos.

## Reversión y orden operacional propuesto

Preferir desactivar Access y preservar datos mientras se prepara corrección hacia delante. Un downgrade físico elimina usuarios, grants, sesiones, membresías, organizaciones y auditoría D2-B; exige aprobación separada, respaldo verificable y ventana sin procesos escribiendo. El SQL reverso solo vuelve a las migraciones iniciales aceptadas, nunca a cero. Cada módulo usa su transacción EF; el paquete completo no es una transacción entre módulos.

Antes de cualquier ejecución: congelar/revisar hashes; autorizar específicamente Test, operaciones y operador; verificar baseline y respaldos; revisar ambos upgrades y GRANT. Ejecutar Test antes que Dev. Hacer pruebas autorizadas de upgrade, repetición, permisos y fixtures. Dev requiere autorización posterior. Guardar evidencias sin credenciales, tickets ni cookies.

La reversión de permisos requiere capturar el estado previo de permisos explícitos y efectivos antes del GRANT, y retirar únicamente el delta aprobado; no se incluye un REVOKE ciego que pudiera retirar permisos aceptados de D2-A. No modificar permisos globales. La autorización actual permite preparar estos archivos, no ejecutarlos.

## TaskResult y evidencia local

Cambio de código limitado a `tools/Kulani.KDI.Migrations/Program.cs`: generación adicional offline de rollback hasta baseline con guardas. No se cambió el modelo, contratos, migraciones históricas, runtime ni la vía de ejecución `--apply`. No se scaffoldó ninguna migración.

Verificaciones ejecutadas desde `.local/d2b-candidate`:

1. `dotnet build tools/Kulani.KDI.Migrations/Kulani.KDI.Migrations.csproj -c Release --no-restore`: exit 0, 0 advertencias, 0 errores (67.31 s).
2. Ejecución del DLL Release con `KulaniKDI_Test` y `KulaniKDI_Dev` a prepared-sql y repetición a repro-sql: cuatro comandos exit 0, ocho mensajes explícitos de generación sin conexión SQL.
3. Comparación SHA-256 de 12 artefactos migración/reversión/hash entre ambas generaciones: idénticos.
4. Inspección estática de las cuatro reversiones: DROP solo sobre tablas nuevas D2-B; eliminación únicamente del registro Access correspondiente; baseline, esquemas e historiales conservados. Manifest SHA-256 completo generado localmente.
5. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Kulani.KDI.Migrations/Verify-PreparedSql.ps1`: exit 0; verifica 14 hashes del manifiesto, 12 hashes de reproducción y cuatro guardas/alcances de rollback. Se usa política solo del proceso: la invocación directa inicial quedó bloqueada por firma de scripts; no se cambió configuración global. Primera ejecución del verificador detectó una diferencia de enumeración JSON de PowerShell 5; se corrigió el verificador y se repitió satisfactoriamente.

La consulta opcional `git status` no pudo ejecutarse porque el workspace no es repositorio Git; no afecta generación ni pruebas offline. No se usa Git como evidencia de integridad. La segunda generación permanece en `repro-sql/` como evidencia de reproducibilidad.

Omisiones y bloqueos: no conexiones SQL, DDL/DML, fixtures, GRANT, aplicación/repetición real, ejecución de reversión ni recuperación de respaldo. No se verificaron catálogo real, permisos efectivos, planes de ejecución, unicidad SQL real, rowversion o carreras concurrentes. Estas pruebas están bloqueadas por autorización específica de operaciones persistentes y acceso operacional aprobado. La revisión aquí es preparación técnica del implementador; no satisface QA ni code-review independientes. No se declara PASS integral ni cierre D2-B.

Siguiente acción: revisión independiente del candidato y de estos hashes; autorización humana concreta del paquete Test y fixtures antes de probar la persistencia real. Infraestructura Auth0/HTTPS se prepara por la coordinación y queda fuera de esta tarea SQL.
