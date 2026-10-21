# Controles y limitaciones

Las instrucciones del modelo **no** son un perímetro de seguridad. El script inspecciona las diferencias Git y bloquea rutas no autorizadas detectables; cambios transitorios, archivos ignorados, red y acceso externo requieren controles adicionales del sistema operativo. Ejecútalo con usuario no privilegiado y, preferentemente, worktree desechable sin credenciales ni conexiones a entornos sensibles.

El token `human_approved=true` más la bandera CLI constituyen un control manual básico, no una firma de aprobación. Integraciones empresariales deben reemplazarlo por un registro de aprobación autenticado.

`codex exec` con roles secuenciales aislados no garantiza que sean subagentes nativos; registra `effective_agent_type=not_applicable`. La presencia de un archivo de rol jamás demuestra que Codex lo aceptó como `agent_type`. Los logs pueden contener información sensible de prompts: restringir acceso y retención.
