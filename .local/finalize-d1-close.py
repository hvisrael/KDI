import hashlib, json, shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1]
d=root/'docs/stage3/d1'; c=d/'closure'
def readlog(name):
    raw=(c/name).read_bytes()
    return raw.decode('utf-16') if raw.startswith((b'\xff\xfe', b'\xfe\xff')) else raw.decode('utf-8-sig')
log=readlog('d1-close-tests-final.log')
counts={name:log.count('correcto Kulani.KDI.'+name+'.') for name in ['UnitTests','ApiTests','ArchitectureTests']}
assert sum(counts.values())==40, counts
assert 'error: 0' in log and 'omitido: 0' in log
shutil.copy2(d/'implementation-manifest.json',c/'candidate-manifest.json')
shutil.copy2(d/'validation-summary.json',c/'historical-validation-summary.json')
manifestHash=hashlib.sha256((d/'implementation-manifest.json').read_bytes()).hexdigest()
summary={'candidateHash':manifestHash,'status':'PENDING_INDEPENDENT_REVIEW','scope':'D1 only; human acceptance pending','tests':{'releaseBuild':'PASS, zero warnings/errors','xunit':{'status':'PASS','total':40,'failed':0,'skipped':0,'bySuite':counts,'internalErrorRedaction':'PASS'},'httpSmoke':'PASS, eight groups','structure':'PASS','typecheck':'PASS','lint':'PASS','webBuild':'PASS','playwright':'PASS, five tests','nugetAudit':'PASS, all packages, no warnings','npmAudit':'PASS after authorized compatible fix, zero vulnerabilities','manifest':'PASS, exhaustive coverage under declared scope, 87 entries'},'independentReports':{'qa':'pending','codeReview':'pending'},'limitations':['SDK/runtime 10.0.103/10.0.3 not updated or certified current','Audits are point-in-time package checks, not runtime security certification','No business modules, SQL, identity, CI or deployment','Pre-existing central versions/test locks cannot be attributed from available history']}
(d/'validation-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
result=d/'RESULT.md'
old=result.read_text(encoding='utf-8')
result.write_text('# Resultado D1 — candidato corregido, revisión independiente en curso\n\nLa corrida vigente de cierre está en [closure/SUMMARY.md](closure/SUMMARY.md) y validation-summary.json: build Release, 40/40 xUnit (incluida redacción de excepción), smoke HTTP, arquitectura, typecheck, lint, build web y 5/5 Playwright pasaron. Auditorías de paquetes completadas; npm terminó con cero vulnerabilidades tras corrección compatible autorizada. NuGet ya no está bloqueado. Las limitaciones y revisiones independientes se registran en closure/. Aceptación humana pendiente; D2 no autorizado.\n\nEl contenido siguiente es HISTÓRICO y describe la primera entrega, no el estado de las pruebas del candidato de cierre.\n\n'+old,encoding='utf-8')
