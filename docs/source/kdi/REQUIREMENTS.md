# Análisis funcional inicial

Estado: propuesta para revisión humana, separada del diseño técnico. Fuente: decisión del usuario; reglas detalladas siguientes son propuestas propias de KDI.

## Glosario y alcance

Organización: límite propuesto de pertenencia y acceso. Cliente: entidad atendida por una organización. Proyecto: iniciativa de entrega bajo una organización, asociada inicialmente a un cliente. Portafolio: agrupación futura de iniciativas. Requerimiento: necesidad versionada. Evolutivo: cambio solicitado sobre una capacidad existente. Trabajo técnico: unidad ejecutable vinculable a demanda. Release: conjunto versionado de entregables; deployment: ejecución de entrega en un ambiente.

Pendiente: ratificar si existen proyectos internos sin cliente, clientes compartidos entre organizaciones o pertenencia de un usuario a varias organizaciones. El piloto propone cliente obligatorio y datos segregados por organización; no implementar antes de aprobar esas reglas.

## Reglas propuestas para clientes y proyectos

| ID | Regla |
|---|---|
| KDI-R01 | Todo cliente y proyecto pertenece a una organización; el servidor valida el alcance efectivo. |
| KDI-R02 | Nombre obligatorio, sin espacios extremos; código obligatorio y único dentro de la organización, normalizado sin distinguir mayúsculas. Límites propuestos: código 32, nombre 200 caracteres. |
| KDI-R03 | Un proyecto referencia un cliente de la misma organización; rechazar referencias cruzadas aunque el ID exista. |
| KDI-R04 | Estados iniciales propuestos: Active/Archived. Archivar un cliente con proyectos activos se rechaza hasta resolverlos; no hay borrado físico en el primer incremento. |
| KDI-R05 | Actualizar/archivar requiere versión vigente; no sobrescribir silenciosamente una edición concurrente. |
| KDI-R06 | Las acciones de lectura, creación, edición y archivo requieren permiso y alcance de organización/proyecto. Un lector no obtiene escritura por pertenecer a la organización. |
| KDI-R07 | Las mutaciones registran actor, instante UTC, acción y recurso, evitando secretos en auditoría. |
| KDI-R08 | Listados paginados y con orden estable; búsqueda por código/nombre y filtro de estado. No retornar datos de otra organización ni conteos que los revelen. |

## Casos de uso y criterios

| Caso | Actor lógico | Criterios verificables |
|---|---|---|
| KDI-UC01 Alta de cliente | Gestor de clientes | AC01: persiste y puede consultarse; AC02: nombre/código inválidos se rechazan sin escritura; AC03: duplicado concurrente no crea dos filas. |
| KDI-UC02 Consulta/edición/archivo de cliente | Lector / gestor | AC04: lectura paginada y filtrada; AC05: edición concurrente falla con conflicto; AC06: archivo con proyectos activos se rechaza. |
| KDI-UC03 Alta de proyecto | Gestor de proyectos | AC07: cliente válido de la organización permite alta; AC08: cliente ajeno/inexistente/archivado se rechaza sin filtrar información ajena. |
| KDI-UC04 Consulta/edición/archivo de proyecto | Lector / gestor | AC09: cambio válido y archivo conservan identidad e historial; AC10: recurso fuera de alcance no se puede leer ni modificar por ID directo. |
| KDI-UC05 Seguridad y auditoría transversal | Administrador / auditor autorizado | AC11: sin autenticación no hay acceso protegido; AC12: rol insuficiente o alcance inválido deniega; AC13: cada mutación exitosa produce auditoría consistente. |

AC01–AC13 son críticos para el primer incremento. Añadir matriz rol×acción×alcance antes de implementación. Propuesta: permisos `clients.read/manage`, `projects.read/manage`, `audit.read`; asignaciones a organización y, para proyectos, alcance adicional explícito. Decidir cómo se concede alta de proyectos y lectura de clientes a miembros con alcance restringido.

## Recorrido UI propuesto

Seleccionar contexto autorizado → lista de clientes → crear/editar/archivar → lista de proyectos → crear seleccionando cliente permitido → detalle/edición/archivo. Mostrar carga, vacío, error, validación y conflicto concurrente con recuperación sin perder entrada. Confirmar archivo, permitir navegación por teclado y no presentar éxito antes de confirmación del servidor.

## Atributos pendientes de cuantificar

Volumen de organizaciones/proyectos, usuarios concurrentes, latencia p95, disponibilidad, RPO/RTO, retención/auditoría, privacidad, accesibilidad y localización. No inventar SLA como compromiso aprobado. Registrar objetivos medibles antes del diseño operativo final.
