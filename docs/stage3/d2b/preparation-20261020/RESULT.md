# D2-B — preparación local del 2026-10-20

Estado: **validación local ejecutada; integración real BLOCKED; D2-B NO cerrado**. Esta entrega no constituye aceptación ni autoriza operaciones SQL/infraestructura. Solo se modificó el candidato aislado y documentación nueva D2-B.

## Compilación y pruebas observadas

| Comprobación | Resultado | Evidencia |
|---|---|---|
| Solución backend Release, después de preparar SQL | Exit 0, 0 errores/advertencias | backend-build-final.log |
| UnitTests | 57/57, 0 fallidas/omitidas | UnitTests.log |
| ApiTests (TestHost y almacenamiento doble) | 20/20, 0 fallidas/omitidas | ApiTests.log |
| ArchitectureTests | 24/24, 0 fallidas/omitidas | ArchitectureTests.log |
| Frontend typecheck/lint/build | Exit 0 en los tres | web-typecheck.log, web-lint.log, web-build.log |
| Playwright Chromium | 11/11, 0 fallidas/omitidas/flaky | browser.log; candidato/docs/stage3/d2b/evidence/browser-contracts.json |
| Generación SQL offline | 12 artefactos reproducidos idénticos; 14 hashes preparados | SQL-REVIEW.md; prepared-sql y repro-sql en herramienta |
| Assets para piloto de mismo origen | Copia preparada; no arranque HTTPS | prepare-web.log |
| Integridad D2-A original/aceptado y evidencia | Verificador exit 0 antes de cambios | Comando reproducible abajo |
| SQL conectado, migraciones, permisos, fixtures y downgrade | NO EJECUTADO / BLOCKED | Restricción humana explícita |
| Auth0 y navegador HTTPS real | NO EJECUTADO / BLOCKED | Tenant/configuración/cuentas, certificado, keyring y entrada privada pendientes |

No se afirma restore limpio, auditoría de dependencias vigente ni actualización SDK/runtime. Se usaron paquetes y node_modules ya existentes. Toolchain observado: .NET SDK 10.0.103, Node v22.23.3, npm 10.5.0. La compilación usa `--no-restore`; no hubo descargas ni cambios de versión. Los tests se ejecutaron con `--no-build --no-restore` después de compilar. El intento browser histórico sin DLL Release quedó resuelto en esta ejecución.

Comandos desde `.local/d2b-candidate`:

```text
dotnet build Kulani.KDI.slnx -c Release --no-restore
dotnet test --project tests/Kulani.KDI.UnitTests/Kulani.KDI.UnitTests.csproj -c Release --no-build --no-restore
dotnet test --project tests/Kulani.KDI.ApiTests/Kulani.KDI.ApiTests.csproj -c Release --no-build --no-restore
dotnet test --project tests/Kulani.KDI.ArchitectureTests/Kulani.KDI.ArchitectureTests.csproj -c Release --no-build --no-restore
```

Variables no secretas de compilación: DOTNET_CLI_HOME=E:/INSSOFT/KDI/.local/dotnet-home; NUGET_PACKAGES=E:/INSSOFT/KDI/.local/nuget/packages. Desde src/Kulani.KDI.Web: npm run typecheck; npm run lint; npm run build; node node_modules/@playwright/test/cli.js test. PATH antepone C:/Users/Israel/AppData/Local/pi-node/current; PLAYWRIGHT_BROWSERS_PATH=C:/Users/Israel/AppData/Local/ms-playwright. Playwright gestiona y cierra sus propios servidores; no usa credenciales reales ni Auth0.

Integridad aceptada, desde raíz KDI: `python docs/stage3/d2a/closure/identities-20261018/verify.py`. D1 conserva su manifiesto aceptado; el árbol raíz ya evolucionado a D2-A no se presenta como snapshot fuente D1.

## Cambios e inventario

La API, persistencia y frontend D2-B ya existían. En esta continuación se añadió exportación offline de rollback hasta baseline al generador, paquete SQL por destino con hashes y verificador, helper de assets para piloto y plantilla no secreta de configuración deshabilitada. No se cambiaron modelos, migraciones EF históricas ni autenticación para hacer pasar pruebas.

Ocho tablas nuevas: identity.Users, PermissionCatalog, Grants, Sessions, AuditEntries; organizations.Organizations, OrganizationMemberships, AuditEntries. Roles fijos en aplicación; sin tabla Roles. Detalle de claves, restricciones y migraciones en [SQL-REVIEW.md](SQL-REVIEW.md). Los scripts Test/Dev se prepararon, no ejecutaron: [paquete SQL](../../../../.local/d2b-candidate/tools/Kulani.KDI.Migrations/prepared-sql/manifest.json).

## Candidato propuesto para revisión

Raíz `.local/d2b-candidate`; manifiesto `candidate-manifest.json`, 190 archivos; SHA-256 `1ab77cd831a7426ba0cdd864cbb0c3edd5479f2d81a416cfec159a12a60888f6`.

> **Actualización 2026-10-21.** Este registro describe la **preparación del 2026-10-20**; los 190 archivos y el hash `1ab77cd8…` son los de ese momento y se conservan como evidencia histórica. El candidato **vigente** es `91dce7d9e495ab6f9596336c4a0bf250e12c15eafd814701b597d20e9e0cb448` con 197 archivos. Los manifiestos intermedios se conservan en este mismo directorio (`candidate-manifest.20261020-*.json`), de modo que el diff entre estados es exhaustivo. Estado canónico: [docs/stage3/STATE.md](../../STATE.md).

`python docs/stage3/d2b/preparation-20261020/freeze.py --verify` compara contenido e inventario completo dentro de las exclusiones explícitas. Quedan fuera bin/obj/node_modules/dist/wwwroot, logs y evidencia browser. El hash identifica fuentes/SQL preparados, no un release ni la evidencia externa. Los artefactos generados deben reconstruirse para cada ejecución.

## Preparación QA y bloqueos

Contrato QA: evaluar este hash sin modificarlo; reproducir pruebas locales y generación offline; conservar SQL/Auth0/HTTPS como BLOCKED. [Matriz](AUTH-SECURITY-MATRIX.md) y [preparación del entorno](LOCAL-SETUP.md) separan contratos con dobles de pruebas reales. Los informes independientes se añaden fuera del candidato.

Bloqueos reales: autorización específica de migraciones/fixtures/permisos Test, infraestructura HTTPS/keyring aprobada y configuración/cuentas Auth0. No se necesitan secretos por conversación; entrada solo en consola privada autorizada. SQL contiene 8 casos pendientes; los negativos efectivos y carreras no se sustituyen por inspección estática.

Siguiente acción concreta: resolver hallazgos independientes, fijar paquete Test exacto y solicitar autorización de sus operaciones y fixtures antes de ejecutar. Preparar Auth0/HTTPS en paralelo mediante operador autorizado. Dev queda para autorización posterior. No avanzar a D2-C ni producción.
