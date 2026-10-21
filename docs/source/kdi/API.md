# API inicial propuesta

Sin OpenAPI generado ni endpoints implementados. Fuente funcional: KDI-UC01–05. Contratos y política de ocultación de recursos ajenos requieren aprobación antes de código.

| Método y ruta relativa a /api/v1 | Intención | Permiso |
|---|---|---|
| GET /organizations/{orgId}/clients | Lista paginada autorizada | clients.read |
| POST /organizations/{orgId}/clients | Alta de cliente | clients.manage |
| GET /organizations/{orgId}/clients/{id} | Detalle autorizado | clients.read |
| PUT /organizations/{orgId}/clients/{id} | Edición con If-Match | clients.manage |
| POST /organizations/{orgId}/clients/{id}/archive | Archivo con If-Match | clients.manage |
| GET /organizations/{orgId}/projects | Lista paginada autorizada | projects.read |
| POST /organizations/{orgId}/projects | Alta vinculada a cliente | projects.manage |
| GET /organizations/{orgId}/projects/{id} | Detalle autorizado | projects.read |
| PUT /organizations/{orgId}/projects/{id} | Edición con If-Match | projects.manage |
| POST /organizations/{orgId}/projects/{id}/archive | Archivo con If-Match | projects.manage |

OrgId en ruta no concede acceso. Validar autenticación, membresía, permiso y alcance del recurso. No exponer existencia ajena: propuesta 404 uniforme al consultar IDs fuera de alcance; 403 para operación denegada en contexto accesible. Ratificar política y probarla de forma consistente.

Entrada cliente: code, name. Proyecto: code, name, clientId. No aceptar OrgId/actor/auditoría del body como autoridad. Salida: id, code, name, status, version; proyecto agrega clientId. ETag opaco derivado de versión para consultas y mutaciones; conflicto de versión 412, falta de If-Match 428 en operaciones que lo exigen.

Paginación propuesta page>=1, pageSize 1..100 (default 25), search acotada, status y orden permitido por allowlist; desempate por ID. TotalCount respeta el mismo alcance. POST exitoso 201 con Location; edición/archivo 200 con representación actual. 400 validación, 401 autenticación, 409 duplicado/regla de archivo, 404 recurso no visible. Errores Problem Details con código de negocio estable y correlationId, sin stack/SQL.

OpenAPI debe reflejar DTO, límites, políticas, ejemplos sintéticos, ETag/If-Match y errores. La unicidad protege doble alta; idempotency keys se evalúan para integraciones futuras. Nunca reintentar automáticamente una mutación de resultado incierto desde el cliente sin protocolo acordado.
