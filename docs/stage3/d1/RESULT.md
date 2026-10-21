# Resultado D1 — PASS técnico; aceptación humana pendiente

> **ESTADO VIGENTE:** D1 fue **ACEPTADO** por el Human Orchestrator. Ver [acceptance/HUMAN-APPROVAL.md](acceptance/HUMAN-APPROVAL.md) y [acceptance/STATUS.json](acceptance/STATUS.json) (`humanDecision: APPROVED`, `candidateHash: e2bb22b739b5924e8c2ead3af421e56ac97dc6e6b0209a7d0b7b5da8d1f51193`). El título y la frase «Aceptación humana pendiente; D2 no autorizado» de este documento **describen el estado en el momento de emitirse** y quedan resueltos exclusivamente por ese recibo posterior, sin modificar los artefactos evaluados. Estado vigente del proyecto: [docs/stage3/STATE.md](../STATE.md).

QA independiente /root/d1_closure_qa y Code Review independiente /root/d1_closure_review emitieron PASS para el candidato. La corrida vigente de cierre está en [closure/SUMMARY.md](closure/SUMMARY.md) y validation-summary.json: build Release, 40/40 xUnit (incluida redacción de excepción), smoke HTTP, arquitectura, typecheck, lint, build web y 5/5 Playwright pasaron. Auditorías de paquetes completadas; npm terminó con cero vulnerabilidades tras corrección compatible autorizada. NuGet ya no está bloqueado. Las limitaciones y revisiones independientes se registran en closure/. Aceptación humana pendiente; D2 no autorizado.

El contenido siguiente es HISTÓRICO y describe la primera entrega, no el estado de las pruebas del candidato de cierre.

# Resultado D1 — implementación disponible; cierre pendiente

D1 fue aceptado mediante respuesta humana directa. Se implementó el tramo autorizado por el principal. La verificación integral permanece BLOCKED por la restauración de las dependencias xUnit/API testing. No se omite ese criterio ni se convierte en PASS mediante las comprobaciones auxiliares.

## Entrega

- Kulani.KDI.slnx: Domain, Contracts, Application, Infrastructure y Api; tres proyectos xUnit separados. Domain e Infrastructure reservan sus reglas/adaptadores para incrementos autorizados, sin entidades ficticias ni SQL.
- API: GET /health/live y /api/v1/system/info; OpenAPI generado, errores Problem Details, correlación acotada a ASCII y sin cabecera Server. No hay endpoints de negocio, sesiones, simulación de identidad o readiness SQL.
- Web: shell accesible por teclado, estados de conexión/carga/error y recuperación; petición acotada a cinco segundos. Sin acciones de clientes/proyectos. Proxy local mismo origen; no se habilitó CORS global.
- Herramientas del autor para smoke HTTP y análisis semántico de arquitectura. Las pruebas xUnit comparten la política de arquitectura con la herramienta; no dependen de la ejecución de AI-ENGINEERING.
- SDK y dependencias directas fijados, locks backend/web, fuentes oficiales y cachés dentro de .local. El lock completo de las suites .NET de pruebas sigue pendiente.

## Verificación observada

| Comprobación | Resultado | Evidencia / límite |
|---|---|---|
| Restore backend con --locked-mode | PASS | Cache verificada; auditoría online omitida explícitamente |
| Build API y capas, Release | PASS | 0 errores, 0 advertencias |
| Smoke HTTP contra proceso real | PASS | api-smoke-result.json; ocho grupos de comprobaciones; exportó docs/api/openapi-v1.json |
| Grafo de proyectos y dependencias semánticas | PASS del autor | architecture-result.json; Roslyn del SDK fijado, sin errores semánticos |
| npm ci --offline --ignore-scripts --no-audit | PASS | 132 paquetes; package-lock.json respetado |
| Typecheck / lint / build web | PASS | TypeScript estricto, ESLint sin advertencias, build Vite |
| Playwright Chromium | PASS | playwright-result.json: 5 esperadas, 0 fallos/omitidas/flaky, salida 0, 5.7 s |
| Visual escritorio/móvil | Revisado por el autor | screenshots/desktop.png y mobile.png; sin desbordamiento a 375 px |
| Restore xUnit/API tests | BLOCKED | NU1801/NU1101: acceso a nuget.org no disponible y paquetes de pruebas ausentes en caché |
| Suites xUnit unitarias/API/arquitectura | NOT RUN | No se declara que las fuentes de esas suites hayan compilado o pasado |
| Auditoría de vulnerabilidades / firma fresca de paquetes | NOT RUN | Sin red; hashes locales e inventario de licencias no sustituyen auditoría |
| SQL, identidad, migraciones, CI | NOT RUN / fuera de D1 | Requieren D2/D3 |
| QA/review independientes | NOT RUN | Sin especialista, runtime validado ni candidato aceptado |

Los cinco casos de navegador cubren carga y teclado, fallo/recuperación, respuesta inesperada, viewport móvil y shell → proxy → API real. Los otros cuatro usan respuestas sintéticas para escenarios de interfaz; no son E2E de negocio ni de DB.

Las reglas de módulos están preparadas, pero todavía no hay módulos de negocio implementados que permitan demostrar su aislamiento real. La comprobación semántica actual valida las capas y la composición existentes; las suites de arquitectura contienen casos negativos de política pendientes de ejecución xUnit.

## Intentos y correcciones conservados

Las sondas HTTPS desde el proceso a npm, NuGet y nodejs.org fallaron. Se importaron entradas públicas verificadas de caché, sin configuración personal ni credenciales. El importador conserva variantes de metadata npm y LF para respetar su formato; no cambia el registro oficial configurado.

Node 20.12.2 no fue suficiente para dependencias modernas del lint. Se descubrió y utilizó Node 22.23.3 ya instalado junto con npm 10.5.0, sin actualización global. Vite 8 no pudo cargar su binding nativo ausente en caché; se seleccionó Vite 6.4.4, rama con parches de seguridad según la política oficial consultada. Véase TOOLCHAIN.md.

Un helper PowerShell sin firma fue rechazado por la política de ejecución; se retiró y no se cambió esa política. Las herramientas se invocaron directamente por sus ejecutables disponibles. El primer restore .NET emitió el banner de primera ejecución/certificado; no se ejecutó un comando de confianza de certificados. Las invocaciones posteriores deshabilitaron esa generación y usaron DOTNET_CLI_HOME local.

El primer arranque del navegador falló por contentRoot relativo; se corrigió con ruta absoluta. Después, cinco casos pasaron, pero el cierre automático por shell no terminó sin cerrar los dos servidores propios. Se sustituyó por globalSetup con procesos directos, handles propios y cierre acotado; la corrida final terminó sola en 5.7 s. No se reutilizan ni se detienen servicios ajenos.

El lock parcial producido por el restore fallido de ApiTests se conserva como failed-api-tests-restore.lock.json para evidencia; no se utiliza como lock válido de las suites.

## Pendientes concretos

1. Con acceso autorizado a nuget.org: restaurar los tres proyectos de pruebas, generar locks completos y ejecutar dotnet test. Resolver cualquier hallazgo y repetir las comprobaciones afectadas sobre el nuevo manifiesto.
2. Completar auditoría de paquetes y actualizar/verificar el parche soportado de SDK/runtime: 10.0.103/10.0.3 es la base local disponible y reproducida, no el parche vigente publicado de .NET 10. No se instaló otro SDK por falta de red.
3. Mantener pendientes las decisiones de SQL/DB aislada/IdP (D2), proveedor de CI y cierre (D3), QA/review independientes y aceptación humana final. Ningún PASS del autor los reemplaza.

implementation-manifest.json identifica esta entrega D1 mediante rutas y SHA-256. Excluye cachés, binarios, reportes y su propia carpeta de evidencia; no representa un release aceptado ni congela un candidato para revisiones independientes aún no autorizadas.
