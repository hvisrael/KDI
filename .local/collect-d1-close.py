import hashlib, json, shutil, runpy
from pathlib import Path
root = Path(__file__).resolve().parents[1]
out = root / 'docs/stage3/d1/closure'
out.mkdir(exist_ok=True)
old = json.loads((root/'docs/stage3/d1/implementation-manifest.json').read_text())
(out/'previous-manifest.json').write_text(json.dumps(old, indent=2)+'\n')
ns = runpy.run_path(str(root/'tools/content-manifest.py'), run_name='manifest_import') if False else None
for p in (root/'.local').glob('d1-close-*'):
    if p.is_file(): shutil.copy2(p, out/p.name)
changes=[]
for e in old['entries']:
    p=root/e['path']
    if not p.exists() or hashlib.sha256(p.read_bytes()).hexdigest()!=e['sha256']:
        changes.append(e['path'])
(out/'changes-from-previous.json').write_text(json.dumps({'modifiedAgainstPreviousManifest':changes,'additionalTestLocks':[str(p.relative_to(root)).replace('\\','/') for p in (root/'tests').glob('*/packages.lock.json')], 'provenance':'Test locks existed before this correction run. NuGet metadata identifies nuget.org and project cache recorded success; no Git metadata or command transcript establishes who created them. Central versions also pre-existed this run. Current locked restore succeeded.'},indent=2)+'\n')
