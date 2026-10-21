# Criterios de cierre del incremento de identidades D2-A

La fase actual es preparación y verificación de solo lectura. Ninguna propuesta SQL ni parche preparado constituye ejecución o autorización de CREATE LOGIN. El candidato original conserva su manifiesto 66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f hasta autorizar el incremento concreto.

## Puerta de aprovisionamiento

Confirmar nuevamente servidor, modo mixto 0, ausencia de conflictos de nombres/SID y permisos de roles antes de cualquier mutación. Obtener autorización humana específica sobre las tres identidades y su matriz por base. Capturar contraseñas exclusivamente en una entrada interactiva local protegida, sin conversación, archivos, argumentos, historial, transcripción ni salida de herramientas. No realizar cambios globales ni habilitar modo mixto como corrección automática. Si una precondición cambió, detener y revisar.

## Evidencia requerida después de autorizar y aprovisionar

- Abrir conexiones reales distintas con cada identidad y registrar solo ORIGINAL_LOGIN(), SUSER_SNAME(), USER_NAME(), DB_NAME(), tipo de autenticación y resultados no secretos. No sustituir con EXECUTE AS.
- Comprobar ausencia de privilegios administrativos, membresías inesperadas y concesiones directas/indirectas. Inspeccionar CONNECT y permisos positivos/negativos concretos por identidad y base.
- Migrador: permisos efectivos acotados a los tres esquemas, prueba controlada bajo su propia autenticación y resultado comprobable; no ejecutar migraciones pendientes inesperadas ni conceder ALTER sobre dbo.
- Aplicación: CONNECT y metadatos mínimos para baseline; CREATE TABLE/VIEW/PROCEDURE/FUNCTION/SCHEMA y ALTER de esquemas denegados; sin DML anticipado sobre tablas de negocio. Una negativa de SELECT no acredita negativa de INSERT: comprobar cada operación por separado y validar el error esperado.
- Test runner: acceso de lectura en Test, ausencia de modificación en Dev. Si la propuesta elimina acceso Dev, acreditar fallo real de conexión allí y ausencia de mapeo/guest/privilegios alternativos.
- Pruebas mutantes de autorización solo sobre objetos probe de esta ejecución, con reversión/limpieza comprobada; nunca sobre objetos preexistentes ni sobre IDI. No confundir ausencia de objeto con denegación de permisos.
- API: arrancar con kdi_app real y sin migración automática, verificar readiness SQL 200, liveness y smoke conectado, registrar endpoints/status sin credenciales. Cerrar procesos y descartar credenciales de memoria al finalizar.
- Regresión: pruebas relevantes de validación de destino/autenticación/privilegios/readiness, build y suites afectadas. Actualizar el contrato de configuración solo dentro del incremento autorizado; conservar el manifiesto original como evidencia histórica.

## Congelación y aceptación

Después de terminar el incremento y sus pruebas, congelar un nuevo candidato con inventario y SHA-256. QA y Code Review separados del autor evalúan el mismo hash, sin modificaciones. Cualquier cambio invalida ambas revisiones. No declarar PASS si falta autenticación real, readiness 200, smoke conectado o pruebas críticas. Solicitar aceptación humana D2-A al completar; no iniciar D2-B/C ni desplegar.
