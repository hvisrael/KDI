import datetime as dt
import hashlib
import json
import re
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
target = root/'docs/stage3/d1/acceptance'
expected = 'e2bb22b739b5924e8c2ead3af421e56ac97dc6e6b0209a7d0b7b5da8d1f51193'
manifest = root/'docs/stage3/d1/implementation-manifest.json'
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
assert sha(manifest) == expected, 'Approved manifest changed'
check = subprocess.run(['python','tools/content-manifest.py','--verify'], cwd=root, capture_output=True, text=True, check=True)
evidence = root/'docs/stage3/d1/closure/evidence-manifest.json'
ev = json.loads(evidence.read_text(encoding='utf-8'))
assert ev['candidateHash'] == expected
for entry in ev['entries']:
    path = (root/entry['path']).resolve()
    assert path.is_relative_to(root)
    assert sha(path) == entry['sha256'], f'Evidence changed: {entry["path"]}'
for name in ['QA.md','CODE-REVIEW.md']:
    assert expected in (root/'docs/stage3/d1/closure'/name).read_text(encoding='utf-8')
now = dt.datetime.now(dt.timezone.utc).isoformat(timespec='seconds').replace('+00:00','Z')
record = {
    'schemaVersion':'1.0','projectId':'kdi','taskId':'kdi-stage3-d1-001',
    'humanActor':'Human Orchestrator','timestampUtc':now,'decision':'APPROVED',
    'action':'Aceptar y cerrar incremento D1 exclusivamente para el candidato técnico evaluado: fundación técnica, correcciones arquitectónicas, evidencia y manifiesto de cierre.',
    'environment':'E:/INSSOFT/KDI; candidato técnico local D1; sin despliegue a producción',
    'candidateHash':expected,
    'conditions':[
        'Aprobación limitada a docs/stage3/d1/implementation-manifest.json y su candidato exacto.',
        'Seguimientos no bloqueantes: verificación operativa de actualización SDK/runtime y fixture negativo adicional de arquitectura.',
        'No desplegar a producción; no modificar ni acceder a IDI; no iniciar D2 automáticamente.',
        'D2 requiere revisión y autorización humana expresa; D3 y aprobación íntegra de ADR permanecen fuera de alcance.',
        'timestampUtc corresponde al registro de esta decisión; no se inventa la hora exacta del mensaje humano.'
    ],
    'sourceReference':'docs/stage3/d1/acceptance/HUMAN-APPROVAL.md; mensaje humano de esta conversación: KDI — ACEPTACIÓN HUMANA DEL INCREMENTO D1'
}
schema_path=Path('E:/INSSOFT/AI-ENGINEERING/contracts/approval-record.schema.json')
schema=json.loads(schema_path.read_text(encoding='utf-8-sig'))
# Bounded validation of every keyword used in this specific closed contract.
# No claim of a general Draft 2020-12 validator; no dependency installation.
def validate(value, s):
    if 'anyOf' in s:
        for branch in s['anyOf']:
            try: validate(value, branch); return
            except AssertionError: pass
        raise AssertionError('No anyOf branch matched')
    types={'object':dict,'string':str,'array':list,'null':type(None)}
    if 'type' in s: assert isinstance(value,types[s['type']])
    if 'const' in s: assert value == s['const']
    if 'enum' in s: assert value in s['enum']
    if isinstance(value,dict):
        assert set(s.get('required',[])) <= value.keys()
        if s.get('additionalProperties') is False: assert value.keys() <= s['properties'].keys()
        for k,v in value.items(): validate(v,s['properties'][k])
    if isinstance(value,str):
        assert len(value)>=s.get('minLength',0) and len(value)<=s.get('maxLength',float('inf'))
        if 'pattern' in s: assert re.search(s['pattern'],value)
        if s.get('format')=='date-time':
            assert dt.datetime.fromisoformat(value.replace('Z','+00:00')).utcoffset()==dt.timedelta(0)
    if isinstance(value,list):
        assert len(value)>=s.get('minItems',0) and len(value)<=s.get('maxItems',float('inf'))
        for item in value: validate(item,s['items'])
validate(record,schema)
assert not (target/'approval-record.json').exists(), 'Do not overwrite existing human approval'
(target/'approval-record.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
status={'projectId':'kdi','taskId':record['taskId'],'state':'COMPLETED','technicalResult':'PASS','humanDecision':'APPROVED','candidateHash':expected,'recordedAtUtc':now,'approvalRecord':'docs/stage3/d1/acceptance/approval-record.json','D2':'PROPOSED_NOT_AUTHORIZED_NOT_STARTED','D3':'PENDING','productionDeployment':'NOT_AUTHORIZED','historicalEvidence':'Preserved unchanged; prior pending-human labels superseded only for this candidate by this acceptance.'}
(target/'STATUS.json').write_text(json.dumps(status,indent=2)+'\n',encoding='utf-8')
verification={'verifiedAtUtc':now,'candidateHash':expected,'candidateVerification':check.stdout.strip(),'evidenceManifest':str(evidence.relative_to(root)).replace('\\','/'),'evidenceManifestSha256':sha(evidence),'evidenceEntriesVerified':len(ev['entries']),'contract':str(schema_path),'contractSha256':sha(schema_path),'contractValidation':'All used schema constraints checked locally; bounded validator, not general Draft 2020-12 engine. jsonschema unavailable; no installation.','reports':['docs/stage3/d1/closure/QA.md','docs/stage3/d1/closure/CODE-REVIEW.md'],'records':[{'path':p.relative_to(root).as_posix(),'sha256':sha(p)} for p in sorted(target.iterdir()) if p.is_file()]}
(target/'verification.json').write_text(json.dumps(verification,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'state':status['state'],'candidateHash':expected,'approvalRecordSha256':sha(target/'approval-record.json'),'evidenceEntriesVerified':len(ev['entries']),'schemaValidation':'PASS bounded contract validation'},indent=2))
