# D2-B — matriz de validación, 2026-10-20

Estado global: preparación local; NO cerrado. Las pruebas con dobles no acreditan SQL ni Auth0 reales.

| Área | Evidencia local comprobada | Verificación real pendiente |
|---|---|---|
| Login OIDC | ApiTests: Code+PKCE, issuer/audience/firma y tratamiento de identidad | Discovery Auth0, intercambio real, PKCE S256, state/nonce inválidos y callback replay: BLOCKED por tenant/aplicación/cuentas y HTTPS |
| Logout | Revocación en store doble; configuración sin guardar tokens | Logout SQL primero, salida Auth0, retorno exacto y no reutilización: BLOCKED |
| Cookies/CSRF | ApiTests: atributos Host/Secure/HttpOnly/SameSite; rechazo Origin y CSRF; returnUrl | Navegador HTTPS real, callback y cookies reales: BLOCKED por certificado/confianza/keyring |
| Multiempresa | UnitTests: default deny, membresía, grants organización/proyecto; ApiTests: organización ajena 404; browser: cambio de contexto y pestañas separadas | Dos organizaciones y usuarios sintéticos en SQL, aislamiento y revocación entre peticiones: BLOCKED por autorización de fixtures SQL |
| Expiración | UnitTests: idle 30 minutos, absoluto 1 hora, límites y renovación | Sesión real, reloj controlado o espera y evidencia redactada: BLOCKED |
| Revocación | UnitTests: usuario inactivo, sesión revocada, CAS y carrera simulada | Carrera SQL real renovación/revocación, sin resurrección: BLOCKED |
| Administración | Código/compilación de CLI, plan/hash/rowversion y lock | Último administrador, bootstrap concurrente, rollback auditoría y disable/membership en SQL: BLOCKED |
| Migraciones | Generación offline y revisión estática, ver SQL-REVIEW.md | Upgrade, repetición, permisos efectivos y rollback transaccional en Test: BLOCKED |
| Caída de almacenamiento | ApiTests: 503 sin fallback; browser: no muestra organizaciones | Fallo real SQL y recuperación con misma identidad runtime: BLOCKED |

Suites ejecutadas: UnitTests 57/57, ApiTests 20/20, ArchitectureTests 24/24; navegador 11/11 (6 contratos de acceso sintéticos y 5 de fundación, incluido proxy a API real sin autenticación). Cero omitidas en estas suites. La suite SqlIntegrationTests NO se ejecutó: contiene 8 casos, incluidos probes de permisos e integración con operaciones persistentes excluidas de esta sesión.

> **Actualización 2026-10-21.** Esta matriz describe la **preparación del 2026-10-20** y sus números son los de ese momento. Desde entonces el candidato ha cambiado y las cifras vigentes son **71** unitarias, 20 API, 24 arquitectura y 11 navegador (14 tests nuevos proceden de la corrección de la ruta de credenciales del operador). Además, **A4 se ha concedido**: `kdi_application` tiene ya los 10 permisos runtime sobre 7 objetos, sin `DELETE`, sin permiso sobre `organizations.AuditEntries` y sin concesiones de esquema, y sus seis `DENY` de DDL siguen intactos. Estado canónico: [docs/stage3/STATE.md](../../STATE.md).

Para la ejecución real guardar solo assertions redactadas, tiempos, estados y hashes de artefactos no secretos. Sin HAR, trace, screenshots, video, cuerpos OIDC, cookies, tokens ni credenciales. La baja en Auth0 no sustituye revocación local en KDI.
