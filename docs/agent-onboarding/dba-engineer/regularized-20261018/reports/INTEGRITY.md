# Verificaciones del principal

Comando reproducible: `python docs/agent-onboarding/dba-engineer/regularized-20261018/verify.py`.

Ejecutado después de congelar y después de despachar revisores: salida 0. Inventario y contenido del congelado coinciden; TOML válido; perfil instalado, configuración proyecto, AGENTS y manifiesto D2-A idénticos a sus copias. No ejecución SQL.

- Perfil SHA-256: 0444822eceba583f5b3ac49def40692e4cc479aa5fc9cfa337390a1a99bd90d2.
- Manifiesto regularizado SHA-256: cdaaf6343589f58b7d654858020e282492253f9139ff372e96acdd43e5d29579.
- Manifiesto D2-A SHA-256: 66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f. `python tools/d2a-manifest.py --verify`: contenido e inventario verificados.
- Manifiesto de cierre anterior SHA-256 preservado: fcd189bd43eff38c72d7f9410fa419d11ca75b804e4a07e28af1005c18117e32. No sustituye su resultado histórico BLOCKED.

Las comprobaciones del principal no sustituyen QA ni Code Review independientes. El archivo de perfil actualizado es la nueva línea base de KDI; no se afirma recarga automática del proceso existente. Sin cambios de configuración ni concesiones de permisos.
