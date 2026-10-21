# ADR-003 — Datos, concurrencia y trazabilidad

Fecha: 2026-10-17. Estado: Propuesto; resolver coordinación cruzada antes de implementar.

Contexto: SQL Server, propiedad modular y necesidad de integridad entre cliente/proyecto, historial y alcance organizacional.

Propuesta base: una DB, esquema y DbContext por módulo, migraciones con dueño/historial diferenciados; unicidad por organización y código normalizado; rowversion para concurrencia; auditoría atómica a la mutación. EF Core por defecto. No aplicar migraciones automáticamente al arranque productivo.

Alternativas: DB por módulo refuerza separación pero exige consistencia distribuida; DbContext único simplifica transacciones pero facilita navegación y escritura cruzada. Para cliente/proyecto, comparar FK compuesta entre esquemas más coordinación de estado frente a operación transaccional coordinada por Infrastructure con locks de cliente y proyecto en orden estable. FK sola no impide la carrera de archivo; consulta previa sin lock tampoco.

Decisión pendiente: escoger estrategia exacta de atomicidad para alta de proyecto y archivo de cliente activo, aislamiento, restricciones y orden de migraciones. La regla propuesta de rechazo exige prueba concurrente. Application usa contratos; la coordinación no permite escritura arbitraria en datos de otro módulo. `Application/Coordination/ClientLifecycle` está por encima de Organizations y Portfolio; ambos módulos permanecen sin dependencia circular y el archivo no expone una vía que omita la coordinación.

Consecuencias: menor independencia física entre módulos, pero invariantes visibles y probables. Proyecciones futuras pueden ser eventuales con frescura explícita; outbox solo cuando un caso lo requiera.

Validación: carreras de duplicado, edición y alta/archivo; rollback de auditoría; cruces de OrgId; migración en DB aislada. No hay DDL ni SQL ejecutado en esta etapa.
