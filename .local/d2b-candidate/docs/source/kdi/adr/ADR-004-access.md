# ADR-004 — Identidad, permisos y alcance

Fecha: 2026-10-17. Estado: Propuesto; proveedor y flujo browser pendientes.

Contexto: aislamiento empresarial y permisos que dependen de organización y proyecto, además del rol.

Propuesta: OIDC con IdP a seleccionar; identidad por subject estable; políticas por permiso/recurso, RBAC más alcance y denegación por defecto. Pertenencia empresarial en Organizations; concesiones de acceso en Identity con IDs de alcance opacos. Orquestación en Application evita dependencia circular. Cada endpoint valida contexto autenticado, pertenencia y concesión; no basta un OrgId enviado por el cliente.

Alternativas: autorización solo por rol es insuficiente para acceso horizontal; identidad propia con contraseñas aumenta responsabilidad operativa; proveedor externo requiere decisión de costos, disponibilidad y ciclo de vida. Para browser comparar BFF/cookie segura con defensa CSRF frente a SPA/OIDC Authorization Code + PKCE y política de tokens. No decidir almacenamiento de tokens por conveniencia.

Pendientes: proveedor, múltiples organizaciones por usuario, scopes, administradores, sesiones/revocación, CORS, CSRF, auditoría, recuperación y cuentas de prueba. No aprobar implícitamente privilegios globales.

Validación: matriz rol×acción×alcance, recursos ajenos por ID, conteos, expiración y revocación según diseño. Nunca usar credenciales ni sesiones de IDI.
