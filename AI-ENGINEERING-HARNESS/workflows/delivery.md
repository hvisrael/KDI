# Etapas y gates

Discovery -> Requirements -> Architecture -> ADR -> Backlog -> Implementation -> QA -> Code review -> Integration -> Release authorization -> Deployment (fuera de este harness).

Gate G0: task válida, rutas permitidas, repo autorizado.
Gate G1: análisis estructurado PASS con evidencia.
Gate G2: autorización humana de escritura.
Gate G3: ingeniería PASS, diff dentro del alcance.
Gate G4: QA independiente PASS y sin modificaciones.
Gate G5: code review PASS y sin modificaciones.
Gate G6: pruebas automatizadas PASS con salidas guardadas.
Gate G7: cierre técnico con evidencia; **nunca equivale a despliegue**.

Todo gate no verificable -> BLOCKED. Toda falla constatada -> FAIL.
