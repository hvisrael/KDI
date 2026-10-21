# D2-B — autorización y ejecución

Fuente humana: «Implement the plan», posterior a la propuesta D2-B ratificada mediante preguntas en esta conversación. Autoriza desarrollar la implementación en candidato separado con Auth0 Free, piloto local HTTPS, administración local auditada, 30 minutos idle/1 hora absoluta, auditoría30 días/sesiones7 días y presupuesto propuesto47–72 horas-agente. No constituye aceptación del resultado.

Base exacta: candidato D2-A c31fe8c14872ff88b6d0d3882f4585b36e6a3798788108ecdd8539841a312973; copiados y comprobados123 archivos a .local/d2b-candidate. D1 y D2-A aprobados permanecen intactos. No se trabaja sobre el src raíz anterior. Nuevos artefactos/evidencia fuera de los congelados.

El usuario confirmó después que tenant/aplicación/cuentas Auth0 todavía no están preparados. Pruebas reales OIDC BLOCKED por esa dependencia; dobles y contratos browser no acreditan interoperabilidad real. No se registran aplicaciones, crean cuentas externas/reales ni contratan planes.

La propuesta conserva puertas específicas para infraestructura y ejecución: certificados/confianza/secretos operacionales, migraciones SQL de objetos concretos, GRANT por tabla y fixtures/probes deben estar preparados/revisados y autorizados específicamente antes de ejecutarlos. La implementación genera herramientas y SQL revisables; ninguna operación SQL real se presupone ejecutada por generar un archivo. Ninguna migración al arranque.

Destinos de SQL propuestos exclusivamente DESKTOP-F25O3MF MSSQLSERVER/KulaniKDI_Test primero y KulaniKDI_Dev después de validación autorizada. No logins nuevos; kdi_migrator para operador/migraciones, kdi_app runtime, kdi_test_runner lectura Test y sin Dev. No roles administrativos, GRANT por esquema a runtime, IDI, otras bases, cambios globales, D2-C ni despliegue.

Delegaciones reales: /root/d2b_backend (engineer), API/BFF y pruebas; /root/d2b_data (dba-engineer), modelo/persistencia/migraciones/CLI y pruebas. Principal posee puertos/DTO compartidos, frontend e integración. Máximo dos especialistas activos; QA y Code Review separados tras congelación, sin modificar candidato.
