# D2-B: continuación técnica local

Estado: **preparación local; integración real BLOCKED; D2-B NO cerrado**. Solicitud humana: «KDI — Continuación de Stage 3 / D2-B». Sin despliegue, SQL persistente, GRANT, credenciales, cambios globales ni acceso a IDI.

Se leyeron AGENTS.md, DECISIONS.md, aprobación/aceptación D1, aceptación D2-A, autorización y preparación D2-B. La raíz no es repositorio Git; se verificó integridad por manifiestos. El manifiesto aceptado D1 conserva SHA-256 `e2bb22b739b5924e8c2ead3af421e56ac97dc6e6b0209a7d0b7b5da8d1f51193`. Verificación del original D2-A, candidato aceptado y evidencia: exit 0 en `accepted-integrity.log`.

## Resultado local

QA independiente ejecutó backend Release: exit 0, cero errores/advertencias; Unit 57/57, API 20/20 y arquitectura 24/24, sin fallos ni omitidas; frontend typecheck/lint/build exit 0; Playwright Chromium 11/11. Las pruebas API usan TestHost y almacenamiento doble; navegador combina contratos sintéticos con smoke de fundación. No acreditan OIDC ni persistencia real. Evidencia y condiciones exactas en [QA](qa/ReviewResult.md).

Generación SQL offline Test y Dev: exit 0; doce artefactos recién reproducidos coinciden con paquete preparado. Verificación final: catorce hashes y cuatro alcances de rollback, exit 0 en `sql-final.log`. [Inventario y estrategia de reversión](SQL-INVENTORY.md). [Matriz de autenticación y preparación operacional](AUTH-READINESS.md).

Se usaron dependencias locales existentes y compilación sin restore. No se afirma restore limpio, auditoría de dependencias vigente ni actualización/certificación del SDK instalado. El primer comando npm de QA encontró política de firma de npm.ps1; npm.cmd permitió continuar sin cambiar política global.

Tras la corrección de propuestas SQL, QA volvió a ejecutar compilación backend, las tres suites backend, typecheck/lint/build web y navegador sobre el congelado final. Mismos resultados; logs `qa/*-final.log`, navegador 11/11 en 7.1 s. La verificación final D2-A también terminó exit 0: `accepted-integrity-final.log`.

## Corrección y congelado propuesto

Review encontró R1/P2: propuesta de DELETE sobre identity.Sessions para runtime, aunque la eliminación corresponde al mantenimiento privilegiado. Principal retiró DELETE en las tres propuestas SQL D2-B y actualizó el manifiesto SQL. No cambió código ejecutable ni se aplicó SQL. El informe original se conserva en [CODE-REVIEW.md](CODE-REVIEW.md); resolución independiente en [CODE-REVIEW-DELTA.md](CODE-REVIEW-DELTA.md).

Candidato: `.local/d2b-candidate`, 190 archivos. Manifiesto `../preparation-20261020/candidate-manifest.json`, nuevo SHA-256 **a1cbed38778ebf45e634fcf8160204f5645fc8b7baf72bf3fd00fe93894a4102**. Sustituye el hash inicial `1ab77cd831a7426ba0cdd864cbb0c3edd5479f2d81a416cfec159a12a60888f6`; los informes previos describen ese estado histórico. El manifiesto se regeneró en la misma ruta; no se conservó copia anterior, por lo que no se afirma diff exhaustivo independiente entre los 190 hashes de ambos intentos. La evidencia inicial/final y corrección observada se distinguen expresamente.

Verificación: `python docs/stage3/d2b/preparation-20261020/freeze.py --verify`. Exclusiones documentadas en el manifiesto: salidas de build, dependencias, wwwroot y evidencia generada. El congelado identifica fuentes y SQL propuesto, no un release aceptado ni evidencia externa.

## Delegaciones y preparación para QA

| Identificador | agent_type efectivo | Responsabilidad / evidencia |
|---|---|---|
| /root/d2b_qa | qa-engineer | Compilaciones y pruebas independientes, integridad y bloqueos: qa/ |
| /root/d2b_review | code-reviewer | Seguridad/SQL/OIDC/HTTPS, R1 y revisión de corrección: CODE-REVIEW*.md |
| /root | principal | Integración, corrección de propuestas, reproducción SQL y documentación |

Máximo dos subagentes activos. QA/revisor no modificaron fuentes; outputs y reportes quedan separados. Contrato QA: verificar hash, reconstruir, probar sin credenciales ni operaciones SQL; conservar BLOCKED para todo criterio que necesita entorno real.

## Bloqueos y siguiente acción

1. Autorización concreta de migraciones y fixtures/limpieza en KulaniKDI_Test; pruebas conectadas no ejecutadas. Nuevos GRANT siguen prohibidos y requieren autorización separada. No se ejecutó SqlIntegrationTests, que incluye operaciones persistentes.
2. Tenant/aplicación/cuentas Auth0 preparados por operador autorizado y valores públicos concretos; secretos solo mediante entrada privada aprobada.
3. Certificado HTTPS localhost, confianza y keyring externo privado aprobados/preparados. No se modificó infraestructura.

No hubo fallos finales de compilación o pruebas locales; R1 fue un hallazgo de revisión sobre SQL propuesto. No hay PASS global ni cierre D2-B. Siguiente acción: revisar el paquete Test y autorizar específicamente sus migraciones/fixtures y, por separado, los permisos mínimos necesarios; preparar Auth0/HTTPS y ejecutar la matriz real antes de solicitar aceptación.
