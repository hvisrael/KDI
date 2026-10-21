import { useCallback, useEffect, useRef, useState } from 'react';
import { AccessFailure, getAccess, getCsrf, getOrganizations, getSession, type Grant, type Organization, type Session } from './client';

type State = { kind: 'loading' | 'disabled' | 'unavailable' | 'anonymous' } | { kind: 'authenticated'; session: Session; organizations: Organization[] };

export function AccessPanel() {
  const [state, setState] = useState<State>({ kind: 'loading' });
  const [csrf, setCsrf] = useState('');
  const [organizationId, setOrganizationId] = useState('');
  const [grants, setGrants] = useState<Grant[]>([]);
  const [grantState, setGrantState] = useState<'idle' | 'loading' | 'ready' | 'denied' | 'error'>('idle');
  const requests = useRef<AbortController | null>(null);
  const load = useCallback(() => {
    requests.current?.abort();
    const controller = new AbortController(); requests.current = controller;
    setCsrf(''); setOrganizationId(''); setGrants([]); setGrantState('idle'); setState({ kind: 'loading' });
    void (async () => {
      try {
        const result = await getSession(controller.signal);
        if (result.kind === 'disabled') { if (!controller.signal.aborted) setState(result); return; }
        const [token, organizations] = await Promise.all([getCsrf(controller.signal),
          result.kind === 'authenticated' ? getOrganizations(controller.signal) : Promise.resolve([])]);
        if (controller.signal.aborted) return;
        setCsrf(token); setState(result.kind === 'authenticated' ? { ...result, organizations } : result);
      } catch (error) {
        if (!controller.signal.aborted) setState({ kind: error instanceof AccessFailure && error.status === 401 ? 'anonymous' : 'unavailable' });
      }
    })();
  }, []);
  useEffect(() => {
    load(); const focus = () => load(); window.addEventListener('focus', focus);
    return () => { requests.current?.abort(); window.removeEventListener('focus', focus); };
  }, [load]);
  useEffect(() => {
    if (state.kind !== 'authenticated') return;
    const expiry = Math.min(Date.parse(state.session.idleExpiresAtUtc), Date.parse(state.session.absoluteExpiresAtUtc));
    const timer = window.setTimeout(() => {
      // Clear private UI at expiry without polling or extending an inactive session.
      requests.current?.abort();
      setState({ kind: 'anonymous' }); setCsrf(''); setOrganizationId(''); setGrants([]); setGrantState('idle');
      const controller = new AbortController(); requests.current = controller;
      void getCsrf(controller.signal).then(token => { if (!controller.signal.aborted) setCsrf(token); }).catch(() => {});
    }, Math.max(0, Math.min(2147483647, expiry - Date.now())));
    return () => window.clearTimeout(timer);
  }, [state, load]);
  useEffect(() => {
    if (!organizationId) return;
    const controller = new AbortController();
    void getAccess(organizationId, controller.signal).then(value => {
      if (!controller.signal.aborted) { setGrants(value); setGrantState('ready'); }
    }).catch(error => {
      if (controller.signal.aborted) return;
      setGrants([]);
      if (error instanceof AccessFailure && error.status === 401) { load(); return; }
      setGrantState(error instanceof AccessFailure && [403, 404].includes(error.status) ? 'denied' : 'error');
    });
    return () => controller.abort();
  }, [organizationId, load]);
  if (state.kind === 'disabled' || state.kind === 'loading') return null;
  return <section className="access-card" aria-labelledby="access-title">
    <h2 id="access-title">Identidad y acceso</h2>
    {state.kind === 'unavailable' ? <>
      <p role="alert">No se pudo verificar tu sesión. No se mostrará información de organizaciones.</p>
      <button type="button" onClick={load}>Reintentar sesión</button>
    </> : <>
      <p>{state.kind === 'authenticated' ? `Sesión de ${state.session.user.displayName}` : 'Inicia sesión para consultar tus organizaciones.'}</p>
      <form method="post" action={state.kind === 'authenticated' ? '/auth/logout' : '/auth/login'}>
        <input type="hidden" name="__RequestVerificationToken" value={csrf} /><input type="hidden" name="returnUrl" value="/" />
        <button type="submit" disabled={!csrf}>{state.kind === 'authenticated' ? 'Cerrar sesión' : 'Iniciar sesión'}</button>
      </form>
      {state.kind === 'authenticated' && <>
        <p className="supporting">La sesión termina a más tardar a las {new Date(state.session.absoluteExpiresAtUtc).toLocaleTimeString()}.</p>
        {state.organizations.length === 0 ? <p>No tienes una organización activa asignada. Solicita acceso al operador de KDI.</p> : <>
          <label htmlFor="organization">Organización</label>
          <select id="organization" value={organizationId} onChange={event => {
            setGrants([]); setGrantState(event.target.value ? 'loading' : 'idle'); setOrganizationId(event.target.value);
          }}><option value="">Selecciona una organización</option>
            {state.organizations.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
          </select>
          {grantState === 'loading' && <p aria-live="polite">Verificando permisos…</p>}
          {grantState === 'denied' && <p role="alert">Esta organización ya no está disponible para tu sesión.</p>}
          {grantState === 'error' && <p role="alert">No se pudieron verificar los permisos.</p>}
          {organizationId && grantState === 'ready' && <div aria-live="polite"><h3>Permisos del contexto seleccionado</h3>
            {grants.length === 0 ? <p>No hay concesiones activas.</p> : <ul>{grants.map(item =>
              <li key={`${item.code}:${item.scopeType}:${item.scopeId}`}>{item.code}{item.scopeType === 'Project' ? ' · proyecto específico' : ' · organización'}</li>)}</ul>}
            <p className="supporting">Este contexto pertenece a esta pestaña. Las funciones de negocio se incorporarán en otra etapa.</p>
          </div>}
        </>}
      </>}
    </>}
  </section>;
}
