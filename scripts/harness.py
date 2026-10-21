#!/usr/bin/env python3
"""Small fail-closed Codex orchestration runner; Python standard library only."""
import argparse, datetime, hashlib, json, os, pathlib, re, shutil, subprocess, sys, uuid
BASE=pathlib.Path(__file__).resolve().parents[1]
ROLES=['analyst-architect','engineer','qa-engineer','code-reviewer']
FORBIDDEN={'idi','.env','secrets'}

def cmd(argv,cwd,timeout=1800):
    return subprocess.run(argv,cwd=str(cwd),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=timeout)

def git_snapshot(root):
    p=cmd(['git','status','--porcelain=v1','--untracked-files=all'],root)
    if p.returncode: raise ValueError('Git status unavailable: '+p.stderr)
    return p.stdout

def git_files(root):
    p=cmd(['git','status','--porcelain=v1','--untracked-files=all'],root)
    if p.returncode: raise ValueError('Git diff unavailable')
    out=[]
    for line in p.stdout.splitlines():
        path=line[3:].split(' -> ')[-1]
        out.append(path.replace('\\','/'))
    return out

def check_path(s):
    p=pathlib.PurePosixPath(s.replace('\\','/'))
    if not s or p.is_absolute() or '..' in p.parts or ':' in s or any(part.lower() in FORBIDDEN for part in p.parts):
        raise ValueError('Rejected path: '+s)
    return str(p)

def validate_task(task):
    keys=('id','title','objective','acceptance_criteria','allowed_paths','test_commands','risk_level','human_approved')
    if any(k not in task for k in keys): raise ValueError('Missing required task fields')
    if not isinstance(task['human_approved'],bool): raise ValueError('human_approved must be boolean')
    if task['risk_level'] not in ('low','medium','high'): raise ValueError('Invalid risk_level')
    if not task['allowed_paths'] or not task['acceptance_criteria']: raise ValueError('Scope and acceptance criteria are required')
    for p in task['allowed_paths']: check_path(p)
    for c in task['test_commands']:
        if not isinstance(c,list) or not c or not all(isinstance(x,str) and x for x in c): raise ValueError('test_commands must be argument lists')
        if pathlib.Path(c[0]).name.lower() in ('powershell','pwsh','cmd','bash','sh','sudo','sqlcmd','docker','kubectl','az','terraform'): raise ValueError('Shell/privileged runner not allowed as test command')
    if not task['test_commands']: raise ValueError('At least one real test command required')

def within_allowed(file,paths):
    file=file.replace('\\','/').strip('/')
    return any(file==p.rstrip('/') or file.startswith(p.rstrip('/')+'/') for p in paths)

def load_json(p): return json.loads(pathlib.Path(p).read_text(encoding='utf-8'))

def report(run, state, reason):
    state['status']=state.get('status','BLOCKED');state['reason']=reason
    (run/'manifest.json').write_text(json.dumps(state,indent=2,ensure_ascii=False),encoding='utf-8')
    print(json.dumps({'run':str(run),'status':state['status'],'reason':reason},ensure_ascii=False))

def doctor(root):
    print('Project:',root)
    for name in ('python','git','codex'):
        print(name,shutil.which(name) or 'NOT FOUND')
    print('Git root:',cmd(['git','rev-parse','--show-toplevel'],root).stdout.strip())
    print('Existing change paths:',git_files(root))
    print('Codex version:',cmd(['codex','--version'],root,30).stdout.strip() if shutil.which('codex') else 'not installed')
    print('Multi-agent spawn support: NOT TESTED; this harness invokes independent CLI processes')

def run(args):
    root=pathlib.Path(args.project).resolve(strict=True)
    if 'idi' in {x.lower() for x in root.parts}: raise ValueError('IDI repository is forbidden')
    gr=cmd(['git','rev-parse','--show-toplevel'],root)
    if gr.returncode or pathlib.Path(gr.stdout.strip()).resolve()!=root: raise ValueError('Project must be Git repository root')
    task=load_json(args.task);validate_task(task)
    if args.execute and not shutil.which('codex'): raise ValueError('Codex CLI not found')
    if args.execute and (not args.authorize_write or not task['human_approved']):
        raise ValueError('Engineering writes require BOTH human_approved=true in task and --authorize-write')
    if task['risk_level']=='high': raise ValueError('High risk tasks require a separate approved workflow')
    prior=git_files(root)
    if prior: raise ValueError('Use a clean Git worktree. Existing changes: '+', '.join(prior[:10]))
    rid=datetime.datetime.now().strftime('%Y%m%dT%H%M%S')+'-'+uuid.uuid4().hex[:6]
    rundir=BASE/'runs'/rid;rundir.mkdir(parents=True)
    state={'task_id':task['id'],'project':str(root),'run_id':rid,'status':'BLOCKED','execution_mode':'isolated_cli_role','effective_agent_type':'not_applicable','max_parallel':2,'effective_parallel':1,'agents':[],'tests':[],'deployed':False,'merge_performed':False}
    (rundir/'task.json').write_text(json.dumps(task,ensure_ascii=False,indent=2),encoding='utf-8')
    if args.dry_run or not args.execute:
        (rundir/'plan.json').write_text(json.dumps({'roles':ROLES,'sandbox':['read-only','workspace-write','read-only','read-only'],'tests':task['test_commands']},indent=2),encoding='utf-8')
        return report(rundir,state,'DRY_RUN: no agents or tests executed; no PASS')
    (rundir/'git-before.txt').write_text(git_snapshot(root),encoding='utf-8')
    schema=BASE/'contracts'/'result.schema.json'
    context='Task:\n'+json.dumps(task,ensure_ascii=False,indent=2)+'\nProject: '+str(root)+'\n'
    for role in ROLES:
        snapshot_before=git_snapshot(root)
        prompt=(BASE/'roles'/f'{role}.md').read_text(encoding='utf-8')+'\n'+context+'\nPrior verified outputs:\n'
        for previous in state['agents']:
            prompt+=json.dumps({'role':previous['role'],'status':previous['status'],'summary':previous.get('summary','')},ensure_ascii=False)+'\n'
        resultfile=rundir/f'{role}.json'
        call=['codex','exec','--sandbox',('workspace-write' if role=='engineer' else 'read-only'),'--output-schema',str(schema),'--output-last-message',str(resultfile),'--json','-']
        try:
            result=subprocess.run(call,input=prompt,cwd=str(root),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=1800)
        except subprocess.TimeoutExpired:
            return report(rundir,state,role+' TIMEOUT')
        (rundir/f'{role}.jsonl').write_text(result.stdout,encoding='utf-8')
        (rundir/f'{role}.stderr.txt').write_text(result.stderr,encoding='utf-8')
        record={'role':role,'process_returncode':result.returncode,'execution_mode':'isolated_cli_role','status':'BLOCKED','output':str(resultfile.relative_to(BASE))}
        try:
            obj=load_json(resultfile)
            if not isinstance(obj,dict) or set(obj)!={'role','status','summary','evidence','findings','changed_files'}: raise ValueError('Unexpected result shape')
            if obj['role']!=role or obj['status'] not in ('PASS','FAIL','BLOCKED'): raise ValueError('Invalid role or status')
            if not isinstance(obj['summary'],str) or not all(isinstance(obj[k],list) and all(isinstance(i,str) for i in obj[k]) for k in ('evidence','findings','changed_files')): raise ValueError('Invalid field types')
            record.update(status=obj['status'],summary=obj['summary'],evidence=obj['evidence'],findings=obj['findings'])
        except (OSError,ValueError,TypeError,json.JSONDecodeError) as exc:
            record['error']='Invalid structured output: '+str(exc)
        snapshot_after=git_snapshot(root)
        changed=git_files(root)
        if role!='engineer' and snapshot_before!=snapshot_after:
            record['status']='FAIL';record['error']='Observer modified Git-visible candidate'
        if role=='engineer' and any(not within_allowed(f,task['allowed_paths']) for f in changed):
            record['status']='FAIL';record['error']='Detected out-of-scope Git-visible changes'
        state['agents'].append(record)
        if result.returncode!=0 or record['status']!='PASS' or not record.get('evidence'):
            state['status']='FAIL' if result.returncode!=0 or record['status']=='FAIL' else 'BLOCKED'
            return report(rundir,state,role+' gate did not PASS with evidence')
    # Real tests are run only after the independent reviews succeed.
    for i,argv in enumerate(task['test_commands'],1):
        before=git_snapshot(root)
        try: p=cmd(argv,root,timeout=1200)
        except (subprocess.TimeoutExpired,OSError) as exc:
            state['status']='BLOCKED';return report(rundir,state,'Test could not run: '+str(exc))
        (rundir/f'test-{i}.stdout.txt').write_text(p.stdout,encoding='utf-8')
        (rundir/f'test-{i}.stderr.txt').write_text(p.stderr,encoding='utf-8')
        after=git_snapshot(root)
        state['tests'].append({'command':argv,'exit_code':p.returncode,'git_visible_change':before!=after})
        if p.returncode!=0:
            state['status']='FAIL';return report(rundir,state,'Test failed: '+str(argv))
        if before!=after:
            state['status']='BLOCKED';return report(rundir,state,'Test changed Git-visible files; manual evaluation required')
    # All checks completed: this is technical PASS, not human authorization for merge or release.
    state['status']='PASS'
    return report(rundir,state,'Technical gates passed; merge/release/deployment NOT authorized')

def main():
    parser=argparse.ArgumentParser(description='AI Engineering harness')
    sub=parser.add_subparsers(dest='command',required=True)
    d=sub.add_parser('doctor');d.add_argument('--project',required=True)
    t=sub.add_parser('init-task');t.add_argument('--project',required=True);t.add_argument('--title',required=True);t.add_argument('--out',required=True)
    r=sub.add_parser('run');r.add_argument('--project',required=True);r.add_argument('--task',required=True);r.add_argument('--dry-run',action='store_true');r.add_argument('--execute',action='store_true');r.add_argument('--authorize-write',action='store_true')
    args=parser.parse_args()
    try:
        if args.command=='doctor': doctor(pathlib.Path(args.project).resolve())
        elif args.command=='init-task':
            obj=load_json(BASE/'examples/task.example.json');obj.update(title=args.title,objective='TODO: define objective',test_commands=[],human_approved=False)
            path=pathlib.Path(args.out);path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(obj,indent=2,ensure_ascii=False),encoding='utf-8');print(path)
        elif args.command=='run': run(args)
    except (ValueError,OSError,subprocess.SubprocessError) as exc:
        print('BLOCKED:',exc,file=sys.stderr);sys.exit(2)
if __name__=='__main__': main()
