"""Read-only verification of both frozen artifacts and original D2-A."""
from pathlib import Path
import hashlib,json,subprocess,sys
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[4]
sha=lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
assert sha(HERE/'freeze.json')==(HERE/'freeze.sha256').read_text().split()[0]
record=json.loads((HERE/'freeze.json').read_bytes())
for key in ['candidate','evidence']:
    assert sha(HERE/record[key+'_manifest'])==record[key+'_sha256'],key
E=HERE/'evidence'
m=E/'manifest.json'
actual=[{'path':p.relative_to(E).as_posix(),'sha256':sha(p)} for p in sorted(E.rglob('*')) if p.is_file() and p!=m]
assert actual==json.loads(m.read_bytes())['entries'],'Frozen evidence changed'
assert sha(ROOT/'docs/stage3/d2a/candidate-manifest.json')==record['original_sha256']
subprocess.run([sys.executable,str(ROOT/'tools/d2a-manifest.py'),'--verify'],cwd=ROOT,check=True)
subprocess.run([sys.executable,str(HERE/'candidate/tools/d2a-manifest.py'),'--verify'],cwd=ROOT,check=True)
j=E/'phase2/sql-v2/execution-20261019T031842176Z.json'
events=json.loads(j.read_text(encoding='utf-8-sig'))
for op,count in [('CREATE LOGIN',3),('CREATE USER',5),('ALTER ROLE ADD MEMBER',5)]:
    assert sum(x['operation']==op and x['result']=='COMMITTED' for x in events)==count
assert sum(x['operation']=='SQL AUTH / READONLY PERMISSION OBSERVATION' for x in events)==5
assert any(x['identity']=='kdi_test_runner' and x['database']=='KulaniKDI_Dev' and x['result']=='EXPECTED_DEV_ACCESS_DENIED' and x['sqlNumber']==4060 for x in events)
assert json.loads((E/'phase2/runtime/connected-smoke-result.json').read_text())['status']=='PASS'
print('Original D2-A, frozen increment and frozen evidence verified; 13 commits, five direct authentications and expected Dev denial recorded')
print('Historical NULL query is supplemented separately; this command is integrity verification, not human acceptance')
