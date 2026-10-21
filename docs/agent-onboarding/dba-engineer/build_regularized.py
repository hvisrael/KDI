"""Freeze local regularized onboarding only; no external writes or SQL."""
from pathlib import Path
import hashlib, json, shutil, subprocess, datetime, tomllib

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent/'regularized-20261018'
C=OUT/'candidate'
assert not OUT.exists(), 'Never overwrite a frozen candidate'
C.mkdir(parents=True)
(OUT/'reports').mkdir()
sha=lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
def put(name,text): (C/name).write_text(text+'\n',encoding='utf-8')
for source,name in [('.codex/agents/dba-engineer.toml','profile.toml'),('.codex/config.toml','project-config.toml'),('AGENTS.md','AGENTS.d2a-original.md'),('docs/stage3/d2a/candidate-manifest.json','d2a-original-manifest.json')]:
    shutil.copyfile(ROOT/source,C/name)
catalog=Path('E:/INSSOFT/AI-ENGINEERING')
for source,name in [('roles/dba-engineer.md','catalog-role.md'),('codex/candidates/agents/dba-engineer.toml','catalog-profile.toml')]:
    shutil.copyfile(catalog/source,C/name)
assert tomllib.loads((C/'profile.toml').read_text(encoding='utf-8-sig'))['name']=='dba-engineer'
assert sha(C/'d2a-original-manifest.json')=='66ee6991b31df99bcb96ae00069a8b89beba85d84433fb0ebacdd35277e9a33f'
r=subprocess.run(['python','tools/d2a-manifest.py','--verify'],cwd=ROOT,capture_output=True,text=True,check=True)
put('d2a-verification.txt',r.stdout.rstrip())
put('HUMAN-DECLARATION.md','''# Declaración de procedencia humana

Fuente: mensaje directo del usuario Human Orchestrator en esta conversación, recibido antes de generar esta línea base. Transcripción literal:

Como Human Orchestrator, confirmo que incorporé manualmente el perfil TOML de dba-engineer en KDI y modifiqué AGENTS.md para incluirlo en el equipo de especialistas.

Reconozco esas modificaciones como parte de la incorporación autorizada del agente DBA.

Esta declaración no certifica una fecha exacta de edición ni sustituye los registros técnicos que no existen.

La declaración acredita procedencia humana declarada y autorización; no constituye un evento técnico de edición ni certificación temporal.''')
put('HISTORICAL-LIMITATION.md','''# Limitación histórica separada

No existe trazabilidad técnica histórica completa de AGENTS.md. Su última escritura observada fue 2026-10-19T02:24:57 UTC; no acredita cuándo exactamente se añadieron las dos líneas DBA. La procedencia humana está declarada en HUMAN-DECLARATION.md; no se atribuyen otros autores, fechas ni eventos.

El AGENTS.md raíz continúa restaurado byte por byte al candidato D2-A. La versión ampliada y la diferencia permanecen preservadas en ../closure-20261018/candidate/ (ruta desde el directorio regularized-20261018). La instrucción humana actual autoriza DBA y esta regularización sin modificar AGENTS raíz.

El despacho DBA anterior fue real en la conversación del principal; los revisores anteriores solo recibieron transcripción. No se certifica retrospectivamente el spawn CLI faltante. Ningún revisor actual debe certificar eventos históricos que no observó. Por decisión humana explícita esta limitación no bloquea por sí sola la línea base técnica actual. No se borra ni reescribe el paquete anterior BLOCKED.''')
put('CHANGES.md','''# Correcciones actuales

Se preservan las responsabilidades y reglas previas del TOML instalado y se agregan controles explícitos: TaskInput 1.0 obligatorio; presupuesto/tiempo/intentos/herramientas; no redelegación con excepción solo por nueva autorización humana expresa; máximo dos especialistas; candidato congelado inmutable y revisión repetida ante cambios; privilegio mínimo y ausencia de concesión de ACL/credenciales; autorización humana expresa y específica para CREATE LOGIN, ALTER ROLE, GRANT, REVOKE, usuarios, DDL y migraciones; autorización específica para DML/backups/restores/conexiones; ningún secreto en código/logs/evidencia; escalamiento de conflictos y presupuesto; TaskResult, evidencia y separación de QA/revisión.

Comparación: catalog-profile.toml y catalog-role.md son copias de lectura de AI-ENGINEERING. El perfil KDI mantiene responsabilidades del instalado y adopta sus controles; la excepción de redelegación se limita a nueva autorización humana, conforme al encargo actual de prohibir redelegación no autorizada. El catálogo dice no redelegar; no hay autorización activa para excepción. Los controles adicionales de SQL y secretos son más explícitos, no privilegios nuevos. No se escribe al catálogo ni se cambian configuraciones globales/proyecto.

El archivo instalado .codex/agents/dba-engineer.toml debe coincidir exactamente con profile.toml. La disponibilidad como tipo real será observada mediante despacho actual separado; cargar perfil revisado dentro del runtime no se presume por editar el archivo. La corrección se verifica por el archivo instalado y las comprobaciones independientes.''')
put('README.md','''# Línea base regularizada DBA

Alcance: onboarding del perfil dba-engineer. Criterios actuales: sintaxis TOML válida; disponibilidad como agent_type real por herramienta de despacho; correspondencia de instrucciones con TaskInput, presupuesto, límites, seguridad SQL/secretos y escalamiento; coherencia con catálogo; archivo instalado idéntico; integridad del manifiesto y candidato; D2-A idéntico. QA y Code Review independientes revisarán este mismo hash y entregarán fuera del candidato. No se exige reconstruir eventos históricos inexistentes ni SQL real.

Perfil: profile.toml. Documentos de referencia: catalog-role.md y catalog-profile.toml. Declaración y limitación histórica separadas. candidate/manifest.json incluye todos los archivos salvo sí mismo; su hash se guarda fuera en manifest.sha256. Cualquier cambio de contenido/inventario invalida revisión y exige nuevo candidato. Informes, prueba de disponibilidad y decisión final se guardan fuera, bajo reports/; no son parte de contenido congelado ni sustituyen controles del perfil.

Restricciones vigentes: sin bases, SQL, conexiones, logins, permisos SQL, IDI, D2-B/C, despliegue o configuraciones globales. Máximo dos especialistas activos. PASS técnico de onboarding permite recomendar aceptación humana; no es aceptación humana automática ni habilita persistencia real. Limitación retrospectiva documentada no equivale a prueba técnica bloqueada actual.''')
entries=[{'path':p.relative_to(C).as_posix(),'sha256':sha(p)} for p in sorted(C.rglob('*')) if p.is_file()]
(C/'manifest.json').write_text(json.dumps({'kind':'Regularized DBA onboarding frozen baseline','frozen_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'entries':entries},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(OUT/'manifest.sha256').write_text(sha(C/'manifest.json')+'  candidate/manifest.json\n',encoding='ascii')
print((OUT/'manifest.sha256').read_text())
