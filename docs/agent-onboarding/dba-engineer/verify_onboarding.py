"""Read-only verification; writes only its requested evidence file."""
from pathlib import Path
import hashlib
import json
import tomllib
import datetime

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
CATALOG = Path('E:/INSSOFT/AI-ENGINEERING')
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
profiles = {}
for path in sorted((ROOT / '.codex/agents').glob('*.toml')):
    value = tomllib.loads(path.read_text(encoding='utf-8-sig'))
    profiles[path.name] = {'keys': sorted(value), 'name': value.get('name'), 'sha256': sha(path)}
candidate = tomllib.loads((HERE / 'dba-engineer.toml').read_text(encoding='utf-8'))
installed_unchanged = profiles['dba-engineer.toml']['sha256'] == 'c7b601ae6857a451238336bd2bfea727bcc7c48667445f8dd5f1a909c30f88b9'
catalog_files = ['roles/dba-engineer.md', 'codex/candidates/agents/dba-engineer.toml', 'roles/README.md', 'workflows/feature-development.md']
catalog = {p: sha(CATALOG / p) for p in catalog_files}
manifest_path = ROOT / 'docs/stage3/d2a/candidate-manifest.json'
original = {e['path']: e['sha256'] for e in json.loads(manifest_path.read_text(encoding='utf-8'))['entries']}
scope = {'__file__': str(ROOT / 'tools/d2a-manifest.py')}
exec((ROOT / 'tools/d2a-manifest.py').read_text(encoding='utf-8').split('if sys.argv')[0], scope)
actual = {e['path']: e['sha256'] for e in scope['entries']()}
drift = {'added': sorted(actual.keys() - original.keys()), 'removed': sorted(original.keys() - actual.keys()),
         'changed': sorted(p for p in original.keys() & actual.keys() if original[p] != actual[p])}
events = [json.loads(line) for line in (HERE / 'new-session-events.jsonl').read_text(encoding='utf-16').splitlines() if line.strip()]
tool_events = [e['item'] for e in events if e.get('item', {}).get('type') == 'collab_tool_call']
result = {'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'profiles': profiles,
          'catalog_profile_parse': set(candidate) == {'name', 'description', 'developer_instructions'},
          'installed_profile_unchanged': installed_unchanged, 'catalog_hashes': catalog,
          'catalog_profile_matches_staged': (CATALOG / catalog_files[1]).read_bytes() == (HERE / 'dba-engineer.toml').read_bytes(),
          'manifest_sha256': sha(manifest_path), 'candidate_drift': drift,
          'new_cli_thread': next(e['thread_id'] for e in events if e['type'] == 'thread.started'),
          'cli_collaboration_events': tool_events,
          'cli_spawn_event_visible': any(e.get('tool') in ('spawn', 'spawn_agent') for e in tool_events),
          'global_status': 'BLOCKED: candidate drift and incomplete new-session spawn trace'}
(HERE / 'verification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result, ensure_ascii=False, indent=2))
