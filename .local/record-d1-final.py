import json, hashlib
from pathlib import Path
r=Path(__file__).resolve().parents[1]; d=r/'docs/stage3/d1'; c=d/'closure'
p=d/'validation-summary.json'; data=json.loads(p.read_text())
data['status']='PASS'
data['independentReports']={'qa':{'profile':'qa-engineer','id':'/root/d1_closure_qa','status':'PASS','report':'closure/QA.md'},'codeReview':{'profile':'code-reviewer','id':'/root/d1_closure_review','status':'PASS','report':'closure/CODE-REVIEW.md'}}
data['humanAcceptance']='PENDING; recommendation limited to D1; D2 not authorized'
p.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
p=d/'RESULT.md'; s=p.read_text(encoding='utf-8').replace('candidato corregido, revisión independiente en curso','PASS técnico; aceptación humana pendiente',1)
s=s.replace('La corrida vigente de cierre','QA independiente /root/d1_closure_qa y Code Review independiente /root/d1_closure_review emitieron PASS para el candidato. La corrida vigente de cierre',1)
p.write_text(s,encoding='utf-8')
p=c/'SUMMARY.md'; s=p.read_text(encoding='utf-8')
s+='\n## Dictamen final\n\nPASS técnico D1. QA `/root/d1_closure_qa` y Code Review `/root/d1_closure_review` emitieron PASS sobre el mismo SHA-256; informes QA.md y CODE-REVIEW.md. Aceptación humana pendiente. STATUS.md conserva preparación histórica y no representa el dictamen de este candidato. validation-summary.json y RESULT.md contienen el estado vigente; dependencies.json y failed-api-tests-restore.lock.json conservan el inventario/intento histórico, no la resolución actual de paquetes, que se obtiene de los locks congelados.\n\nSeguimientos no bloqueantes: SDK/runtime sin actualización/certificación de parche vigente (QA P2 operativo) y fixture negativo completo de ArchitecturePolicy.Check (Code Review P3). Ninguno autoriza producción o D2.\n'
p.write_text(s,encoding='utf-8')
paths=[p for p in c.rglob('*') if p.is_file() and p.name!='evidence-manifest.json']
paths.extend(d/name for name in ['RESULT.md','validation-summary.json','TOOLCHAIN.md','api-smoke-result.json','architecture-result.json','playwright-result.json','implementation-manifest.json'])
e={'kind':'d1-closure-evidence','candidateHash':data['candidateHash'],'entries':[{'path':p.relative_to(r).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(paths)]}
p=c/'evidence-manifest.json'; p.write_text(json.dumps(e,indent=2)+'\n',encoding='utf-8')
print('evidence manifest SHA256',hashlib.sha256(p.read_bytes()).hexdigest())
print('xUnit counts',data['tests']['xunit']['bySuite'])
