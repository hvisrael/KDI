# Preparación Auth0/OIDC y HTTPS

No se registró aplicación, creó identidad, instaló certificado, cambió confianza/ACL ni inició acceso real. `tools/local-access.example.json` es una plantilla no cargada automáticamente y mantiene Enabled=false. No contiene secretos.

Configuración propuesta: aplicación Auth0 Regular Web Application dedicada, Authorization Code con PKCE, autenticación client_secret_post, scopes openid/profile y sin refresh tokens. Issuer exacto del discovery, clientId público. Callback https://localhost:7443/auth/callback y logout https://localhost:7443/auth/signout-callback, sin comodines. Confirmar discovery/end_session_endpoint y compatibilidad logout_hint/client_id sin id_token_hint en la prueba real. No atribuir compatibilidad real al test de opciones.

Referencias primarias consultadas el 2026-10-20: [Auth0: PKCE en aplicaciones web confidenciales](https://auth0.com/blog/pkce-in-web-applications-with-spring-security/), [Auth0: coincidencia exacta de retorno de logout](https://support.auth0.com/center/s/article/Error-invalid-request-The-post-logout-redirect-uri-querystring-parameter-XXX-is-not-defined-as-a-valid-URL-in-Allowed-Logout-URLs). No se evaluaron ni contrataron planes comerciales.

Antes del arranque autorizado:

1. Preparar tenant/aplicación y seis cuentas sintéticas según ../AUTH0-SETUP.md; contraseñas solo en la UI del proveedor.
2. Aprobar y preparar certificado localhost válido/confiable y directorio externo privado de keyring DPAPI. La app valida ACL y no lo crea. No ejecutar `dotnet dev-certs --trust` sin autorización.
3. Aprobar migraciones y permisos exactos Test; obtener credenciales mediante consola privada. La conexión no lleva User ID/Password: la API recibe SqlCredential kdi_app con `--kdi-sql-console-credential`. El secreto OIDC se recibe con `--kdi-oidc-console-secret`; nunca pasar su valor en argumentos/env/JSON.
4. Tras build frontend ejecutar `python tools/prepare-local-web.py`: copia dist a wwwroot del candidato. Se comprobó la copia; rechaza sobrescritura. Es salida generada ignorada/excluida del manifiesto. La API sirve archivos solo con acceso habilitado. El navegador local ejecutado usa preview y proxy, no acredita este piloto HTTPS.
5. La plantilla JSON sirve como referencia para claves de configuración no secretas, no como comando de arranque. Concretar los valores aprobados antes de habilitar acceso. La API usa https://localhost:7443 y el certificado seleccionado por Kestrel; no se configuró certificado en esta sesión.

No usar el servidor Vite ni desactivar validación TLS para el flujo real. Mantener logging de autenticación/HTTP/EF suprimido como en el candidato. No lanzar desde un runner que capture consola privada. SQL permanece restringido a DESKTOP-F25O3MF/KulaniKDI_Test para la primera validación.

La primera variante de helper PowerShell no se pudo ejecutar por política de firma. Se sustituyó por helper Python portable sin cambiar la política de ejecución; la copia se ejecutó correctamente. No hay cambios globales.
