# D2-A: matriz de privilegios e identidades

Misma configuracion en KulaniKDI_Dev y KulaniKDI_Test. Roles propietarios dbo, sin membresias en roles administrativos. Ninguna identidad runtime es sysadmin, dbcreator ni db_owner. Las identidades reales pendientes deben verificarse bajo su propio login antes de activar persistencia API.

| Rol | CONNECT | DDL | Datos baseline | Testigo WITHOUT LOGIN |
| --- | --- | --- | --- | --- |
| kdi_migrations | GRANT | CREATE TABLE en DB; ALTER/REFERENCES en identity, organizations, portfolio | SELECT/INSERT/UPDATE/DELETE y VIEW DEFINITION en sus tres schemas | kdi_migration_witness |
| kdi_application | GRANT | DENY CREATE TABLE/VIEW/PROCEDURE/FUNCTION/SCHEMA; DENY ALTER en tres schemas | Ningun GRANT DML: D2-A no tiene tablas de negocio | kdi_application_witness |
| kdi_tests | GRANT | Sin concesion | SELECT en los tres schemas; no escritura | kdi_test_witness |

El permiso CREATE TABLE del migrador no concede ALTER sobre dbo ni otros schemas. El rol migrador no tiene CREATE SCHEMA; los schemas se aprovisionan administrativamente. Los historiales quedan bajo su schema, no bajo dbo. Los testigos no tienen login y no pueden autenticar una conexion desde API o herramienta externa. Los permisos efectivos se consultan en02 y se ejercitan en03 y en la suite SQL; los diarios del principal conservan resultados.

Incremento de identidades SQL autorizado por Human Orchestrator: kdi_migrator se vincula a kdi_migrations en Dev/Test; kdi_app a kdi_application en Dev/Test; kdi_test_runner a kdi_tests solo en Test, sin usuario Dev. Se conservan los roles y permisos anteriores; ningun GRANT nuevo ni membresia administrativa. Tres CREATE LOGIN con CHECK_POLICY=ON y CHECK_EXPIRATION=ON; cinco CREATE USER y cinco ALTER ROLE ADD MEMBER. Estado de ejecucion y evidencia en SQL-IDENTITIES-PHASE2.md y paquete de fase2; no confundir propuesta con operaciones confirmadas.

La API admite el camino Windows integrado preexistente y un opt-in SQL exclusivo kdi_app mediante SqlCredential/SecureString fuera de la cadena de conexion. Identidades migrador/pruebas no se aceptan como credencial runtime de API. No secretos en appsettings, archivos, entorno, argumentos ni logs. Entrada local protegida directa al proceso. Persisten guardas de destino/cifrado, denegaciones DDL y rechazo de privilegios administrativos. No migracion al arranque. La excepcion de certificado de desarrollo local no cambia configuracion global.

Comprobar autenticacion con conexiones reales por identidad, no EXECUTE AS. El runner debe autenticar Test y fallar Dev. Registrar permisos efectivos positivos/negativos y lecturas autorizadas; no ejecutar probes DDL/DML adicionales fuera de la autorizacion humana exacta. CHECK_EXPIRATION requiere rotacion de contrasenas supervisada. El nuevo candidato se revisa separadamente; el manifiesto D2-A original se conserva intacto en el workspace principal.

Separacion modular: una identidad de aplicacion compartida no es una frontera de seguridad entre schemas; D2-A establece propiedad mediante contextos y arquitectura. Futuras tablas B/C requieren concesiones DML explicitas revisadas; esta entrega no las concede anticipadamente a runtime.
