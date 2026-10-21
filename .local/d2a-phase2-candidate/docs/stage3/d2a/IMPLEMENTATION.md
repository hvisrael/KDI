# D2-A: implementacion de persistencia

TaskInput: autorizacion humana completa D2-A de 2026-10-18; delegacion del principal a `/root/d2a_engineer`, perfil efectivo `engineer`. Presupuesto operativo asignado por principal: hasta 45 minutos para primera implementacion. Fuentes: AGENTS.md, `.codex/agents/engineer.toml`, `docs/stage3/d2-proposal/PROPOSAL.md`, `TECHNICAL-DESIGN.md`, `ARCHITECTURE-DECISIONS.md` y autorizacion actual. Esta autorizacion sustituye la incertidumbre de infraestructura de la propuesta para los dos destinos expresamente nombrados.

## Candidato

EF Core SqlServer/Design 10.0.13, sobre el .NET 10 existente, SQL Server 2019 y compatibility level 150. No cambio de stack. El restore inicial detecto NU1902/NU1903 en `System.Security.Cryptography.Pkcs` 9.0.11 transitivo; referencia explicita 10.0.13 corrige la resolucion sin suprimir auditoria. Fuentes oficiales de compatibilidad registradas por principal en `evidence/compatibility-sources.md`.

Tres DbContexts vacios, propietarios de `identity`, `organizations`, `portfolio`. Cada uno tiene baseline `20261018000100_Initial{Modulo}`, snapshot y tabla `__EFMigrationsHistory` en su propio esquema. No entidades, tablas, endpoints ni UI de negocio B/C. Los schemas son provisionados por operador; la migracion inicial comprueba su existencia. `Down` no elimina schemas ni tablas: solamente EF elimina su fila de historial. Los schemas e historiales son infraestructura compartida conservada deliberadamente.

`MigrationContexts` es una ruta explicita para operador/design time, separada del registro runtime. Las factories usan `KDI_MIGRATION_CONNECTION`. El generador `tools/Kulani.KDI.Migrations` produce SQL idempotente con guardas exactas de host/DB sin abrir conexiones. No instala herramienta global y no ejecuta SQL. Baselines independientes, sin dependencia cruzada ni orden funcional entre modulos.

## Conexion runtime

Configurar `ConnectionStrings__Kdi` mediante entorno de la identidad Windows de aplicacion. Ejemplo sin secretos: `Server=DESKTOP-F25O3MF;Database=KulaniKDI_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true`. `TrustServerCertificate=true` solo permite el certificado local del entorno de desarrollo autorizado; es configurable y no cambia politica global. No configurar esta variable bajo la sesion administrativa para uso normal de API. La ausencia de configuracion mantiene liveness y metadata disponibles, con readiness 503.

`SqlTarget` exige el nombre exacto de servidor predeterminado, uno de los dos nombres de DB, Windows integrada, cifrado y ausencia de credenciales SQL, archivos adjuntos, failover partner y persistencia de informacion sensible. El interceptor EF verifica en cada apertura el servidor/DB real, pertenencia a `kdi_application`, ausencia de sysadmin/dbcreator/db_owner/CONTROL y ausencia de permisos DDL en la DB y los tres schemas. Rechaza y cierra la conexion antes de entregar su uso a la aplicacion. No imprime cadena ni excepcion SQL en la respuesta publica.

`/health/ready` devuelve solamente `{"status":"ready"}` con 200 al abrir/cerrar satisfactoriamente esa conexion, o `{"status":"not_ready"}` con 503. Este chequeo acredita conectividad e identidad runtime; no pretende validar la version completa de esquema ni reemplaza las verificaciones de migracion. `/health/live` conserva su contrato y no conecta SQL. No se llama Migrate/EnsureCreated al inicio de la API.

## Operacion revisable

Todos los comandos se ejecutan desde la raiz KDI. El principal es responsable de ejecucion SQL real y evidencia; el engineer no abrio conexiones ni ejecuto DDL.

1. `01-provision.sql`: una sola vez, con Windows admin y contexto master. Comprueba ambas DB ausentes antes de crearlas; usa rutas predeterminadas. Configura nivel150, recovery SIMPLE, AUTO_CLOSE/OFF y AUTO_SHRINK/OFF. Si falla parcialmente, detenerse: no reejecutar ni reparar automaticamente; evaluar y obtener autorizacion adicional para cualquier objeto preexistente.
2. `02-verify.sql`: lectura por cada DB, configuracion, rutas, schemas, ownership, roles, permisos y membresias.
3. Generar cada destino: `dotnet run --project tools/Kulani.KDI.Migrations -c Release -- KulaniKDI_Test docs/stage3/d2a/sql/generated` (repetir para Dev). Revisar y congelar los seis archivos generados.
4. `04-migrate-KulaniKDI_{Dev|Test}.sql`: wrappers de evidencia del rol migrador mediante `EXECUTE AS USER` y `:r` de los tres scripts. Canal Windows admin, autoridad efectiva de DDL limitada al testigo migrador. Repetir para comprobar idempotencia. Estos wrappers no sustituyen autenticacion independiente del migrador Windows.
5. `03-permissions-and-rollback.sql`: solo Test, probes sinteticos por transaccion, ROLLBACK antes REVERT incluso ante error. Nunca elimina objetos preexistentes.
6. `05-assert-baseline.sql`: ambas DB; exactamente tres historiales con una fila esperada cada uno, sin tablas de negocio/probes, propietario dbo. Solo Test revierte temporalmente las filas de historial bajo transaccion y confirma restauracion tras rollback. No ejecuta downgrade permanente.

Capturar SQL con `sqlcmd -S DESKTOP-F25O3MF -E -d <nombre-autorizado> -l 10 -b -r 1 -e -i <script> -o <evidencia>`. `-e` registra DDL exacto, resultados y errores; los scripts incorporan `:ON ERROR EXIT`. No cambiar servidor, rutas de instancia ni configuracion global.

## Pruebas

Pruebas unitarias adicionales: guardas de configuracion/destino, aislamiento de esquema/historial, modelo sin entidades, una migracion por modulo, ausencia de drift y Down no destructivo. API cubre readiness sin configuracion y OpenAPI de las tres operaciones Foundation. Arquitectura resuelve referencias transitivas desde la salida exacta de API para mantener analisis semantico tras introducir EF.

`tests/Kulani.KDI.SqlIntegrationTests` requiere `KDI_SQL_TEST_CONNECTION` Windows integrada contra Test. Su ausencia falla expresamente con BLOCKED; no se omiten casos ni se usan SQLite/InMemory. Es un harness administrativo: impersona `kdi_migration_witness`, ejecuta EF Migrate dos veces por modulo, verifica pendientes cero e historial estable, ejecuta el reverse generado por IMigrator dentro de transaccion y verifica rollback. Comprueba lectura/no escritura del testigo de pruebas y rechazo de la conexion sysadmin por el interceptor runtime. No acredita login de identidades Windows independientes.

Comando de suite completa: `dotnet test --solution Kulani.KDI.slnx -c Release --no-restore`, con el entorno SQL explicito anterior para la parte SQL. Los runners HTTP, arquitectura y Playwright escriben exclusivamente evidencia D2-A y conservan los artefactos D1.

## TaskResult del engineer

Correccion previa al congelado: la primera suite SQL del principal obtuvo55/56 por IDs manuales de12 digitos, incompatibles con resolucion de nombres EF. Se corrigieron artefactos a IDs canonicos14 digitos (`20261018000100_...`) y se agrego asercion `IMigrationsIdGenerator.IsValidId` para impedir regresion. El principal autorizo corregir exclusivamente las tres filas de historial creadas durante esta sesion en cada DB, cuya ausencia inicial esta registrada. `06-correct-session-baseline-ids.sql` exige host/DB, tres tablas y exactamente las tres filas originales con version10.0.13; bloquea historiales, actualiza una fila por modulo en transaccion y revierte todo ante discrepancia. No recrea ni elimina schemas/tablas. Es una correccion de esta sesion, no un paso de instalacion nuevo ni una reparacion generica. Ejecutar06 antes de repetir los scripts regenerados; estos ahora rechazan cualquier historial desconocido. Conservar primer fallo y evidencia de correccion; repetir suite y verificaciones antes de congelar.

Cambios: infraestructura EF y baselines; guardas y registro runtime; readiness; generador offline; scripts de aprovisionamiento/migracion/verificacion/permisos/rollback; pruebas unitarias/API/SQL y ajuste semantico de arquitectura; rutas de evidencia smoke D2-A.

Verificaciones propias ejecutadas antes del congelado: restore exitoso tras pin Pkcs, build Release con cero errores/advertencias; 54 pruebas unitarias/API/arquitectura satisfactorias antes de agregar la suite SQL; generacion offline exitosa de seis scripts. La suite SQL compila en Release; su ejecucion corresponde al principal. Evidencia SQL real, resultados finales, hashes, QA/review y decision de aceptacion se registran separadamente por el principal.

Omisiones y limites: sin creacion de cuentas Windows, cambios de politica de Windows, logins SQL/secretos, acceso IDI, D2-B/C, produccion ni autoaprobacion. Sin identidades Windows independientes suministradas, quedan BLOCKED autenticacion real del migrador/runtime/pruebas y smoke API con persistencia positiva bajo identidad runtime. Los testigos WITHOUT LOGIN solo prueban autorizacion SQL efectiva. No declarar PASS integral mientras subsistan estos bloqueos. Aceptacion final exclusivamente humana.
