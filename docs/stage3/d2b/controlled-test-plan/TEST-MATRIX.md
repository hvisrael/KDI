# Infraestructura, pruebas reales y aceptación D2-B

Estado inicial de todas las pruebas conectadas: **NOT RUN / BLOCKED**. Esta matriz es un procedimiento, no evidencia de ejecución. Candidato a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102.

## Checklist previo del operador

- [ ] A1–A6 pertinentes aprobadas por separado; operador Windows/SQL existente identificado; ventana y responsables de parada/recuperación designados.
- [ ] Servidor/base/version/compatibility/baseline reales comprobados por lectura autorizada; snapshot de esquema/permisos y backup recuperable acordado. Identidad integrada del harness con DML suficiente ya existente; no elevar kdi_test_runner ni runtime.
- [ ] Tenant Auth0 de pruebas exclusivo y aplicación Regular Web Application preparados por operador; no funciones pagadas/trial como dependencia. Issuer discovery exacto y clientId públicos confirmados; secreto disponible solo para entrada privada.
- [ ] Code + PKCE S256, scopes openid/profile, sin refresh tokens; conexión de usuarios sintéticos dedicada, sin signup público. No usar email como autoridad ni organizaciones/RBAC externos como autoridad KDI.
- [ ] Callback exacto https://localhost:7443/auth/callback y logout exacto https://localhost:7443/auth/signout-callback; sin comodines; discovery/end_session_endpoint y comportamiento sin id_token_hint pendientes de validación real.
- [ ] Seis cuentas controladas: adminA, adminB, compartido A/B, auditorA, lector restringidoA, sin membresía; identificadores concretos registrados sin contraseñas. Dos organizaciones A/B y grants/proyectos sintéticos aprobados; manifiesto de IDs de corrida para limpieza.
- [ ] Certificado localhost válido con SAN/uso adecuados, clave privada accesible solo a identidad aprobada, confianza aprobada; loopback 7443 libre. No deshabilitar TLS IdP/navegador ni usar Vite para autenticación real.
- [ ] KeyDirectory externo existente, ruta concreta aprobada, ACL solo operador/SYSTEM/Administrators, DPAPI Windows mismo usuario, sin repositorio/sincronización pública. No crear ni modificar ACL sin A6.
- [ ] Configuración privada concreta y frontend mismo origen reconstruido desde congelado; template Enabled=false no inicia por sí sola. wwwroot generado previamente debe inspeccionarse antes de reemplazo: helper rechaza sobrescritura.
- [ ] Credenciales kdi_migrator/kdi_app vigentes entregadas exclusivamente por consola privada sin captura; ninguna contraseña en argumentos, entorno, JSON, logs o evidencia. Harness usa solo connection string integrada no secreta; no admite el mismo mecanismo SqlCredential sin cambios.
- [ ] SQL actual usa Encrypt con TrustServerCertificate=true en rutas de operador/piloto: excepción local existente que no prueba identidad TLS del servidor. Decisión documentada de seguridad requerida para piloto o cambio separado y nuevo congelado; no extenderla a Auth0/HTTPS ni producción.
- [ ] Logs de HTTP/autenticación/EF suprimidos; trace/HAR/video/capturas desactivados en flujo autenticado. Conservar solo código de caso, UTC, duración, assertion/estado y hash; no URL con código/state, tokens, cookies, tickets, handles ni cuerpos OIDC.
- [ ] Harness/limpieza reparado o alternativa revisada y aprobada: residuos de auditoría, cancelación, recuperación por IDs. Clases SQL ejecutadas secuencialmente y fixtures de acceso limpiados antes de administración; baseline excluido.

## Matriz integrada

| ID | Caso y ejecución prevista | Resultado verificable exigido | Puerta |
|---|---|---|---|
| SQL01 | Preflight y baseline | Destino exacto; historias aceptadas/estado conocido; Portfolio intacto; sin NULL convertido en permiso denegado | A1 |
| SQL02 | Upgrade identity y organizations | Ocho tablas/índices/FK/CHECK/rowversion y ocho códigos correctos; solo dos Access añadidas | A2/A3 |
| SQL03 | Segunda aplicación de archivos exactos | Sin duplicados/cambio de seeds o datos; mismo esquema e historial; exit 0 | A3 |
| SQL04 | Conexión real kdi_app y matriz efectiva | Operaciones mínimas funcionan; sin DDL/DELETE sesiones/escritura org/membresías/grants ni privilegios globales. Probes de denegación redactados y autorizados | A1/A4/A5/A8 |
| SQL05 | Cinco casos administración + uno acceso elegidos, en invocaciones secuenciales | Unicidad issuer/sub exacta, membresía autoritativa, último admin conservado, bootstrap único, usuario deshabilitado denegado, fallo auditoría revierte mutación; cero residuos comprobados | A5/A8; harness resuelto |
| SQL06 | Revoke tras lectura stale y carrera simultánea adicional | Fixture existente demuestra revocación seguida de renovación stale rechazada; prueba adicional con barreras/orden real debe demostrar que ninguna renovación concurrente revive sesión | A5 |
| CLI01 | CLI plan/apply real con operador aprobado | Plan no cambia datos; SID/30min/hash/versiones controlados; plan alterado/expirado/estado modificado rechazado; apply aprobado auditado atómicamente | A1/A5/A8 para probes negativos |
| TLS01 | HTTPS real y cookies | Cadena localhost confiable; Secure/HttpOnly/__Host/path y SameSite esperados; API no accesible externamente; sin mixed content | A6 |
| KEY01 | Keyring persistente | Reinicio del piloto por mismo usuario preserva sesión válida; ACL/DPAPI verificados sin leer/exportar claves. Pruebas con ACL inválida solo en directorio aislado aprobado sin alterar keyring operativo | A5/A6/A8 para prueba negativa |
| OIDC01 | Login de seis cuentas, discovery e intercambio reales | Issuer/audience/firma/algoritmo, PKCE S256, state/nonce correctos; identidad local creada sin membresía automática; inactiva no reactivada | A4/A5/A6 |
| OIDC02 | state/nonce incorrectos, callback replay y returnUrl externo | Sin sesión autenticada nueva ni redirección externa. No copiar códigos/tokens a herramientas o evidencia; harness privado revisado o registrar BLOCKED | A5/A6/A8 |
| OIDC03 | Logout KDI + Auth0 | Sesión local revocada antes de salida; retorno exacto; cookie previa rechazada; reautenticación IdP observada sin asumir que logout remoto revoca todas las sesiones locales | A5/A6 |
| CSRF01 | Mutación sin CSRF o Origin ajeno | Rechazo sin mutación; con solicitud válida solo operación autorizada | A5/A6/A8 |
| ORG01 | A, B, usuario compartido y sin membresía | Sin lista/datos de organización ajena; acceso directo ajeno 404; contexto por pestaña no cruza datos; compartido solo según membresía/grants | A5/A6 |
| AUTHZ01 | Roles, proyecto restringido, grants revocados y usuario inactivo | Denegación por defecto; permisos limitados a org/proyecto; cambios tienen efecto en siguiente petición iniciada tras commit; UI no sustituye control API | A5/A6 |
| EXP01 | Idle 30 min | Sin peticiones de fondo que renueven sesión, acceso rechazado tras 30 min sin actividad; evidencia UTC y estado. No cambiar reloj global | A5/A6 |
| EXP02 | Absoluto 1 h con actividad antes de idle | Acceso rechazado tras 1 h desde creación aunque hubiera renovación; no reducir límites del candidato para simular aprobación | A5/A6 |
| REV01 | Revocación local y disable/membership con sesiones abiertas | Siguiente petición iniciada después del commit denegada, incluidas otras pestañas; no resurrección por renovación; baja solo Auth0 no cuenta como revocación local | A5/A6 |
| FAIL01 | Caída de almacenamiento por inyección local aprobada | Respuesta 503/sin fallback ni datos stale; recuperación con misma identidad runtime. No detener SQL Server ni afectar otras bases | A5/A6/A8 |
| REC01 | Recuperación acordada | Backup verificable y restauración de ensayo autorizada o evidencia suficiente aprobada; downgrade/recovery separados no asumidos por SQL03 | A2/A7 |

El harness actual llama persistencia directamente y no acredita privilegios de kdi_app ni el protocolo de CLI. No afirmar carrera concurrente de sesiones basándose únicamente en revocación y renovación secuenciales. No ejecutar SqlBaselineTests: incluye Portfolio, downgrade a cero dentro de transacción y un caso que presupone sysadmin; queda fuera del plan vigente.

## Criterios verificables de aceptación

Cada caso aplicable debe tener resultado real, evidencia redactada, fecha UTC, operador/QA, destino y hashes del candidato/scripts; ningún criterio crítico BLOCKED o fallido admite PASS global. Cualquier exclusión exige decisión humana explícita y no se registra como prueba exitosa.

Se exige: integridad D1/D2-A sin cambios de artefactos; Portfolio/objetos baseline preservados; SQL02–06 y permisos efectivos acreditados; fixtures recuperables sin residuos; CLI real; login/logout/TLS/keyring/CSRF; expiración idle/absoluta; revocación y aislamiento A/B; restauración/recuperación acordada. Regresiones locales deben corresponder al mismo congelado que se ejecuta. Cambios de fuente/harness/dependencias requieren nuevo candidato y revisión independiente.

QA independiente valida resultados sin modificar candidato; revisión de seguridad/SQL confirma alcance y mínimos. Aceptación humana debe identificar hash y límites; no se convierte en autorización de Dev ni producción.

[Auth0 exige coincidencia exacta del retorno OIDC](https://support.auth0.com/center/s/article/Error-invalid-request-The-post-logout-redirect-uri-querystring-parameter-XXX-is-not-defined-as-a-valid-URL-in-Allowed-Logout-URLs). Configuración documental no demuestra compatibilidad del tenant concreto; permanece por probar.
