# Dependencias y riesgos A1

Actualización de recuperación: mecanismo SqlCredential/SecureString solo en memoria y procesos D2-A finalizados, documentados en RECOVERY-RESULT.md. Sin variables ni módulos de almacén de secretos disponibles en contexto sandbox inspeccionado; no se examinaron secretos del perfil Israel. El fallback exacto está en RECOVERY-LOCAL-WINDOWS.md y requiere sesión local ya autenticada con kdi_test_runner/Test, sin contraseñas nuevas ni conexión administradora.

Fecha: 2026-10-21T00:34:11.5786286Z. Candidato 190/a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102 verificado. Script aprobado 60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c, sin ejecutar. Operador Windows detectado: DESKTOP-F25O3MF\CodexSandboxOffline. Destino efectivo SQL no verificado.

| Dependencia | Evidencia / estado |
|---|---|
| Python y verificadores de manifiesto | Ejecutados exit 0; logs adjuntos |
| SQLCMD compatible con :ON ERROR EXIT | SQLCMD.EXE instalado en Client SDK ODBC/170; no se invocó ni certificó conectividad |
| Operador existente autorizado | BLOQUEO principal; identidad proceso no basta para establecerlo |
| SQL Server, versión/edición/compatibility/recovery | UNKNOWN, script no ejecutado |
| Visibilidad esquemas e historias identity/organizations/portfolio | UNKNOWN; ausencia o NULL futuros no se reinterpretarán como inexistencia válida |
| Ocho tablas D2-B y baseline | Inventario propuesto documentado en PLAN; presencia real y drift no verificados |
| Bloqueos/concurrencia | No medidos; SELECT de metadatos puede esperar locks; no se han iniciado sesiones |
| Backup y recuperación | Ruta/espacio/operador/permisos/retención sin comprobar; fuera del script A1 |

Persisten riesgos del harness ya identificados estáticamente: dos baseline tests tocan Portfolio/reverse a cero y quedan excluidos; cleanup de auditoría user-state incompleto, cancelación y recuperación por GUID pendientes; clases acceso/administración requieren ejecución secuencial y limpieza verificada. No se reprodujeron ni corrigieron mediante SQL en A1.

Auth0, HTTPS/keyring, migraciones, permisos, fixtures, backup y pruebas negativas siguen fuera de alcance A1. No se consultaron secretos ni se abrieron configuraciones privadas; ninguna de las cinco variables de conexión enumeradas por nombre estaba presente. No se presupone que esto descarte todos los mecanismos privados disponibles.
