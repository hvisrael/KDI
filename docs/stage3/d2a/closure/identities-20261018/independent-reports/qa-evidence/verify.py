from pathlib import Path
import hashlib,json,shutil,runpy
R=Path.cwd(); B=R/'docs/stage3/d2a/closure/identities-20261018'; O=R/'.local/d2a-phase2-qa'
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def check(root,manifest,inventory=False):
 m=json.loads(manifest.read_text(encoding='utf-8-sig')); bad=[e['path'] for e in m['entries'] if not (root/e['path']).is_file() or h(root/e['path'])!=e['sha256']]; assert not bad,bad
 if inventory: assert {e['path'] for e in m['entries']}=={p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and p!=manifest}
 return len(m['entries'])
c=B/'candidate'; e=B/'evidence'; cm=c/'docs/stage3/d2a/candidate-manifest.json'; em=e/'manifest.json'
assert h(cm)=='c31fe8c14872ff88b6d0d3882f4585b36e6a3798788108ecdd8539841a312973'
assert h(em)=='80ff0d9ac23386569e7c620d237a6af401ba6b48dd5f9447859ba3090e646d68'
print('Candidate entries',check(c,cm),'Evidence entries',check(e,em,True))
orig=e/'original-candidate-manifest.json'; print('Original root entries',check(R,orig),h(orig))
actual={p.relative_to(c).as_posix() for p in c.rglob('*') if p.is_file() and p!=cm}; assert actual=={x['path'] for x in json.loads(cm.read_text())['entries']}
s=O/'source'; shutil.copytree(c,s,dirs_exist_ok=True); print('Copy verified',check(s,s/'docs/stage3/d2a/candidate-manifest.json'))
mod=runpy.run_path(str(e/'phase2/runtime/connected-smoke.py')); checks=mod['run'](5088); print('Independent connected smoke',len(checks),checks)
(O/'integrity-http.json').write_text(json.dumps({'candidateHash':h(cm),'evidenceHash':h(em),'originalHash':h(orig),'smoke':checks,'status':'PASS'},indent=2))
