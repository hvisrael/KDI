import subprocess, json
from pathlib import Path
r=Path(__file__).resolve().parents[1]
web=r/'src/Kulani.KDI.Web'
node='C:/Users/Israel/AppData/Local/pi-node/current/node.exe'
out=r/'docs/stage3/d1/closure'
records=[]
for name,args in [('typecheck',['node_modules/typescript/bin/tsc','--noEmit']),('lint',['node_modules/eslint/bin/eslint.js','.','--max-warnings','0'])]:
    command=[node,*args]
    p=subprocess.run(command,cwd=web,capture_output=True,text=True,timeout=60)
    (out/f'd1-close-{name}-final.log').write_text(f'Command: {command}\nExit code: {p.returncode}\n'+p.stdout+p.stderr,encoding='utf-8')
    records.append({'name':name,'command':command,'cwd':str(web),'exitCode':p.returncode})
(out/'web-exit-codes.json').write_text(json.dumps(records,indent=2)+'\n')
print(records)
assert all(x['exitCode']==0 for x in records)
