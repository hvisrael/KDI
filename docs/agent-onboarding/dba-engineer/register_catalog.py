"""Apply only the reviewed DBA catalog additions; no KDI profile overwrite."""
from pathlib import Path
import hashlib
import json
import tomllib

HERE = Path(__file__).resolve().parent
ROOT = Path('E:/INSSOFT/AI-ENGINEERING')
payload = tomllib.loads((HERE / 'dba-engineer.toml').read_text(encoding='utf-8'))
assert set(payload) == {'name', 'description', 'developer_instructions'}
assert payload['name'] == 'dba-engineer'

def create_once(target, content):
    if target.exists():
        assert target.read_bytes() == content, f'Existing different file; stop: {target}'
        return 'unchanged'
    target.write_bytes(content)
    return 'created'

results = {}
for relative, source in [('roles/dba-engineer.md', 'role.md'),
                         ('codex/candidates/agents/dba-engineer.toml', 'dba-engineer.toml')]:
    target = ROOT / relative
    results[relative] = create_once(target, (HERE / source).read_bytes())

additions = {
    'roles/README.md': '\n## Incorporación DBA autorizada — 2026-10-18\n\nSe incorpora [dba-engineer](dba-engineer.md) para ingeniería de datos y SQL Server. Engineer conserva implementación general; database-architect conserva arquitectura neutral de datos. Asignar un responsable por entregable. Adaptación TOML en codex/candidates/agents/dba-engineer.toml; KDI ya tenía perfil instalado y no se sobrescribe. Máximo dos especialistas simultáneos; sin ampliación de permisos.\n',
    'workflows/feature-development.md': '\n## Especialidad DBA — 2026-10-18\n\nCuando existan tareas de datos o SQL Server, el principal puede asignarlas a dba-engineer con TaskInput, rutas, destinos, herramientas, presupuesto y autorización por acción. Separa diseño e implementación de datos de la implementación general de engineer. Tras integrar, congela candidato; QA y code-reviewer revisan independientemente, sin modificarlo. DBA autor no sustituye estos controles. Máximo dos especialistas simultáneos, sin redelegación. En la incorporación KDI se autoriza exclusivamente revisión estática D2-A: sin conexiones SQL, cambios de bases, cuentas o permisos; sin IDI ni D2-B/C.\n'
}
for relative, addition in additions.items():
    target = ROOT / relative
    current = target.read_text(encoding='utf-8')
    if addition.strip() not in current:
        with target.open('a', encoding='utf-8', newline='\n') as stream:
            stream.write(addition)
        results[relative] = 'appended'
    else:
        results[relative] = 'unchanged'

print(json.dumps({'results': results, 'sha256': {
    p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in results
}}, indent=2))
