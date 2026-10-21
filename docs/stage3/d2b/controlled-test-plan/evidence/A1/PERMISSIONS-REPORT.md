# Permisos A1 — sin conexión

Continuación: RECOVERY-RESULT.md identifica kdi_test_runner/kdi_tests en Test y lectura de las tres historias comprobadas durante D2-A. Esa evidencia histórica respalda la selección de cuenta para A1; no acredita permisos vigentes ni suministra autenticación al sandbox. El intento actual continúa sin conexión ni medición efectiva.

Observación: 2026-10-21T00:34:11.5786286Z. Operador de proceso Windows DESKTOP-F25O3MF\CodexSandboxOffline (NTLM/autenticado); no prueba autorización SQL. Servidor/base son destinos propuestos, no verificados. Script hash 60dbad787ec5b33a421f6944549a78962314603c6fda8f0f8a96b88683bbf79c, no ejecutado.

No se obtuvieron ORIGINAL_LOGIN(), USER_NAME() ni sys.fn_my_permissions. Login SQL, usuario DB, roles, permisos DATABASE y privilegios por objeto permanecen **UNKNOWN/NOT RUN**, no DENIED ni NULL interpretado como cero. No se estableció si la identidad Windows tiene login/mapping SQL. No se sustituyó por administrador, kdi_migrator ni otra cuenta.

El script aprobado listaría permisos DATABASE de la conexión actual; no mide permisos efectivos de kdi_app ni roles globales/otros usuarios. Tampoco autoriza consultas adicionales, impersonación o probes de escritura. Un resultado favorable futuro no bastaría para certificar mínimo privilegio runtime.

No GRANT/REVOKE/ALTER ROLE ni creación de cuentas. A4 y A8 siguen fuera de alcance. Se necesita identificación del operador existente; tras ella, ejecutar solo A1 y documentar la lista real de permisos sin inferencias. Evidencia: context.json y EXECUTION-LOG.md; no hay salida SQL.
