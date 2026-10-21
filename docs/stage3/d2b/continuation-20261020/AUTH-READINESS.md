# D2-B: preparación de autenticación y seguridad

No se crearon tenant, aplicación, cuentas, certificados, keyring ni permisos. No se solicitó ni registró ningún secreto. La plantilla existente `tools/local-access.example.json` mantiene acceso deshabilitado y valores públicos de ejemplo; no se carga automáticamente.

La propuesta conserva Auth0 Regular Web Application, Code + PKCE, scopes openid/profile, sin guardar tokens ni solicitar refresh tokens. Origen https://localhost:7443; callback /auth/callback; salida /auth/signout-callback. El secreto y SqlCredential se introducen por consola privada aprobada; nunca por conversación, archivos, argumentos o logs. HTTPS requiere certificado localhost aprobado y keyring externo con ACL privada y DPAPI. No se cambió confianza ni configuración global.

La documentación primaria de Auth0 confirma [PKCE para clientes web confidenciales](https://auth0.com/blog/pkce-in-web-applications-with-spring-security/) y [coincidencia exacta de post_logout_redirect_uri](https://support.auth0.com/center/s/article/Error-invalid-request-The-post-logout-redirect-uri-querystring-parameter-XXX-is-not-defined-as-a-valid-URL-in-Allowed-Logout-URLs), consultadas en esta continuación. Esto respalda la preparación documental; discovery, intercambio y logout del tenant concreto siguen sin comprobarse.

| Prueba real | Estado y dependencia |
|---|---|
| Login, PKCE S256, state/nonce y replay | BLOCKED: tenant/aplicación/cuentas, HTTPS y almacenamiento autorizado |
| Logout local y Auth0, retorno exacto, rechazo de cookie previa | BLOCKED: entorno real y persistencia |
| Cookie Secure/HttpOnly/SameSite, CSRF y origen | Contratos locales verificables; comportamiento HTTPS real BLOCKED |
| Aislamiento multiempresa y permisos de organización/proyecto | BLOCKED: fixtures SQL de dos organizaciones y usuarios autorizados |
| Idle 30 minutos y absoluto 1 hora | Casos unitarios disponibles; sesión real BLOCKED |
| Revocación, usuario inactivo y carrera con renovación | Simulación local disponible; carrera SQL real BLOCKED |
| Último administrador, bootstrap y auditoría atómica | BLOCKED: autorización de integración SQL |
| Caída de SQL y recuperación sin fallback | Contratos locales disponibles; fallo real BLOCKED |

Matriz detallada: [AUTH-SECURITY-MATRIX.md](../preparation-20261020/AUTH-SECURITY-MATRIX.md). Preparación de operador: [LOCAL-SETUP.md](../preparation-20261020/LOCAL-SETUP.md). No confundir navegador con respuestas interceptadas con interoperabilidad OIDC real. Para pruebas reales conservar únicamente assertions redactadas, tiempos, estados y hashes no secretos; sin HAR, trazas, video ni capturas del flujo autenticado.
