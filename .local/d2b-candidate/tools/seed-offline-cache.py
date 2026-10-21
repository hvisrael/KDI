"""Copy verified public package cache entries into KDI; never copy user configuration.

Optional local recovery for restricted networking. Not a package source or a substitute
for online vulnerability audits. Writes only .local and docs/stage3/d1/cache-evidence.json.
"""
import base64
import hashlib
import json
import os
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[1]
profile = Path(os.environ['USERPROFILE'])
evidence = {'nuget': [], 'npmEntries': 0, 'npmMetadata': {}, 'limitations': [
    'Cached bytes validated against their stored hashes, not fresh registry signatures.',
    'No online audit was possible; cache age and current advisories are not verified.',
]}

# Only two known, public packages needed for the API (test dependencies may be absent).
for name, version in [('microsoft.aspnetcore.openapi', '10.0.3'), ('microsoft.openapi', '2.0.0')]:
    source = profile / '.nuget/packages' / name / version
    archive = source / f'{name}.{version}.nupkg'
    if not archive.exists():
        continue
    digest = hashlib.sha512(archive.read_bytes()).digest()
    assert base64.b64decode((source / f'{name}.{version}.nupkg.sha512').read_text()) == digest
    metadata = json.loads((source / '.nupkg.metadata').read_text(encoding='utf-8-sig'))
    assert metadata.get('source', '').rstrip('/') == 'https://api.nuget.org/v3/index.json'
    target = ROOT / '.local/nuget/packages' / name / version
    shutil.copytree(source, target, dirs_exist_ok=True)
    evidence['nuget'].append({'name': name, 'version': version, 'sha512': base64.b64encode(digest).decode()})

# Read only npm's public registry records. Recreate a local cache with validated blobs.
# Preserve no authentication, npmrc, environment dumps, private registry or project files.
source = profile / 'AppData/Local/npm-cache/_cacache'
target = ROOT / '.local/npm-cache/_cacache'
entries = {}
variants = {}
for index in (source / 'index-v5').rglob('*'):
    if not index.is_file():
        continue
    for line in index.read_text(encoding='utf-8', errors='replace').splitlines():
        try:
            checksum, raw = line.split('\t', 1)
            if hashlib.sha1(raw.encode()).hexdigest() != checksum:
                continue
            entry = json.loads(raw)
            key = entry.get('key', '')
            if key.startswith('make-fetch-happen:request-cache:https://registry.npmjs.org/') and entry.get('integrity'):
                entries[key] = (entry, line)
                accept = entry.get('metadata', {}).get('reqHeaders', {}).get('accept', '')
                variants[(key, accept, entry['integrity'])] = (entry, line)
        except (ValueError, KeyError):
            continue

selected = {'vite': '6.4.4', 'react': '19.3.0', 'react-dom': '19.3.0', 'typescript': '5.9.3',
            'eslint': '9.39.5', '@eslint/js': '9.39.5', 'typescript-eslint': '8.65.0',
            '@types/react': '19.3.0', '@types/react-dom': '19.3.0', '@types/node': '22.20.1',
            '@playwright/test': '1.62.1'}

# Limit imported records to the dependency closure of selected metadata. Npm still
# resolves/verifies each version and tarball integrity itself with --offline.
allowed = set(selected)
metadata_records = {}
for key, (entry, _) in entries.items():
    name = key.split('https://registry.npmjs.org/', 1)[1].replace('%2f', '/').replace('%2F', '/')
    if '/-/' not in name:
        metadata_records[name] = (key, entry)

decoded = {}
def read_blob(entry):
    algorithm, digest = entry['integrity'].split('-', 1)
    expected = base64.b64decode(digest)
    hexdigest = expected.hex()
    path = Path('content-v2') / algorithm / hexdigest[:2] / hexdigest[2:4] / hexdigest[4:]
    data = (source / path).read_bytes()
    assert hashlib.new(algorithm, data).digest() == expected
    return path, data

queue = list(allowed)
while queue:
    name = queue.pop()
    if name not in metadata_records:
        continue
    _, entry = metadata_records[name]
    try:
        _, data = read_blob(entry)
        meta = json.loads(data)
        decoded[name] = meta
        # Restrict recursion to versions actually present as tarballs in this cache.
        versions = meta.get('versions', {})
        available = [v for v in versions.values() if 'make-fetch-happen:request-cache:' + v.get('dist', {}).get('tarball', '') in entries]
        if name in selected and selected[name] in versions:
            v = versions[selected[name]]
            evidence['npmMetadata'][name] = {k: v.get(k) for k in ['version', 'engines', 'peerDependencies', 'license']}
            available.append(v)
        for version in available:
            deps = dict(version.get('dependencies', {}))
            deps.update(version.get('optionalDependencies', {}))
            deps.update(version.get('peerDependencies', {}))
            for dependency in deps:
                if dependency not in allowed:
                    allowed.add(dependency)
                    queue.append(dependency)
    except (OSError, ValueError):
        continue

index_lines = {}
for (key, _, _), (entry, line) in variants.items():
    name = key.split('https://registry.npmjs.org/', 1)[1].replace('%2f', '/').replace('%2F', '/').split('/-/', 1)[0]
    if name not in allowed:
        continue
    try:
        relative, data = read_blob(entry)
        dest = target / relative
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(data)
        index_hash = hashlib.sha256(key.encode()).hexdigest()
        dest_index = target / 'index-v5' / index_hash[:2] / index_hash[2:4] / index_hash[4:]
        dest_index.parent.mkdir(parents=True, exist_ok=True)
        # cacache hashes each JSON line; CRLF translation would invalidate it.
        metadata = entry.get('metadata', {})
        if 'reqHeaders' in metadata:
            metadata['reqHeaders'] = {k: v for k, v in metadata['reqHeaders'].items()
                                      if k.lower() in {'accept', 'accept-encoding'}}
        raw = json.dumps(entry, separators=(',', ':'), ensure_ascii=False)
        safe_line = hashlib.sha1(raw.encode()).hexdigest() + '\t' + raw
        index_lines.setdefault(dest_index, []).append(safe_line)
        evidence['npmEntries'] += 1
    except (OSError, ValueError):
        continue

for dest_index, lines in index_lines.items():
    dest_index.write_bytes(('\n' + '\n'.join(lines) + '\n').encode('utf-8'))

(ROOT / 'docs/stage3/d1/cache-evidence.json').write_text(json.dumps(evidence, indent=2) + '\n', encoding='utf-8')
print(f"Verified cache import: {len(evidence['nuget'])} NuGet packages; {evidence['npmEntries']} npm records")
