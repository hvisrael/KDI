"""Read-only verification of current baseline, installed profile and D2-A."""
from pathlib import Path
import hashlib, json, subprocess, tomllib
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
C=HERE/'candidate'
sha=lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
m=C/'manifest.json'
assert sha(m)==(HERE/'manifest.sha256').read_text().split()[0]
actual=[{'path':p.relative_to(C).as_posix(),'sha256':sha(p)} for p in sorted(C.rglob('*')) if p.is_file() and p!=m]
assert actual==json.loads(m.read_bytes())['entries'], 'Frozen candidate changed'
for src,dst in [('.codex/agents/dba-engineer.toml','profile.toml'),('.codex/config.toml','project-config.toml'),('AGENTS.md','AGENTS.d2a-original.md'),('docs/stage3/d2a/candidate-manifest.json','d2a-original-manifest.json')]:
    assert (ROOT/src).read_bytes()==(C/dst).read_bytes(),src
for name in ['profile.toml','catalog-profile.toml','project-config.toml']:
    value=tomllib.loads((C/name).read_text(encoding='utf-8-sig'))
    if name!='project-config.toml':
        assert set(value)=={'name','description','developer_instructions'}
        assert value['name']=='dba-engineer'
assert sha(C/'d2a-original-manifest.json')=='66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f'
subprocess.run(['python','tools/d2a-manifest.py','--verify'],cwd=ROOT,check=True)
print('Regularized candidate inventory, hashes, TOML and installed profile verified')
print('Manifest SHA-256 '+sha(m))
print('Profile SHA-256 '+sha(C/'profile.toml'))
