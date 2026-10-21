# Decisiones para continuar la fundación

D1 ACEPTADO por el Human Orchestrator mediante «Acepto», en respuesta directa a la solicitud de aceptación de D1. D2 y D3 permanecen pendientes. La aceptación parcial no altera los ADR históricos ni autoriza persistencia, identidad o CI.

## D1 — Solución y verificaciones base

Propuesta concreta de alcance inicial, derivada de ADR-001, ADR-002 y ADR-005:

1. Crear Kulani.KDI.slnx con Domain, Application, Contracts, Infrastructure y Api; Web React/TypeScript/Vite independiente, sin csproj artificial. SDK inicial propuesto 10.0.103, disponible localmente, sujeto a comprobación de soporte antes de restore.
2. Referencias: Application → Domain/Contracts; Infrastructure → Application/Domain/Contracts; Api → Application/Contracts e Infrastructure exclusivamente para composición; Domain y Contracts sin referencias entre sí ni a infraestructura. Carpetas por módulo; pruebas rechazan ciclos y referencias internas prohibidas.
3. Primera API limitada a liveness y metadatos no sensibles, Problem Details y correlación. Sin endpoints de negocio, autenticación simulada ni conexión automática a SQL. OpenAPI del contrato base verificado por pruebas de host.
4. Pruebas base: xUnit para backend/API/arquitectura; frontend con typecheck, lint, build y prueba de carga. Playwright para smoke del shell cuando se concrete su versión compatible. El shell presenta estado de conexión; no ofrece acciones de negocio sin backend autorizado.
5. Elegir versiones exactas verificadas de paquetes .NET/frontend desde nuget.org y registry.npmjs.org; restauración confinada al workspace/cachés autorizados, sin instalaciones globales. Registrar matriz, locks, licencias y comandos antes de declarar reproducibilidad. Node 20.12.2 observado no se presume compatible con versiones actuales de Vite.
6. Implementación por el principal, sin delegación ni sustitución del piloto bloqueado. Sus pruebas no reemplazan QA/review independientes. Git local puede inicializarse después de revisar ignore y archivos; sin commit/push/merge.

Aceptar D1 autorizaría el tramo solución/shell/pruebas y restauraciones oficiales necesarias. No autoriza D2/D3 ni cierra los ADR completos. Antes de ejecutar se registra la aceptación auténtica en este índice y el TaskInput específico de implementación.

## D2 — Persistencia e identidad

Pendiente de datos externos concretos: versión/edición/compatibility level SQL Server, servidor y nombre de DB de pruebas aislada, quién puede crearla y aplicar/limpiar migraciones. Nunca solicitar credenciales en documentos.

ADR-003: se mantiene una DB, esquema/DbContext/historial por módulo, rowversion y auditoría atómica como propuesta. La coordinación alta de proyecto/archivo de cliente requiere diseño de locks, orden y aislamiento más prueba concurrente SQL antes de esos casos. No se escoge una estrategia incompleta solo para crear un esquema.

ADR-004: falta IdP y flujo browser. Preparar comparación BFF/cookie/CSRF frente a SPA Code+PKCE, sesiones y revocación, y matriz de pertenencia/permisos con el proveedor elegido. No introducir contraseñas propias ni bypass de autenticación para completar una demo.

## D3 — CI y cierre

Propuesta: script de validación local portable primero; adaptador de CI después de designar proveedor/repositorio. Validar backend/web/contrato, sin despliegue, secretos ni DB implícita. Integración SQL exige servicio aislado explícito. No seleccionar GitHub/Azure por suposición.

Congelar candidato con rutas/SHA-256; registrar comandos, salidas y omisiones. QA y revisión independientes sobre ese mismo candidato requieren runtime/contrato/presupuesto validados. No usar esta sesión multiagente como evidencia del piloto. La aceptación final humana permanece pendiente.

