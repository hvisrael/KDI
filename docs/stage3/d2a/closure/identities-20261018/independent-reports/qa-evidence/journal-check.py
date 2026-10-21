from pathlib import Path
import json,collections,hashlib
R=Path.cwd(); B=R/'docs/stage3/d2a/closure/identities-20261018'; E=B/'evidence/phase2'; J=json.loads((E/'sql-v2/execution-20261019T031842176Z.json').read_text(encoding='utf-8-sig'))
comm=[x for x in J if x['result']=='COMMITTED']; assert collections.Counter(x['operation'] for x in comm)=={'CREATE LOGIN':3,'CREATE USER':5,'ALTER ROLE ADD MEMBER':5}
obs=[x for x in J if x['stage']=='OBSERVE_PERMISSIONS']; assert len(obs)==5
for x in obs:
 f=x['facts']; n=x['identity']; d=x['database']; assert all(f[k]==n for k in ('login_name','current_login','user_name')); assert f['database_name']==d and f['authentication_type']=='INSTANCE'
 for k,v in f.items():
  if k.startswith('server_'): assert v==0,(n,k,v)
  elif k.startswith('serverperm_'): assert v is None
  elif k.startswith('member_'): assert v==int(k=='member_'+{'kdi_app':'kdi_application','kdi_migrator':'kdi_migrations','kdi_test_runner':'kdi_tests'}[n]),(n,k,v)
  elif k.startswith('dbperm_'): assert v==int(n=='kdi_migrator' and k=='dbperm_CREATE_TABLE'),(n,k,v)
  elif '_' in k and k.split('_')[0] in ('identity','organizations','portfolio','dbo') and not k.endswith('_count'):
   schema,perm=k.split('_',1); assert v==int(schema!='dbo' and (n=='kdi_migrator' or (n=='kdi_test_runner' and perm=='SELECT'))),(n,k,v)
 assert len(f['mismatches'])==3 and all(z.startswith('serverperm_') for z in f['mismatches'])
assert len([x for x in J if x['identity']=='kdi_test_runner' and x['database']=='KulaniKDI_Dev' and x['sqlNumber']==4060])==1
print('Independent journal validation: 13 authorized commits; 5 direct auth; 5 complete effective DB matrices; runnerDev4060; only 3 historical NULL fields each require server supplement.')
