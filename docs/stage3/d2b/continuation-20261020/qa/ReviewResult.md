# ReviewResult — qa

TaskInput: D2B-CONT-20261020-QA, intento 1. Agente efectivo: qa-engineer, identificador `/root/d2b_qa`. Autorización: validación local de preparación D2-B por Human Orchestrator, canalizada por principal. Presupuesto: 25 minutos. Producto y fuentes evaluados sin modificaciones por QA; salidas de compilación generadas en directorios excluidos. Sin redelegación.

candidateHash final: `a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102`, manifiesto de 190 archivos. `integrity-after.log` y `integrity-final.log` confirman inventario/contenido antes y después de repetir todas las comprobaciones locales. TaskInput intento 2 y presupuesto adicional de 10 minutos autorizados por principal.

El hash inicial `1ab77cd831a7426ba0cdd864cbb0c3edd5479f2d81a416cfec159a12a60888f6` se verificó en `integrity-before.log`. El principal modificó propuestas SQL para retirar DELETE de Sessions y regeneró el manifiesto. No se conservó copia completa del manifiesto previo; QA no acredita una comparación exhaustiva de ambos inventarios históricos. Esta limitación se resuelve para las pruebas actuales mediante repetición completa sobre el hash final. QA no editó producto.

## Pruebas reproducidas

| Criterio | Resultado observado | Evidencia QA |
|---|---|---|
| Solución backend Release `dotnet build Kulani.KDI.slnx -c Release --no-restore` | exit 0, 0 errores, 0 advertencias | backend-build.log |
| `dotnet test --project tests/Kulani.KDI.UnitTests/Kulani.KDI.UnitTests.csproj -c Release --no-build --no-restore` | exit 0, 57 correctas, 0 fallidas, 0 omitidas | UnitTests.log |
| Mismo comando para ApiTests | exit 0, 20 correctas, 0 fallidas, 0 omitidas | ApiTests.log |
| Mismo comando para ArchitectureTests | exit 0, 24 correctas, 0 fallidas, 0 omitidas | ArchitectureTests.log |
| `npm.cmd run typecheck` | exit 0 | web-typecheck.log |
| `npm.cmd run lint` | exit 0 | web-lint.log |
| `npm.cmd run build` | exit 0; 32 módulos Vite | web-build.log |
| Chromium, `node node_modules/@playwright/test/cli.js test --reporter=list --output=E:/INSSOFT/KDI/docs/stage3/d2b/continuation-20261020/qa/browser-artifacts` | exit 0, 11 correctas (8.0 s), retries 0 | browser.log |

La tabla conserva la primera reproducción. Todas las filas se repitieron satisfactoriamente sobre el hash final con logs `backend-build-final.log`, `UnitTests-final.log`, `ApiTests-final.log`, `ArchitectureTests-final.log`, `web-typecheck-final.log`, `web-lint-final.log`, `web-build-final.log`, `browser-final.log`. Mismos comandos y conteos; browser final usa `browser-artifacts-final` y terminó en 7.1 segundos. Backend final: 0 errores y advertencias.

SQL offline independiente: `python docs/stage3/d2b/continuation-20261020/qa/verify_sql.py`, exit 0 (`sql-offline-final.log`). Verifica 14 hashes y tamaños, 12 reproducciones exactas existentes, 4 rollback con guardas de destino/historia/alcance de tablas y tres propuestas sin GRANT DELETE. No genera ni ejecuta SQL. El script PowerShell existente no se ejecutó por política de firma; QA usó comprobaciones Python independientes sin cambiar política. La reproducción fresca de generación pertenece al principal; comparar archivos de reproducción existentes no acredita por sí solo su regeneración durante este QA.

Backend ejecutado desde `.local/d2b-candidate`; frontend desde `src/Kulani.KDI.Web`. DOTNET_CLI_HOME=`E:/INSSOFT/KDI/.local/dotnet-home`; NUGET_PACKAGES=`E:/INSSOFT/KDI/.local/nuget/packages`. PATH antepone `C:/Users/Israel/AppData/Local/pi-node/current`; PLAYWRIGHT_BROWSERS_PATH=`C:/Users/Israel/AppData/Local/ms-playwright`. Paquetes existentes; no restore, instalación ni actualización. El intento inicial `npm` encontró bloqueo de firma de npm.ps1; `npm.cmd` permitió ejecutar el mismo npm sin cambios de política global. Advertencias de colores Playwright no afectan resultado.

Playwright gestionó servidores propios HTTP locales API y preview. Se deshabilitan trace, screenshots y video en su configuración. Se sobrescribió reporter para evitar escribir el reporte JSON histórico dentro del candidato. Se evaluaron seis contratos sintéticos de acceso y cinco de fundación, incluido proxy con API real anónima. No hubo login real.

## Omisiones y bloqueos críticos

- SqlIntegrationTests: no ejecutados por contener operaciones persistentes no autorizadas. Upgrade/downgrade reales, idempotencia contra SQL, permisos efectivos negativos, aislamiento con fixtures, concurrencia y auditoría transaccional siguen BLOCKED. Generación SQL offline corresponde al principal; QA verificó artefactos offline con las limitaciones descritas.
- Auth0/OIDC real: no probado. Las pruebas de opciones verifican configuración de issuer/audience/algoritmos/PKCE; no ejercitan discovery real, validación criptográfica de tokens reales, intercambio de código, callback, nonce/state ni replay contra proveedor.
- HTTPS, cookies reales de navegador, keyring/ACL, login/logout completo y revocación persistente: BLOCKED por configuración/infraestructura/autorización y cuentas requeridas.
- Expiración y carreras de revocación, multiempresa y autorización: cobertura local con dobles comprobada; no acredita funcionamiento contra SQL ni proveedor real.
- Restore limpio, auditoría actual de dependencias y validación de runtime/SDK actualizado: fuera de esta reproducción con cachés.

## Veredicto

**BLOCKED para aceptación/cierre D2-B.** Los criterios locales ejecutados sobre el hash final tienen resultados satisfactorios; hay criterios críticos de integración y seguridad sin probar. No se declara PASS global. Siguiente acción: obtener autorización concreta del paquete Test y fixtures, además de preparación HTTPS/Auth0 mediante operador autorizado, antes de validar integración real.
