"""Independent offline inspection only; never connects to SQL."""
import hashlib
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[5] / '.local/d2b-candidate'
prepared = root / 'tools/Kulani.KDI.Migrations/prepared-sql'
repro = root / 'tools/Kulani.KDI.Migrations/repro-sql'
manifest = json.loads((prepared / 'manifest.json').read_text(encoding='utf-8-sig'))
assert len(manifest) == 14
for entry in manifest:
    raw = (prepared / entry['path']).read_bytes()
    assert hashlib.sha256(raw).hexdigest().lower() == entry['sha256'].lower(), entry['path']
    assert len(raw) == entry['bytes'], entry['path']
files = [p for p in repro.iterdir() if p.is_file()]
assert len(files) == 12
for file in files:
    assert file.read_bytes() == (prepared / file.name).read_bytes(), file.name
for database in ['KulaniKDI_Test', 'KulaniKDI_Dev']:
    for module, tables in [('identity', {'AuditEntries', 'Grants', 'Sessions', 'PermissionCatalog', 'Users'}),
                           ('organizations', {'AuditEntries', 'OrganizationMemberships', 'Organizations'})]:
        sql = (prepared / f'{database}-{module}.rollback.sql').read_text(encoding='utf-8-sig')
        assert f"DB_NAME() <> N'{database}'" in sql
        assert 'Expected D2-B history required' in sql
        assert 'DROP SCHEMA' not in sql
        assert not re.search(r'DROP TABLE \[[^]]+\]\.\[__EFMigrationsHistory\]', sql)
        assert "WHERE [MigrationId] = N'20261018000100" not in sql
        assert set(re.findall(r'DROP TABLE \[([^]]+)\]\.\[([^]]+)\]', sql)) == {(module, t) for t in tables}
    grants = (prepared / f'{database}-runtime-grants.sql').read_text(encoding='utf-8-sig')
    assert 'GRANT SELECT, INSERT, UPDATE ON OBJECT::[identity].[Sessions]' in grants
    assert not re.search(r'GRANT[^;]*DELETE', grants, re.I)
proposal = (root / 'docs/stage3/d2b/data-evidence/PROPOSED-RUNTIME-GRANTS.sql').read_text(encoding='utf-8-sig')
assert not re.search(r'GRANT[^;]*DELETE', proposal, re.I)
print('OFFLINE VERIFIED: 14 hashes/byte counts; 12 exact reproductions; 4 rollback scopes/guards; 3 proposals without DELETE grant. No SQL executed.')
