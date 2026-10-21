# D2-A: matriz de privilegios e identidades

Misma configuracion en KulaniKDI_Dev y KulaniKDI_Test. Roles propietarios dbo, sin membresias en roles administrativos. Ninguna identidad runtime es sysadmin, dbcreator ni db_owner. Las identidades reales pendientes deben verificarse bajo su propio login antes de activar persistencia API.

| Rol | CONNECT | DDL | Datos baseline | Testigo WITHOUT LOGIN |
| --- | --- | --- | --- | --- |
| kdi_migrations | GRANT | CREATE TABLE en DB; ALTER/REFERENCES en identity, organizations, portfolio | SELECT/INSERT/UPDATE/DELETE y VIEW DEFINITION en sus tres schemas | kdi_migration_witness |
| kdi_application | GRANT | DENY CREATE TABLE/VIEW/PROCEDURE/FUNCTION/SCHEMA; DENY ALTER en tres schemas | Ningun GRANT DML: D2-A no tiene tablas de negocio | kdi_application_witness |
| kdi_tests | GRANT | Sin concesion | SELECT en los tres schemas; no escritura | kdi_test_witness |

El permiso CREATE TABLE del migrador no concede ALTER sobre dbo ni otros schemas. El rol migrador no tiene CREATE SCHEMA; los schemas se aprovisionan administrativamente. Los historiales quedan bajo su schema, no bajo dbo. Los testigos no tienen login y no pueden autenticar una conexion desde API o herramienta externa. Los permisos efectivos se consultan en02 y se ejercitan en03 y en la suite SQL; los diarios del principal conservan resultados.

No se inventan nuevas identidades Windows. Alternativa operativa: Human Orchestrator suministra tres identidades Windows existentes y distintas (migrador, aplicacion, pruebas); administrador valida sus logins/grupos, crea usuarios de DB asociados a esos logins exclusivamente en ambas DB y agrega cada usuario a su rol correspondiente. Si no existen logins, el administrador debe evaluar y autorizar su alta Windows en SQL Server. Crear cuentas del sistema o cambiar politicas Windows requiere otra decision y no se automatiza aqui. Autenticacion SQL no se propone.

El binding pendiente debe comprobar pertenencia indirecta por grupos y ausencia de privilegios de servidor/base, no solo roles asignados directamente. Ejecutar las pruebas desde las tres identidades reales y API desde la de aplicacion. Las comprobaciones de impersonacion no acreditan estos pasos. No se entrega un script con nombres supuestos ni se vincula la sesion sysadmin al rol runtime.

Separacion modular: una identidad de aplicacion compartida no es una frontera de seguridad entre schemas; D2-A establece propiedad mediante contextos y arquitectura. Futuras tablas B/C requieren concesiones DML explicitas revisadas; esta entrega no las concede anticipadamente a runtime.
