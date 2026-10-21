# Análisis funcional de fundación

Estado: preparado por el principal, pendiente de revisión humana. Separado del diseño y sin implementación de clientes/proyectos.

La fundación debe permitir desarrollar y verificar KDI independientemente de AI-ENGINEERING. Sus consumidores iniciales son desarrolladores y responsables de validación. El resultado esperado es una solución reproducible, una aplicación web base, límites comprobables, contrato HTTP y evidencia de calidad. La identidad y pertenencia mínimas se incorporan solo tras acordar su autoridad y aislamiento.

| Necesidad | Criterio de fundación |
|---|---|
| Independencia | Build y ejecución no leen AI-ENGINEERING ni otro producto |
| Reproducibilidad | SDK, paquetes y fuentes fijados; locks y comandos registrados |
| Salud operativa | Endpoint de liveness sin datos de negocio; readiness diferenciada al incorporar SQL |
| Límites | Domain/Contracts independientes; capas y módulos sin ciclos |
| Acceso | Denegar acceso de negocio por defecto; ninguna identidad ficticia habilitada en producción |
| Datos | Fixtures sintéticas en DB aislada autorizada; no migraciones al arrancar |
| Evidencia | Fallos/omisiones visibles; revisión independiente del mismo candidato |

No forman parte de esta entrega los recorridos KDI-UC01–05 ni las tablas de módulos futuros. AC01–13 siguen como propuestas del primer incremento funcional. No se deducen SLA, presupuesto, retención ni reglas empresariales por silencio.

Decisiones funcionales antes de identidad/esquema: pertenencia a varias organizaciones, autoridad de altas organizacionales, permisos de alta de proyectos para miembros restringidos y matriz rol/acción/alcance. Antes de Etapa 4: cliente obligatorio, normalización y reutilización de códigos, archivo y política uniforme de recursos no visibles. Las fuentes históricas detallan estas propuestas sin ratificación.
