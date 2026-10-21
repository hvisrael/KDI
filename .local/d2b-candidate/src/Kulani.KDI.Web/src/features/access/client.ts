export interface Session {
  user: { id: string; displayName: string };
  createdAtUtc: string; idleExpiresAtUtc: string; absoluteExpiresAtUtc: string;
}
export interface Organization { id: string; code: string; name: string }
export interface Grant { code: string; scopeType: string; scopeId: string }
export type SessionResult = { kind: 'disabled' } | { kind: 'anonymous' } | { kind: 'authenticated'; session: Session };
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
function object(value: unknown): value is Record<string, unknown> { return typeof value === 'object' && value !== null; }
function date(value: unknown): value is string { return typeof value === 'string' && Number.isFinite(Date.parse(value)); }
export class AccessFailure extends Error { constructor(public readonly status: number) { super('No se pudo verificar el acceso.'); } }
async function get(path: string, signal: AbortSignal): Promise<Response> {
  return fetch(path, { signal, cache: 'no-store', credentials: 'same-origin', redirect: 'error', headers: { Accept: 'application/json' } });
}
export async function getSession(signal: AbortSignal): Promise<SessionResult> {
  const response = await get('/api/v1/session', signal);
  if (response.status === 404) return { kind: 'disabled' };
  if (response.status === 401) return { kind: 'anonymous' };
  if (!response.ok) throw new AccessFailure(response.status);
  const value: unknown = await response.json();
  if (!object(value) || !object(value.user) || typeof value.user.id !== 'string' || !guid.test(value.user.id)
    || typeof value.user.displayName !== 'string' || !date(value.createdAtUtc) || !date(value.idleExpiresAtUtc) || !date(value.absoluteExpiresAtUtc)) throw new AccessFailure(502);
  return { kind: 'authenticated', session: value as unknown as Session };
}
export async function getCsrf(signal: AbortSignal): Promise<string> {
  const response = await get('/auth/csrf', signal);
  if (!response.ok) throw new AccessFailure(response.status);
  const value: unknown = await response.json();
  if (!object(value) || typeof value.requestToken !== 'string' || value.requestToken.length === 0 || value.requestToken.length > 8192) throw new AccessFailure(502);
  return value.requestToken;
}
export async function getOrganizations(signal: AbortSignal): Promise<Organization[]> {
  const response = await get('/api/v1/organizations', signal);
  if (!response.ok) throw new AccessFailure(response.status);
  const value: unknown = await response.json();
  if (!object(value) || !Array.isArray(value.items) || !value.items.every((item: unknown) => object(item)
    && typeof item.id === 'string' && guid.test(item.id) && typeof item.code === 'string' && typeof item.name === 'string')) throw new AccessFailure(502);
  return value.items as Organization[];
}
export async function getAccess(id: string, signal: AbortSignal): Promise<Grant[]> {
  if (!guid.test(id)) throw new AccessFailure(400);
  const response = await get(`/api/v1/organizations/${encodeURIComponent(id)}/access`, signal);
  if (!response.ok) throw new AccessFailure(response.status);
  const value: unknown = await response.json();
  if (!object(value) || value.organizationId !== id || !Array.isArray(value.permissions)
    || !value.permissions.every((grant: unknown) => object(grant) && typeof grant.code === 'string'
      && (grant.scopeType === 'Organization' || grant.scopeType === 'Project') && typeof grant.scopeId === 'string' && guid.test(grant.scopeId))) throw new AccessFailure(502);
  return value.permissions as Grant[];
}
