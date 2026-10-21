export type ConnectionState =
  | { kind: 'loading' }
  | { kind: 'connected' }
  | { kind: 'unavailable' };

export async function checkConnection(signal: AbortSignal): Promise<void> {
  const response = await fetch('/health/live', {
    signal,
    cache: 'no-store',
    headers: { Accept: 'application/json' },
  });
  if (!response.ok) throw new Error('Connection unavailable');
  const body: unknown = await response.json();
  if (typeof body !== 'object' || body === null || !('status' in body) || body.status !== 'healthy') {
    throw new Error('Unexpected liveness response');
  }
}
