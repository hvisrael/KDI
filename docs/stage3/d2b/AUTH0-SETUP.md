# Auth0 Free — preparación humana necesaria

Estado: NO EJECUTADO. El usuario confirmó que tenant, aplicación y cuentas sintéticas aún no existen. Este documento es una guía de operador; no autoriza contratar, registrar aplicaciones ni crear usuarios reales.

## Registro por operador autorizado

1. Crear/seleccionar tenant exclusivo KDI de pruebas en región US, plan Free real, dominio estándar Auth0. No depender de funciones de un trial ni activar opciones pagadas/custom domain. Verificar condiciones actuales del plan en https://auth0.com/pricing antes de registrar.
2. Crear una aplicación Regular Web Application llamada KDI-D2B-Local; Authorization Code, autenticación del token endpoint client_secret_post; no refresh tokens, grants password/client_credentials ni API audience. No activar Native Organizations/RBAC como autoridad de KDI.
3. Allowed Callback URL exacta: https://localhost:7443/auth/callback. Allowed Logout URL: https://localhost:7443/auth/signout-callback. Sin comodines. Origen del navegador https://localhost:7443; no hace falta habilitar CORS en la API KDI.
4. Habilitar únicamente la conexión de base de usuarios dedicada de pruebas y deshabilitar signups en ella. Sin social/enterprise connections ni altas por dominio de email. No cambiar otras aplicaciones del tenant.
5. Obtener issuer exacto del discovery y clientId. Son configuración no secreta. Client secret se introduce solo mediante la entrada local protegida del launcher/API; no enviarlo por conversación, archivos, appsettings, argumentos, logs ni manifiestos.
6. Tras autorización de cuentas sintéticas, crear seis identidades de prueba controladas: administradorA, administradorB, usuario compartido A/B, auditorA, lector restringidoA y usuario sin membresía. El operador elige identificadores e introduce credenciales directamente en el IdP. No almacenar contraseñas en fixtures. No usar identidades de IDI ni datos personales reales.

## Preparación local separada

Se necesita certificado válido para localhost y confianza del usuario Windows autorizado, escucha loopback HTTPS7443, directorio privado del keyring fuera del repositorio/SQL con DPAPI y ACL, y entrada protegida de SQL kdi_app/OIDC client secret. No instalar certificados ni cambiar confianza/políticas sin autorización específica. No deshabilitar validación TLS del proveedor.

Las tablas Identity/Organizations y los GRANT mínimos deben aplicarse únicamente después de revisar/autorizar el paquete SQL generado. El bootstrap se ejecuta por CLI privilegiado separado; primer login crea identidad local sin pertenencia y nunca la reactiva automáticamente.

## Verificación real requerida

Con el entorno autorizado, iniciar API con auth habilitada y frontend compilado del mismo candidato. Validar discovery/issuer, PKCE S256, state/nonce, callback y cookie; login/logout HTTPS real, dos organizaciones, expiración, revocación y sesiones separadas. Cualquier entrada de contraseña se hace por el humano en la ventana privada correspondiente.

Desactivar trace/HAR/video/screenshots y no serializar request/response de autenticación, códigos de callback, cookies o tokens. Guardar solo assertions redactadas, conteos, status y hashes de archivos no secretos. Pruebas con proveedor simulado o respuestas HTTP interceptadas no satisfacen esta verificación.

La baja solo en Auth0 no cierra inmediatamente la sesión SQL de KDI: el operador debe revocar también en KDI. La revocación local surte efecto en la siguiente petición iniciada tras commit; límite absoluto1 hora, idle30 minutos. No back-channel Enterprise ni SLA de revocación remota5 minutos en este piloto.
