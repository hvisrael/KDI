# Visión funcional de KDI

KDI, Kulani Delivery Intelligence, permitirá a una organización conservar la trazabilidad entre inversión, demanda, ejecución y entrega de software. Será un producto empresarial independiente, con información operativa y ejecutiva consistente.

## Actores propuestos

Dirección/PMO prioriza portafolio y observa resultados; responsables de proyecto planifican alcance y capacidad; analistas gestionan demanda y requisitos; equipos técnicos ejecutan trabajo; QA registra verificación; responsables de entrega gestionan releases y deployments; administradores gestionan identidad y alcance; colaboradores registran tiempo. Los nombres de roles y sus permisos deben ratificarse; un cargo no concede acceso global automáticamente.

## Capacidades solicitadas

Gestión de organizaciones/clientes, portafolio/proyectos, requerimientos y evolutivos, trabajo técnico, Scrum/Kanban, estimaciones, horas y capacidad, QA, releases/deployments y reportes ejecutivos. Integraciones y notificaciones se incorporarán gradualmente, con límites propios y sin acoplarse al motor de agentes.

## Valor y trazabilidad

Cadena objetivo: organización → cliente → proyecto → demanda/requisito → trabajo técnico → planificación → esfuerzo/tiempo → evidencia QA → release → deployment. No todas las relaciones serán obligatorias desde el primer incremento; cada incorporación debe preservar IDs, historial y permisos.

Métricas candidatas: avance por criterios cumplidos, demanda pendiente, esfuerzo estimado frente a registrado, carga disponible, defectos y estabilidad de entregas. Definir fórmula, ventana temporal, fuente y granularidad antes de publicar cada indicador. No confundir horas humanas registradas con consumo de IA ni comparar story points entre equipos sin acuerdo.

## Primer resultado verificable

Un usuario autorizado puede administrar clientes y proyectos dentro de su organización y alcance, con validaciones, auditoría y pruebas de aislamiento. El producto debe completar el recorrido UI → API → persistencia → consulta sin depender de AI-ENGINEERING en ejecución.

## Fuera del primer incremento

Motor de agentes, copia/migración de IDI, sincronización con productos externos, despliegues automáticos a infraestructura real, carga de datos reales, facturación y nómina. No implica descartar capacidades futuras; requieren análisis y autorización propios.
