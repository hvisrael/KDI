# Módulos lógicos y dependencias propuestas

Son hipótesis de bounded contexts, no límites definitivos aprobados. Las flechas de dependencia son `consumidor → proveedor`. Un módulo no escribe tablas de otro. Los proveedores no importan al consumidor. Identity valida claims/permisos; Organizations conserva pertenencias empresariales y entrega ese contexto por contrato al borde de aplicación, evitando un ciclo Identity↔Organizations.

| Módulo | Posee | Proveedores lógicos |
|---|---|---|
| Identity & Access | Identidades locales, roles, permisos y concesiones de acceso; integración IdP | Ninguno; IDs de alcance opacos |
| Organizations & Clients | Organizaciones, miembros empresariales, clientes | Identity & Access |
| Portfolio & Projects | Portafolios, proyectos y su vínculo de cliente | Organizations & Clients |
| Requirements & Intake | Demanda, requisitos/evolutivos versionados y aceptación | Portfolio & Projects |
| Work Management | Trabajo técnico, estados, dependencias y estimaciones versionadas | Requirements & Intake; Portfolio & Projects |
| Agile Planning | Equipos de planificación, sprints, tableros y asignación a planificación | Work Management |
| Time Tracking | Registros y correcciones/aprobación de horas | Work Management |
| Capacity Management | Calendarios, disponibilidad, asignaciones y proyecciones | Agile Planning; Time Tracking |
| Quality Assurance | Casos, ejecuciones, evidencias y defectos QA | Work Management; Requirements & Intake |
| Releases & Deployments | Contenido de release, ambientes y ejecuciones de entrega | Quality Assurance; Work Management |
| Reporting & Analytics | Modelos de lectura, fórmulas y snapshots | Eventos/contratos de módulos empresariales |
| Integrations | Mapeos externos, adaptadores y estado de sincronización | Fachadas públicas de módulos habilitados |
| Notifications | Preferencias, plantillas y estado de entrega | Eventos públicos de módulos habilitados |

Identity & Access se aplica transversalmente en bordes de casos de uso; Domain no llama al IdP. Reporting, Integrations y Notifications son consumidores terminales: los módulos fuente no importan sus implementaciones. Si un caso requiere iniciar integración, publica un evento neutral; el adaptador consume y llama contratos autorizados. Cualquier extensión debe volver a comprobar el grafo.

## Decisiones de frontera

- Work Management posee estado de ejecución; Agile Planning posee ubicación en sprint/tablero. La pertenencia a sprint no se duplica como dueño en Work.
- QA posee defecto y evidencia; un trabajo de corrección se crea en Work por contrato, sin dependencia Work→QA. La correlación conserva ambos IDs.
- Portfolio posee proyectos y cliente referenciado; Organizations valida elegibilidad de cliente. Auditoría de cada mutación pertenece al módulo que la realiza, con lectura de auditoría autorizada.
- Archivo de cliente con proyectos activos: `Application/Coordination/ClientLifecycle` depende de las fachadas de Organizations y Portfolio y coordina la regla/atomicidad; no se agrega dependencia inversa Organizations→Portfolio. Este componente superior no posee entidades ni admite escritura ajena arbitraria.
- Capacity calcula disponibilidad sin convertirse en autoridad de horas registradas ni estimación del trabajo.
- Release no sobrescribe requisitos ni resultados QA históricos; conserva referencias/versiones de la evidencia aceptada.

Posibles extracciones futuras: separar organizaciones y clientes; separar portafolio y proyecto; separar releases y deployments. Decidir por invariantes, carga y evolución, no por conteo de carpetas. Pruebas de arquitectura deben rechazar ciclos y acceso a internals ajenos.
