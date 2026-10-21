"""Freeze separate D2-A source/evidence; never SQL or secret input."""
from pathlib import Path
import json, hashlib, shutil, subprocess, datetime

ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
WORK=ROOT/'.local/d2a-phase2-candidate'
OUT=ROOT/'docs/stage3/d2a/closure/identities-20261018'
assert not OUT.exists(), 'Never overwrite a frozen increment'
sha=lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
subprocess.run(['python','tools/d2a-manifest.py','--verify'],cwd=ROOT,check=True)
assert sha(ROOT/'docs/stage3/d2a/candidate-manifest.json')=='66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f'
scope={'__file__':str(WORK/'tools/d2a-manifest.py')}
exec((WORK/'tools/d2a-manifest.py').read_text().split('if sys.argv')[0],scope)
C=OUT/'candidate'
for entry in scope['entries']():
    src=WORK/entry['path']; dst=C/entry['path']; dst.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(src,dst)
subprocess.run(['python',str(C/'tools/d2a-manifest.py')],cwd=ROOT,check=True)
E=OUT/'evidence'
shutil.copytree(HERE,E/'phase2',ignore=shutil.ignore_patterns('__pycache__'))
shutil.copyfile(ROOT/'docs/stage3/d2a/candidate-manifest.json',E/'original-candidate-manifest.json')
shutil.copytree(ROOT/'docs/stage3/d2a/closure',E/'original-closure',ignore=shutil.ignore_patterns('identities-20261018'))
shutil.copytree(ROOT/'docs/stage3/d2a/reports',E/'original-reports')
shutil.copyfile(ROOT/'docs/agent-onboarding/dba-engineer/sql-identities-20261018/engineer/PROPOSAL.md',E/'original-api-proposal.md')
entries=[{'path':p.relative_to(E).as_posix(),'sha256':sha(p)} for p in sorted(E.rglob('*')) if p.is_file()]
(E/'manifest.json').write_text(json.dumps({'kind':'Frozen nonsecret D2-A identity evidence; historical records remain historical','entries':entries},indent=2)+'\n',encoding='utf-8')
record={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'kind':'Separate D2-A SQL identity increment; human acceptance pending','candidate_manifest':'candidate/docs/stage3/d2a/candidate-manifest.json','candidate_sha256':sha(C/'docs/stage3/d2a/candidate-manifest.json'),'evidence_manifest':'evidence/manifest.json','evidence_sha256':sha(E/'manifest.json'),'original_sha256':sha(ROOT/'docs/stage3/d2a/candidate-manifest.json')}
(OUT/'freeze.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
(OUT/'freeze.sha256').write_text(sha(OUT/'freeze.json')+'  freeze.json\n',encoding='ascii')
(OUT/'independent-reports').mkdir()
print(json.dumps(record,indent=2))
