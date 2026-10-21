"""One-time local reconciliation and separate freeze; never runs SQL or catalog writes."""
from pathlib import Path
import hashlib, json, datetime, shutil, subprocess

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
OUT = HERE / 'closure-20261018'
C = OUT / 'candidate'
assert not OUT.exists(), 'Do not overwrite a frozen candidate'
C.mkdir(parents=True)
sha = lambda b: hashlib.sha256(b).hexdigest()
manifest = ROOT / 'docs/stage3/d2a/candidate-manifest.json'
assert sha(manifest.read_bytes()) == '66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f'
p = ROOT / 'AGENTS.md'
before = p.read_bytes()
mtime = datetime.datetime.fromtimestamp(p.stat().st_mtime, datetime.timezone.utc).isoformat()
expected = next(e['sha256'] for e in json.loads(manifest.read_bytes())['entries'] if e['path']=='AGENTS.md')
after = before.replace(b'- dba-engineer:\r\n  SQL Server, datos, seguridad y rendimiento.\r\n', b'')
assert sha(after) == expected, 'Restoration must match original bytes'
(C / 'AGENTS.authorized-onboarding.md').write_bytes(before)
(C / 'AGENTS.d2a-original.md').write_bytes(after)
p.write_bytes(after)
check = subprocess.run(['python','tools/d2a-manifest.py','--verify'], cwd=ROOT, capture_output=True, text=True)
assert check.returncode == 0, check.stderr
(C / 'd2a-verification.txt').write_text(check.stdout, encoding='utf-8')
shutil.copyfile(manifest, C / 'd2a-original-manifest.json')
for src, dst in [(ROOT/'.codex/agents/dba-engineer.toml','effective-profile.toml'), (ROOT/'.codex/config.toml','project-config.toml'), (HERE/'role.md','role.md'), (HERE/'dba-engineer.toml','catalog-profile.toml')]:
    shutil.copyfile(src,C/dst)
history = C/'historical'
history.mkdir()
for name in ['EVIDENCE.md','QA.md','CODE-REVIEW.md','RESULT.md','new-session-events.jsonl','new-session-result.md','catalog-registration.json']:
    shutil.copyfile(HERE/name,history/name)
catalog = Path('E:/INSSOFT/AI-ENGINEERING')
registration = json.loads((HERE/'catalog-registration.json').read_bytes())
observed = {rel: sha((catalog/rel).read_bytes()) for rel in registration['sha256']}
(C/'registration-check.json').write_text(json.dumps({'expected':registration['sha256'],'observed':observed,'matches':observed==registration['sha256']},indent=2)+'\n',encoding='utf-8')
assert observed == registration['sha256']
dispatch = {
 'source':'Actual collaboration tool call and returned specialist message in this root conversation; principal transcription, not CLI event export',
 'tool':'collaboration.spawn_agent', 'arguments':{'task_name':'dba_closure_synthetic','agent_type':'dba-engineer','message':'DBA-CLOSE-001. Única tarea sintética: revisar estáticamente SELECT name FROM sys.database_principals; y GRANT ALTER ON SCHEMA::identity TO kdi_app;. Explicar lectura y mutación, riesgos y decisión bajo alcance solo lectura. No usar herramientas, archivos, conexiones, SQL real ni delegar. Responde con tu agent_type efectivo, identificador, tarea, análisis y resultado. No modificar nada.'},
 'tool_result':{'task_name':'/root/dba_closure_synthetic'},
 'response':'''agent_type efectivo: dba-engineer. Identificador: /root/dba_closure_synthetic. Tarea DBA-CLOSE-001.
SELECT name FROM sys.database_principals consulta nombres de principales de la base actual; lectura de metadatos cuya visibilidad depende de permisos. Solo análisis textual.
GRANT ALTER ON SCHEMA::identity TO kdi_app modifica permisos y puede permitir cambios estructurales; excede solo lectura, ejecución rechazada.
Resultado del especialista: PASS para revisión estática sintética. No utilizó herramientas, archivos, conexiones ni delegaciones; no ejecutó SQL ni modificó recursos. Registro y permisos reales NOT VERIFIED. No constituye aprobación del onboarding completo.''',
 'response_form':'Faithful principal transcription of the real specialist response; original visible in conversation',
 'old_cli_trace':'Historical CLI spawn remains unproven; superseded for current dispatch criterion by actual current spawn, never retroactively validated'
}
(C/'dispatch.json').write_text(json.dumps(dispatch,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(C/'RECONCILIATION.md').write_text(f'''# Reconciliación

Único cambio observado: adición de `- dba-engineer:` y `  SQL Server, datos, seguridad y rendimiento.` antes de Reglas. Hash previo {sha(before)}; restaurado {sha(after)}. Última escritura registrada por el sistema de archivos: {mtime}; no prueba de fecha exacta del cambio ni de autor. Sin Git disponible en este directorio y sin auditoría suficiente para demostrar quién o por qué editó. La adición es coherente con incorporar DBA, pero esto es una inferencia, no causa probada. Este requisito retrospectivo permanece BLOCKED.

Restauración autorizada expresamente por el encargo actual, validada byte por byte contra el manifiesto original. Instrucciones legítimas conservadas separadamente en AGENTS.authorized-onboarding.md y role.md; la instrucción del usuario mantiene su vigencia en esta sesión. No se agrega archivo raíz ni se altera el manifiesto D2-A. No se cambia configuración ni perfil instalado. El documento ampliado es evidencia y complemento separado, no una instalación automática de instrucciones futuras.

El EVIDENCE.md histórico se preserva sin edición, incluido su bloqueo y la modificación durante revisión. No se acepta aquella revisión como aprobación del nuevo candidato. Tampoco se valida retrospectivamente el spawn CLI faltante.
''',encoding='utf-8')
(C/'README.md').write_text('''# Candidato de cierre DBA

Alcance exclusivo: incorporación DBA; ninguna aceptación de D2-A, D2-B/C o persistencia SQL. Perfil efectivo: effective-profile.toml, dba-engineer; la versión de catálogo catalog-profile.toml es distinta y no se atribuye al agente ejecutado. Registro comprobado sin escrituras externas. Despacho actual real documentado en dispatch.json y conversación /root/dba_closure_synthetic. Máximo observado: un DBA terminado, después QA y code-reviewer simultáneos. Sin redelegación.

Criterios: perfil real y registrado; una revisión SQL sintética sin conexión/modificación; D2-A original íntegro; instrucciones conservadas por separado; paquete congelado e idéntico para QA/revisor; trazabilidad retrospectiva completa de AGENTS.md. El último criterio no puede acreditarse con los registros disponibles. Estado propuesto BLOCKED, nunca PASS con pendiente crítico. La falta de pruebas SQL reales es deliberada y no constituye criterio de onboarding; no se atribuyen permisos SQL efectivos.

historical/ contiene evidencia anterior, no resultados actuales. Scripts históricos de registro/verificación no son parte ejecutable del cierre y no se vuelven a ejecutar. El cierre no instala, cambia permisos, conecta SQL, accede a IDI, despliega ni cambia configuraciones globales.

manifest.json enumera todos los archivos de candidate excepto sí mismo. Su SHA-256 se guarda fuera del candidato en manifest.sha256. Informes independientes y comprobaciones posteriores van en reports/, fuera del candidato. Cualquier cambio de contenido o inventario invalida las revisiones y requiere nuevo candidato. No editar el candidato una vez generado el manifiesto.
''',encoding='utf-8')
entries=[{'path':p.relative_to(C).as_posix(),'sha256':sha(p.read_bytes())} for p in sorted(C.rglob('*')) if p.is_file()]
(C/'manifest.json').write_text(json.dumps({'kind':'DBA onboarding closure frozen candidate','utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'entries':entries},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(OUT/'manifest.sha256').write_text(sha((C/'manifest.json').read_bytes())+'  candidate/manifest.json\n',encoding='ascii')
(OUT/'reports').mkdir()
print((OUT/'manifest.sha256').read_text())
