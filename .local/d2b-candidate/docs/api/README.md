# Contrato HTTP de fundación D1

openapi-v1.json fue obtenido del host Release mediante tools/smoke-api.py. El endpoint /openapi/v1.json publica el contrato generado por ASP.NET Core. Solo se implementan:

| Método/ruta | Respuesta 200 |
|---|---|
| GET /health/live | {"status":"healthy"}; acredita vida del proceso, no DB/readiness |
| GET /api/v1/system/info | {"product":"Kulani Delivery Intelligence","apiVersion":"v1"} |

Son rutas públicas sin información de usuario/ambiente. Las rutas de negocio no existen. Una ruta desconocida retorna 404; método incorrecto sobre liveness retorna 405. Los errores negociados como JSON usan application/problem+json, status, title, code estable y correlationId, sin detalle de excepción. Los códigos actuales son not_found, method_not_allowed y request_failed.

Todas las respuestas incluyen Cache-Control: no-store y X-Correlation-ID. Se conserva un identificador enviado si es único, de 1 a 64 caracteres ASCII alfanuméricos o . _ -; cualquier otra entrada se sustituye por un GUID sin guiones. No se confía en ese identificador para autenticación ni autorización. No se agrega cabecera Server.

Los contratos empresariales de docs/source/kdi/API.md siguen siendo propuestas de Etapa 4. No hay autenticación simulada ni permisos de negocio concedidos por esta API.
