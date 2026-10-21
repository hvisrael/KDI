# D2-B — estado comprobado

> **Estado canónico del proyecto:** [docs/stage3/STATE.md](../STATE.md) — tabla única de gates, hashes vigentes, glosario de estados y decisiones abiertas. Este documento describe D2-B; no sustituye al índice.

Actualización 2026-10-20: [continuación, validación y revisión independiente](continuation-20261020/RESULT.md). La [preparación previa](preparation-20261020/RESULT.md) describe el intento anterior.

Backend Release y frontend compilan; **71** unitarias, 20 API, 24 arquitectura y 11 navegador satisfactorias. SQL conectado/Auth0/HTTPS real permanecen BLOCKED. No cierre D2-B ni release aceptado.

Revisión independiente detectó y verificó corrección de permiso DELETE innecesario en propuestas SQL de sesiones. Congelado vigente: `91dce7d9e495ab6f9596336c4a0bf250e12c15eafd814701b597d20e9e0cb448`, **197 archivos**. Dictámenes y límites en la continuación.

**A4 — permisos runtime concedidos (2026-10-21).** Los 10 permisos runtime de `kdi_application` sobre 7 objetos están concedidos y verificados con aserciones duras: el conjunto coincide **exactamente** con el aprobado, `kdi_application` **no tiene `DELETE`**, **no tiene** permiso sobre `organizations.AuditEntries`, **no tiene** concesiones de esquema, los seis `DENY` de DDL siguen intactos, `kdi_tests` **sigue siendo de solo lectura** y las membresías y logins no han cambiado. `kdi_app` cumple las nueve condiciones del interceptor de privilegios del runtime.

La concesión la ejecutó la **identidad del operador**, no `kdi_migrator`: los objetos D2-B **no tienen dueño** (`sys.objects.principal_id` es NULL) y SQL Server solo permite conceder sobre un objeto sin dueño a quien tenga `CONTROL`; con `kdi_migrator` se produce `Msg 15151`. El conjunto concedido es idéntico por ambas vías: cambia quién concede, no qué se concede. Ver [paquete de A4](controlled-test-plan/A4-RUNTIME-GRANTS-PACK.md) y la evidencia en `controlled-test-plan/evidence/A1/A4-*`.

**Corrección de la ruta de credenciales del operador (2026-10-21).** `OperatorConnection.Create` entregaba a `SqlCredential` una copia del `SecureString` **no marcada como de solo lectura**, y el constructor la rechazaba siempre (`ArgumentException: password debe marcarse como de solo lectura`): `tools/Kulani.KDI.AccessAdmin` (`plan` y `apply`) y `tools/Kulani.KDI.Migrations --apply` eran inejecutables. Corregido en tres rondas con revisión independiente de code-reviewer y QA, y 14 tests nuevos. Ver [CODE-REVIEW-R2.md](operator-credential-fix-20261021/CODE-REVIEW-R2.md) y [QA.md](operator-credential-fix-20261021/QA.md).

**Corrección del harness de fixtures (2026-10-20).** Los siete defectos F1–F7 que el QA de preparación dejó como condición previa de A5 están corregidos o documentados, y el cambio fue revisado de forma independiente por un code-reviewer y un qa-engineer en sesiones separadas sobre el mismo hash, sin abrir ninguna conexión a SQL Server. El QA descubrió además un bloqueante de plan: el rol `kdi_tests` de `KulaniKDI_Test` solo tiene `GRANT SELECT`, y el harness necesita `INSERT` y `DELETE`. Ver [resultado](harness-correction-20261020/RESULT.md) · [revisión](harness-correction-20261020/CODE-REVIEW.md) · [QA](harness-correction-20261020/QA.md) · [resolución](harness-correction-20261020/CODE-REVIEW-DELTA.md).

Trabajo aislado en `.local/d2b-candidate`. D1/D2-A aceptados no se modificaron. Los índices históricos README/STATUS de fundación se conservan como parte de la línea base; este documento indica el avance posterior.
