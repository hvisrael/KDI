# Matriz de autorizaciones — Test exclusivamente

Estado: propuestas sin aprobación de ejecución. Fuente: solicitud humana de preparar ejecución controlada D2-B. Fecha 2026-10-20. Destino único DESKTOP-F25O3MF / instancia MSSQLSERVER / KulaniKDI_Test. Dev, producción, IDI, roles globales y otros proyectos excluidos.

| ID | Operación | Clasificación | Autorización humana necesaria |
|---|---|---|---|
| A0 | Leer archivos, hashes, manifiestos, verificar SQL offline, redactar plan | Segura sin modificar datos SQL | Ya autorizada; no conexión |
| A1 | Preflight conectado SELECT de destino, versión, baseline, catálogo, usuarios/roles/permisos y capacidad backup | Lectura sin DML; conexión restringida | Autorizar explícitamente conexión read-only Test, operador existente y consultas de metadatos; no implica A2–A8 |
| A2 | Backup COPY_ONLY/CHECKSUM a archivo nuevo y VERIFYONLY | Requiere autorización de infraestructura/operación backup | Operador con permisos existentes, ruta privada exacta, ventana, espacio, retención; escribe backup y metadatos de backup |
| A3 | Dos upgrades Test y repetición de ambos | Requiere autorización de migración | Aprobar los dos hashes de archivo completos del paquete; identity y organizations solamente; seed PermissionCatalog e historial incluidos; sin GRANT ni fixtures |
| A4 | Siete GRANT por objeto al rol existente kdi_application | Requiere autorización de permisos | Aprobar hash Test runtime-grants, permisos exactos de tabla debajo y snapshot previo; sin nuevos logins/roles/membresías, sin GRANT por esquema |
| A5 | Fixtures, bootstrap, membresías, roles, sesiones, revocación y limpieza acotada | Requiere autorización de migración/datos de prueba | Aprobar casos exactos, GUID de corrida, tablas, identidad Windows existente, DML y recuperación de residuos; no comprende downgrade ni purge global |
| A6 | Certificado/confianza, keyring/ACL, escucha HTTPS, Auth0/aplicación/cuentas y arranque piloto | Requiere autorización de infraestructura | Aprobar acciones concretas, operador, directorios, certificado, tenant/app y seis cuentas sintéticas. Sin comodines ni opciones pagadas; no contraseñas en conversación |
| A7 | Restauración de ensayo o Test; downgrade físico | Requiere autorización de infraestructura y migración destructiva separada | Destino/rutas/backup/ventana exactos; aceptación de pérdida de datos. NO autorizado por A2/A3. Restaurar sobre Test afecta también baseline, por lo que está fuera del alcance vigente de preservación |
| A8 | Pruebas negativas que intenten escrituras denegadas, inyección de fallos o alteren estado operacional | Requiere autorización de pruebas persistentes/infra según caso | Aprobar cada probe y recuperación; un WHERE 1=0 o rollback esperado no elimina este requisito |

Permisos A4 propuestos: identity.Users SELECT/INSERT; PermissionCatalog SELECT; Grants SELECT; Sessions SELECT/INSERT/UPDATE; identity.AuditEntries INSERT; organizations.Organizations SELECT; OrganizationMemberships SELECT. Sin DELETE Sessions, UPDATE Users/Grants, lectura de auditoría runtime, acceso organizations.AuditEntries ni DDL runtime. Los permisos efectivos podrían ser mayores por herencia; deben medirse antes/después, no presumirse mínimos por el script.

Fixtures necesitan un operador existente con capacidad suficiente; kdi_app no puede administrar/limpiar y kdi_test_runner es lectura. El harness SQL actual exige Integrated Security, no credencial SQL en entorno. No crear usuario, conceder permisos o usar sysadmin para desbloquearlo automáticamente. Si ninguna identidad existente aprobada cumple, registrar bloqueo y diseñar alternativa separada.

Las variables de opt-in, los hashes y el comando `AccessAdmin plan` no constituyen autorización humana. Este último conecta y obtiene applock aunque revierta su transacción.

Primera autorización solicitada: **A1 únicamente**, conexión de lectura Test con operador existente designado. Backup, migración, permisos, datos e infraestructura se aprueban después, con valores concretos y evidencias previas. No se solicita aprobación genérica ni se interpreta silencio como consentimiento.
