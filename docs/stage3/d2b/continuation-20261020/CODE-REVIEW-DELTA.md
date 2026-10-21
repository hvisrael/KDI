# ReviewResult — code-review D2-B, intento 2

- TaskInput: `D2B-CONT-20261020-REVIEW`, intento 2, delta; presupuesto 10 minutos.
- Agente: `/root/d2b_review`; agent_type efectivo: `code-reviewer`.
- candidateHash: `a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102`.
- Hash anterior revisado: `1ab77cd831a7426ba0cdd864cbb0c3edd5479f2d81a416cfec159a12a60888f6`.
- Resultado: **R1 RESUELTO en la propuesta SQL observada. Revisión estática acotada sin hallazgos adicionales confirmados. Integración real BLOCKED; D2-B NO cerrado.**

Este addendum conserva el informe original `CODE-REVIEW.md`; su hallazgo corresponde al hash anterior. El revisor no fue autor de la corrección y no editó producto, SQL o manifiestos. No hubo operaciones SQL ni cambios de permisos, infraestructura o configuración global. Única escritura de esta revisión: el presente informe.

## Corrección comprobada

Las siguientes tres propuestas conceden ahora `SELECT, INSERT, UPDATE` sobre `identity.Sessions` a `kdi_application`, sin DELETE:

- `docs/stage3/d2b/data-evidence/PROPOSED-RUNTIME-GRANTS.sql:13` dentro del candidato.
- `tools/Kulani.KDI.Migrations/prepared-sql/KulaniKDI_Test-runtime-grants.sql:15`.
- `tools/Kulani.KDI.Migrations/prepared-sql/KulaniKDI_Dev-runtime-grants.sql:15`.

La separación observada coincide con el código previamente revisado: runtime revoca mediante UPDATE; purge privilegiado efectúa el borrado físico. R1 queda resuelto para estos artefactos preparados, sin afirmar cambios en permisos SQL existentes.

## Evidencia independiente

Desde la raíz KDI:

```powershell
python docs/stage3/d2b/preparation-20261020/freeze.py --verify
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .local/d2b-candidate/tools/Kulani.KDI.Migrations/Verify-PreparedSql.ps1
rg -n 'GRANT.*Sessions|DELETE' .local/d2b-candidate/tools/Kulani.KDI.Migrations/prepared-sql -g '*runtime-grants.sql'
rg -n 'GRANT.*Sessions|DELETE' .local/d2b-candidate/docs/stage3/d2b/data-evidence/PROPOSED-RUNTIME-GRANTS.sql
```

Freeze: exit 0, 190 archivos, SHA-256 coincidente con candidateHash; repetido al final con el mismo resultado. Verificador SQL: exit 0; 14 hashes del paquete, 12 hashes de reproducción y cuatro alcances de rollback comprobados sin ejecutar SQL. La búsqueda de `SELECT, INSERT, UPDATE, DELETE ON OBJECT::[identity].[Sessions]` sobre los archivos SQL del candidato no encontró coincidencias. Una primera búsqueda utilizó un comodín de ruta incompatible con rg en Windows; se corrigió usando `-g` y se comprobó explícitamente cada ubicación.

Se inspeccionaron el script de corrección `restrict-proposed-grants.py`, el manifiesto SQL actualizado y las tres propuestas. **No se conservó copia independiente del manifiesto anterior antes de regenerarlo; por ello no se afirma una comparación exhaustiva archivo por archivo entre ambos freezes.** El principal reporta cuatro archivos modificados (tres propuestas y manifest SQL), pero ese reporte no sustituye un diff independiente. Sí se verificaron la corrección concreta, la consistencia del paquete actual y la integridad completa contra el nuevo manifiesto.

## Alcance pendiente

Este delta no modifica los límites de la revisión original. SQL conectado, migraciones/repetición, permisos efectivos, fixtures/limpieza, downgrade/recuperación, Auth0/OIDC y navegador HTTPS real siguen BLOCKED. No se ha acreditado auditoría actual de dependencias ni restore limpio. No se declaran pruebas pendientes como exitosas ni aceptación global.

Siguiente acción: consolidar el informe QA sobre este mismo hash y presentar el candidato preparado con esos bloqueos. Cualquier ejecución persistente y los permisos exactos requieren autorización humana aplicable; el hash y la revisión no conceden esa autorización.
