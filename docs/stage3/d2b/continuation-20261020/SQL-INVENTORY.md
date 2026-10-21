# D2-B: inventario y operaciones pendientes

Preparación del 2026-10-20. No se abrió conexión SQL ni se ejecutaron DDL, DML, GRANT, fixtures o downgrade en esta continuación.

| Módulo / migración | Tablas añadidas |
|---|---|
| identity / 20261019040513_Accessidentity | Users, PermissionCatalog, Grants, Sessions, AuditEntries |
| organizations / 20261019040514_Accessorganizations | Organizations, OrganizationMemberships, AuditEntries |

Roles Administrator, Manager, Reader, Auditor y RestrictedReader son conjuntos fijos de permisos en aplicación; no existe tabla Roles. PermissionCatalog contiene ocho permisos. Las membresías enlazan usuarios por identificador validado en aplicación, sin FK entre módulos. Los historiales y migraciones iniciales de D2-A se preservan; Portfolio no cambia.

Paquete revisable: `.local/d2b-candidate/tools/Kulani.KDI.Migrations/prepared-sql/manifest.json`. Contiene 14 entradas: por cada destino Test/Dev, dos upgrades, dos hashes del cuerpo EF, dos rollback y una propuesta de permisos. Los hashes del cuerpo EF no sustituyen los hashes de archivo completo del manifiesto.

Se regeneraron los doce artefactos EF en `sql-reproduced/` usando el DLL Release compilado en esta continuación. Generación Test y Dev: exit 0. Comparación con paquete preparado: exit 0; catorce hashes de paquete, doce reproducciones y cuatro alcances de rollback comprobados en `sql-reproduction.log`. Esta comprobación es offline y no demuestra idempotencia operacional.

Los upgrades tienen guardas de servidor/base/baseline/historia y cuerpo EF idempotente. Los rollback vuelven al baseline y eliminan datos D2-B; no son idempotentes y requieren respaldo, ausencia de escritores y autorización separada. No hay transacción conjunta entre módulos. Para revertir permisos se debe capturar el estado previo y retirar únicamente el delta aprobado; no hay REVOKE ciego preparado. Preferir deshabilitar acceso y conservar datos durante una corrección hacia delante.

La revisión independiente R1 detectó DELETE innecesario sobre Sessions para runtime. Se retiró en las tres propuestas del candidato; SELECT/INSERT/UPDATE permanecen propuestos. El borrado de mantenimiento corresponde al operador privilegiado. Se actualizaron hashes y se repitió el verificador: `sql-final.log`. Ningún permiso fue aplicado.

Antes de ejecutar se necesita autorización concreta del paquete Test, operador, migraciones y fixtures/limpieza; los permisos requieren una decisión separada. La solicitud vigente prohíbe nuevos GRANT. Dev queda para autorización posterior. No usar `--apply`, pruebas SQL ni scripts por el solo hecho de disponer de hashes.

Revisión detallada de claves, índices, restricciones y permisos propuestos: [SQL-REVIEW.md](../preparation-20261020/SQL-REVIEW.md). La prueba conectada debe verificar upgrade, repetición, permisos efectivos, concurrencia, auditoría atómica y recuperación/reversión autorizada.
