"""Remove an unused runtime permission from review artifacts; never connects to SQL."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[4] / '.local/d2b-candidate'
paths = [root / 'docs/stage3/d2b/data-evidence/PROPOSED-RUNTIME-GRANTS.sql']
prepared = root / 'tools/Kulani.KDI.Migrations/prepared-sql'
paths += sorted(prepared.glob('*-runtime-grants.sql'))
old = b'GRANT SELECT, INSERT, UPDATE, DELETE ON OBJECT::[identity].[Sessions]'
new = b'GRANT SELECT, INSERT, UPDATE ON OBJECT::[identity].[Sessions]'
for path in paths:
    content = path.read_bytes()
    assert content.count(old) == 1, path
    path.write_bytes(content.replace(old, new))
manifest = prepared / 'manifest.json'
entries = json.loads(manifest.read_text(encoding='utf-8-sig'))
for entry in entries:
    data = (prepared / entry['path']).read_bytes()
    entry['sha256'] = hashlib.sha256(data).hexdigest().upper()
    entry['bytes'] = len(data)
manifest.write_text(json.dumps(entries, indent=2) + '\n', encoding='utf-8')
print('Updated three SQL proposals and their artifact manifest. No SQL executed.')
