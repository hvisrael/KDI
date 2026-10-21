# Plan de pruebas KDI

No se ejecutan pruebas del producto en Etapa 1. Elegir frameworks/versiones al autorizar fundación. Propuesta xUnit para .NET y Playwright para E2E, sujeta a decisión; sin instalación actual.

| Nivel | Criterios / riesgo | Evidencia requerida |
|---|---|---|
| Unitarias | AC02, reglas de nombre/código/estado; permisos como política pura donde aplique | Casos normales, límites y errores |
| Integración SQL Server | AC01, AC03, AC05–09, AC13; índices, rowversion, rollback y carreras alta/archivo | DB aislada real, fixture sintética, resultados y limpieza autorizada |
| API | AC01–13; validación, códigos HTTP, ETag, paginación y contrato | Host de prueba, identidades sintéticas, checks OpenAPI |
| E2E | Alta cliente → alta proyecto → consulta/edición/archivo; lector denegado | Browser/API/DB aislados y evidencia reproducible |
| Arquitectura | Dependencias de capas y módulos sin ciclos; no acceso a DbContext ajeno | Análisis de referencias y reglas internas |

Matriz de seguridad: anónimo; usuario sin permiso; rol permitido en otra organización; miembro con proyecto restringido; gestor autorizado; cliente cruzado por ID; listados/conteos; alteración del OrgId enviado. Probar también dos escrituras concurrentes y dos altas con mismo código. Una prueba en memoria no acredita comportamiento de SQL Server.

Pipeline futuro: build backend; lint/typecheck/build frontend; unitarias/arquitectura; integración/API; E2E del flujo crítico; verificación de contrato, secretos y dependencias pertinente al riesgo. Sin credenciales persistidas ni SQL productivo. Publicar limitaciones si falta un nivel.

Salida de Etapa 4: AC01–13 aprobados o cambiados explícitamente por humano; no hallazgos críticos/altos abiertos; QA y code-reviewer PASS sobre mismo candidateHash; documentación/API actualizadas; aceptación humana. Una corrección cambia el hash y exige repetir verificaciones afectadas y ambos veredictos.
