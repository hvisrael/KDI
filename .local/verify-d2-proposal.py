import datetime as dt
import hashlib
import json
import subprocess
from pathlib import Path

r=Path(__file__).resolve().parents[1]
d=r/'docs/stage3/d2-proposal'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
expected='e2bb22b739b5924e8c2ead3af421e56ac97dc6e6b0209a7d0b7b5da8d1f51193'
assert sha(r/'docs/stage3/d1/implementation-manifest.json')==expected
check=subprocess.run(['python','tools/content-manifest.py','--verify'],cwd=r,capture_output=True,text=True,check=True)
evidence=json.loads((r/'docs/stage3/d1/closure/evidence-manifest.json').read_text(encoding='utf-8'))
acceptance=json.loads((r/'docs/stage3/d1/acceptance/verification.json').read_text(encoding='utf-8'))
for e in evidence['entries']+acceptance['records']:
    assert sha(r/e['path'])==e['sha256'],e['path']
assert sha(r/'docs/stage3/d1/closure/evidence-manifest.json')==acceptance['evidenceManifestSha256']
files=sorted(d.glob('*.md'))
assert len(files)==5, [p.name for p in files]
record={'status':'PROPOSED_AWAITING_HUMAN_DECISION','implementationStatus':'NOT_STARTED','createdAtUtc':dt.datetime.now(dt.timezone.utc).isoformat(timespec='seconds').replace('+00:00','Z'),'approvedD1Hash':expected,'D1Verification':check.stdout.strip(),'D1EvidenceAndAcceptancePreserved':True,'specialists':[{'agent_type':'analyst-architect','id':'/root/d2_functional_proposal','task':'Functional rules, organizations, permissions and acceptance criteria','result':'Documentary proposal completed; human decisions pending'},{'agent_type':'engineer','id':'/root/d2_technical_proposal','task':'SQL persistence, migrations, transactions, OIDC and technical contracts design','result':'Documentary proposal completed; no implementation'}],'proposalEntries':[{'path':p.relative_to(r).as_posix(),'sha256':sha(p)} for p in files],'executionEstimatesHoursAgent':{'D2-A':[16,27],'D2-B':[30,51],'D2-C':[42,70]},'limitations':['Only document preparation authorized','No product tests, restores or SQL/IdP connections executed','Infrastructure unconfirmed; budgets are unapproved estimates','No independent QA/code-review of product in this preparation']}
p=d/'proposal-manifest.json';p.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(check.stdout.strip())
print('PASS preservation of existing D1 evidence and human acceptance')
print('Proposal document count:',len(files))
print('Proposal manifest SHA256:',sha(p))
