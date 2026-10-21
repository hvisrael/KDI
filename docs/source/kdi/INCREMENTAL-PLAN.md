# Plan incremental y releases

| Etapa / incremento | Resultado | Puerta humana |
|---|---|---|
| 1 — actual | Plataforma documental y especificación KDI | Revisar entrega; no activar agentes |
| 2 — compatibilidad | Configuración y delegación comprobadas | Aprobar prueba inocua y su presupuesto |
| 3 — fundación / 0.1 candidato | Repositorio KDI independiente, backend/web, pruebas, identidad/organización mínima, persistencia y CI | Aprobar creación/escritura, herramientas, ADR y DB aislada |
| 4 — clientes/proyectos / 0.2 candidato | Primer recorrido funcional completo | Aprobar alcance/reglas y aceptar candidato verificado |
| 5A | Portafolio, demanda y requisitos | Priorizar y aceptar cada incremento |
| 5B | Trabajo técnico y estimaciones | Aprobar estados, unidades y conservación histórica |
| 5C | Scrum/Kanban, horas y capacidad | Aprobar reglas por equipo, periodo y calendario |
| 5D | QA, releases y deployments | Aprobar criterios de calidad y ambientes |
| 5E | Reportes, integraciones y notificaciones graduales | Aprobar métricas/contratos y efectos externos |

Versiones son hitos propuestos, no releases publicados. Integraciones, métricas y notificaciones pueden adelantarse solo con sus contratos fuente y prioridad humana.

## Fundación

Confirmar permisos y contenido existente del destino antes de crear repositorio. No sobrescribir archivos ni inicializar sobre un producto desconocido. Fijar SDK, versiones, gestor de paquetes y locks; restauración autorizada por fuentes oficiales. Registrar comandos reproducibles, ambientes y efectos. DB de desarrollo/pruebas separada; migraciones revisadas y aplicadas explícitamente. CI primero valida; CD queda condicionado a ambientes, secretos y aprobación, sin despliegue implícito.

## Entrega por incremento

Requisito → análisis funcional → diseño/ADR → implementación → pruebas → revisión → aprobación. Entregar trazabilidad, manifest/hash, OpenAPI, cambios de datos, guía de operación y notas de release. No declarar éxito por solo compilar. Estructura futura docs/releases/<version> incluye alcance, compatibilidad, migraciones, validaciones, riesgos y recuperación; nunca secretos.

Antes de desplegar: candidato aceptado, ambiente autorizado, migraciones ensayadas y procedimiento de recuperación validado. Recuperar DB no equivale a revertir binarios; documentar estrategia por cambio. No estimar fechas/costos de todo el producto sin tamaño y prioridades aprobados.
