# Modelo lógico inicial y trazabilidad

Propuesta, sin DDL, migraciones ni datos. Prefijos/esquemas definitivos pendientes de ADR-003.

| Dueño | Entidad | Campos/reglas iniciales |
|---|---|---|
| Identity & Access | Principal / Role / Permission / Grant | SubjectId externo estable; rol, acción y ScopeType/ScopeId; no almacenar tokens en auditoría |
| Organizations & Clients | Organization | Id, Code, Name, Status; autoridad de pertenencia empresarial |
| Organizations & Clients | OrganizationMembership | OrgId, PrincipalId, estado; diferenciar pertenencia de concesión de permisos |
| Organizations & Clients | Client | Id, OrgId, Code, NormalizedCode, Name, Status, Version, auditoría |
| Portfolio & Projects | Project | Id, OrgId, ClientId, Code, NormalizedCode, Name, Status, Version, auditoría |
| Cada módulo | AuditEntry | Id, OrgId, ActorId, Action, ResourceType/Id, OccurredAtUtc, CorrelationId, cambios mínimos redactados |

Propuestas: UUID/GUID para IDs; timestamps UTC; rowversion SQL Server para concurrencia (sin significado de fecha); índice único OrgId+NormalizedCode por tipo de recurso, incluso archivados hasta decidir política de reutilización. Longitudes y normalización coherentes entre API, dominio e índices. No confiar solo en validación previa para unicidad: capturar violación de índice y devolver conflicto seguro.

## Invariantes cruzadas

Project.OrgId = Client.OrgId y cliente elegible al crear/vincular proyecto. La carrera entre alta de proyecto y archivo de cliente requiere coordinación transaccional explícita. Comparar FK compuesta entre esquemas frente a operación coordinada con locks dentro de una DB; documentar consecuencias de acoplamiento y migraciones en ADR-003 antes de implementar. No usar navegación EF que convierta entidades de otro módulo en agregado propio.

Creación/actualización y auditoría deben confirmarse o fallar juntas para el primer incremento. Lecturas se filtran por organización y alcance en servidor; índices deben apoyar esa ruta. Revisar tanto recursos individuales como listados, búsquedas y conteos.

## Evolución y conservación

Demanda, requisitos, estimaciones, planes y criterios conservarán revisiones y fuente; horas tendrán correcciones trazables; QA conservará ejecución/evidencia; release y deployment conservarán versión y ambiente. No implementar esas tablas en fundación solo para anticiparse al backlog. Cada módulo incorpora su diccionario, invariantes y migraciones al entrar en alcance.

Sin borrado físico inicial; retención, anonimización, obligaciones legales y eliminación de datos personales requieren decisión propia. Sin importación IDI ni datos reales en pruebas.
