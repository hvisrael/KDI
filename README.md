# AI-ENGINEERING Harness v1.0

Harness de ingeniería con Codex CLI para KDI u otros repositorios. **No sustituye el repositorio KDI ni modifica SGARH.** Ejecuta especialistas en procesos aislados, conserva logs y controla gates. Se entrega como paquete para instalar en la estación de trabajo.

## Componentes
- Orquestación ejecutable: `scripts/harness.py` (Python 3.10+ estándar, sin dependencias externas).
- Especialistas: analista/arquitecto, ingeniero, QA y revisor.
- Contratos JSON Schema y resultados estructurados.
- Controles: verificación de raíz Git, rechazo de IDI, no producción, sandbox read-only para roles observadores, comprobación de cambios no autorizados, aprobación humana para escritura y gates de pruebas.
- Auditoría: carpeta `runs/<id>` con respuestas, comandos de validación, hash de Git y manifiesto de ejecución.

## Requisitos
Python 3.10+, Git, Codex CLI autenticado (`codex login`), repositorio limpio o cambios preexistentes documentados. Ejecutar desde Windows PowerShell o terminal. No se requieren paquetes Python adicionales.

## Uso (Windows PowerShell)
```powershell
cd E:\INSSOFT\AI-ENGINEERING-HARNESS
python scripts/harness.py doctor --project E:\INSSOFT\KDI
python scripts/harness.py init-task --project E:\INSSOFT\KDI --title "Implementar catálogo" --out examples/my-task.json
# Editar examples/my-task.json: acceptance_criteria, test_commands, allowed_paths.
python scripts/harness.py run --task examples/my-task.json --project E:\INSSOFT\KDI --dry-run
# Solo tras revisar alcance y pruebas; escritura exige bandera explícita:
python scripts/harness.py run --task examples/my-task.json --project E:\INSSOFT\KDI --execute --authorize-write
```
**Nota:** la ruta es ejemplo; usa donde realmente estén los repositorios. El archivo de tarea contiene rutas relativas; no admite IDI. `--authorize-write` autoriza cambios locales en el repositorio de trabajo, **no** cambios de base de datos, migraciones, producción ni despliegues.

## Validación
```powershell
python -m unittest discover -s tests -v
```
Los test_commands de cada tarea son obligatorios y se ejecutan por `subprocess` sin `shell=True`; cada comando se expresa como arreglo de argumentos, por ejemplo `["dotnet", "test", "KDI.sln", "--no-restore"]`.

## Garantías y límites
El launcher controla cómo invoca Codex; no es una sandbox de sistema operativo. La protección contra escritura de QA y Review es **detectiva** mediante Git y sandbox de Codex, no una garantía criptográfica sobre archivos ignorados/no rastreados. Ejecútalo dentro de una VM o contenedor sin secretos, sin acceso a producción, con permisos mínimos. No garantiza que Codex respete todos los límites de rutas indicados en prompts. Para seguridad fuerte, usa worktrees/containers separados y controles del SO. El veredicto del modelo nunca reemplaza las pruebas reales.

**Estado de esta entrega:** fuentes y pruebas locales incluidas; no se ha ejecutado Codex ni se ha instalado en tu computadora.
