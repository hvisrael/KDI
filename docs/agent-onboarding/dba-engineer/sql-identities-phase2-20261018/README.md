# Equivalencia de evidencia — rollout de identidades SQL, fase 2

## Aviso

Este directorio es una **copia** de la evidencia del rollout de identidades SQL fase 2 (2026-10-18/19) que se conserva bajo el árbol de onboarding del rol `dba-engineer`.

**La fuente normativa es:**

`E:\INSSOFT\KDI\docs\stage3\d2a\closure\identities-20261018\evidence\phase2\`

## Por qué existen las dos copias

El rollout de fase 2 se documentó primero bajo `docs/agent-onboarding/dba-engineer/` porque nació como trabajo de incorporación del rol DBA. Al cerrarse D2-A, la entrega se consolidó bajo `docs/stage3/d2a/closure/identities-20261018/`, que es la ubicación cubierta por el manifiesto de evidencia congelada (`evidence/manifest.json`, SHA-256 `80ff0d9ac23386569e7c620d237a6af401ba6b48dd5f9447859ba3090e646d68`) y por el recibo de aceptación.

Las dos copias tienen contenido idéntico byte a byte en el momento de esta nota.

## Regla de trabajo

- Cualquier corrección, aclaración o evidencia nueva se aplica **solo** en la fuente normativa (`docs/stage3/d2a/closure/identities-20261018/evidence/phase2/`), y solo con la verificación y el congelado que correspondan.
- Esta copia **no se actualiza**. Si divergiera de la fuente, la fuente normativa prevalece y la divergencia debe reportarse como hallazgo.
- No se borra la copia: forma parte del rastro de incorporación del rol y su eliminación rompería referencias históricas de documentación.

## Verificación de integridad

```powershell
python docs/stage3/d2a/closure/identities-20261018/verify.py
```

Ese verificador comprueba el manifiesto de evidencia congelada y el árbol del candidato D2-A aceptado, y confirma además los 13 commits de fase 2 y las cinco autenticaciones directas registradas.

## Relación con el estado vigente

Estado canónico del proyecto: [docs/stage3/STATE.md](../../../stage3/STATE.md).
