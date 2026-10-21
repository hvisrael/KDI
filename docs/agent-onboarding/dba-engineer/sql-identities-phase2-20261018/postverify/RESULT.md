# TaskResult D2A-P2-POSTVERIFY

KDI / phase2 / attempt3; agente efectivo `dba-engineer`, identificador `/root/d2a_phase2_sql_executor`. Alcance: complemento de lectura autorizado, sin contraseñas, GUI, nuevos logins, usuarios, permisos ni objetos persistentes. No se reejecutó provisioning ni se modificaron sql/, sql-v2/ o D2-A.

## Resultado acotado

PASS de autenticación y matriz observada, usando conjuntamente el journal original y este suplemento. No representa PASS integral D2-A, de API, smoke, revisión independiente o pruebas de DDL/DML, que corresponden al principal.

El journal original `../sql-v2/execution-20261019T031842176Z.json` registra exactamente 3 CREATE LOGIN, 5 CREATE USER y 5 ALTER ROLE confirmados, cinco conexiones SQL directas con authentication_type INSTANCE y login/usuario esperados, y rechazo del runner en Dev con error4060. SHA-256 original `eee6da6e5006d23d94927f500287be5fecead323727b94a9563e2e7b3ec7784d`. Se conserva su resultado histórico FAIL_PERMISSION_MATRIX. `journal-summary.json` resume sin reinterpretar ni reescribir esos eventos. Los únicos mismatches de las cinco observaciones son los tres serverperm NULL; los valores restantes de la matriz coincidieron con lo esperado.

El defecto pertenece a la consulta de verificación: HAS_PERMS_BY_NAME(NULL,'SERVER',permission) utiliza una clase incorrecta para comprobar servidor. La documentación oficial muestra `HAS_PERMS_BY_NAME(NULL,NULL,permission)` para servidor y distingue 0 de NULL. Fuente consultada: [Microsoft Learn, HAS_PERMS_BY_NAME](https://learn.microsoft.com/en-us/sql/t-sql/functions/has-perms-by-name-transact-sql?view=sql-server-2016). NULL no se interpretó como denegación.

`server-readonly.sql` se ejecutó exitosamente por sqlcmd Windows admin en master (`server-readonly-elevated.txt`, exit0). Para cada uno de los tres logins, EXECUTE AS LOGIN local a la sesión mostró la consulta anterior NULL y la firma correcta 0 para CONTROL SERVER, ALTER ANY LOGIN y ALTER ANY SERVER ROLE. REVERT restableció el administrador Windows. Esta es evidencia explícita de permisos efectivos bajo impersonación, NO nueva autenticación independiente; esta última ya consta en el journal.

`database-readonly.sql` se ejecutó exitosamente por sqlcmd Windows admin en master y ambas bases autorizadas (`database-readonly.txt`, exit0). Acredita los tres logins habilitados, CHECK_POLICY y CHECK_EXPIRATION ON, sin membresías en roles de servidor; SID de los cinco usuarios idéntico al login respectivo, authentication_type INSTANCE, exactamente los roles autorizados, y ningún usuario runner en Dev. Registra permisos de identidades, roles y public. CONNECT SQL/CONNECT observados directamente en principales son permisos generados por CREATE LOGIN/CREATE USER por el motor; este suplemento no ejecuta sentencias GRANT ni añade permisos. No hay permiso administrativo concedido a app/runner.

## Ejecuciones y límites

La primera conexión sandbox a master falló SSL/SSPI antes de consultar (`server-readonly.txt`, exit1); el reintento escalado autorizado completó las mismas consultas. Se conserva el fallo y el éxito, sin ocultar la incidencia. La segunda consulta se ejecutó escalada después del fallo de conexión ya acreditado. Sin cambio global de SQL/TLS/firewall/Windows, sin IDI y sin datos de negocio.

Los permisos efectivos son observación actual; no prueban que una operación DDL/DML real haya sido intentada y rechazada. Ninguna prueba DDL/DML adicional se ejecutó porque esta tarea autoriza solo lectura y no las incluye en las 13 operaciones persistentes. No reutilizar el helper v2 para volver a aprovisionar: conserva el defecto diagnóstico histórico y los nombres ya existen. Cualquier nueva herramienta exige revisión nueva.
