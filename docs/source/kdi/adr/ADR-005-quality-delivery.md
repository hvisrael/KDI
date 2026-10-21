# ADR-005 — Calidad, contratos y entrega

Fecha: 2026-10-17. Estado: Propuesto.

Contexto: probar un ciclo completo con evidencia y preparar entregas empresariales sin automatizar producción prematuramente.

Propuesta: unitarias/integración/API separadas, E2E para flujos críticos, pruebas de arquitectura; OpenAPI como contrato verificable; CI valida backend/web y genera artefacto versionado. CD por ambiente con aprobación y migraciones controladas. Elegir frameworks, proveedor CI y hosting al fundar, sin paquetes ni archivos de pipeline activos ahora.

Alternativas: solo E2E dificulta diagnóstico; solo unitarias no comprueba SQL/auth/HTTP; despliegue continuo sin puertas exige madurez y entorno aún inexistentes. Propuesta xUnit/Playwright sujeta a compatibilidad y aceptación.

Consecuencias: inversión en fixtures SQL aisladas, identidades sintéticas y evidencia; control de tiempo y costo por corrida. PASS técnico no equivale a aprobación humana o producción.

Validación: AC01–13 trazados, pipeline reproducible, QA y revisión independientes sobre el mismo candidateHash. Retención de artefactos, escaneo, RPO/RTO y recuperación operacional pendientes de objetivos acordados.
