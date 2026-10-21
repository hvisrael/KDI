# Candidato de cierre DBA

Alcance exclusivo: incorporación DBA; ninguna aceptación de D2-A, D2-B/C o persistencia SQL. Perfil efectivo: effective-profile.toml, dba-engineer; la versión de catálogo catalog-profile.toml es distinta y no se atribuye al agente ejecutado. Registro comprobado sin escrituras externas. Despacho actual real documentado en dispatch.json y conversación /root/dba_closure_synthetic. Máximo observado: un DBA terminado, después QA y code-reviewer simultáneos. Sin redelegación.

Criterios: perfil real y registrado; una revisión SQL sintética sin conexión/modificación; D2-A original íntegro; instrucciones conservadas por separado; paquete congelado e idéntico para QA/revisor; trazabilidad retrospectiva completa de AGENTS.md. El último criterio no puede acreditarse con los registros disponibles. Estado propuesto BLOCKED, nunca PASS con pendiente crítico. La falta de pruebas SQL reales es deliberada y no constituye criterio de onboarding; no se atribuyen permisos SQL efectivos.

historical/ contiene evidencia anterior, no resultados actuales. Scripts históricos de registro/verificación no son parte ejecutable del cierre y no se vuelven a ejecutar. El cierre no instala, cambia permisos, conecta SQL, accede a IDI, despliega ni cambia configuraciones globales.

manifest.json enumera todos los archivos de candidate excepto sí mismo. Su SHA-256 se guarda fuera del candidato en manifest.sha256. Informes independientes y comprobaciones posteriores van en reports/, fuera del candidato. Cualquier cambio de contenido o inventario invalida las revisiones y requiere nuevo candidato. No editar el candidato una vez generado el manifiesto.
