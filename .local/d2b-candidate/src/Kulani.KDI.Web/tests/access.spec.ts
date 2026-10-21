import { expect, test, type Page } from '@playwright/test';

// Browser contracts with synthetic HTTP fixtures only. These do not prove real OIDC or SQL.
const a = '11111111-1111-4111-8111-111111111111';
const b = '22222222-2222-4222-8222-222222222222';
const user = '33333333-3333-4333-8333-333333333333';
const organizations = [{ id: a, code: 'A', name: 'Organización A' }, { id: b, code: 'B', name: 'Organización B' }];
const session = () => ({ user: { id: user, displayName: 'Usuario sintético' }, createdAtUtc: new Date().toISOString(),
  idleExpiresAtUtc: new Date(Date.now() + 1800000).toISOString(), absoluteExpiresAtUtc: new Date(Date.now() + 3600000).toISOString() });
async function baseline(page: Page) {
  await page.route('**/health/live', route => route.fulfill({ json: { status: 'healthy' } }));
  await page.route('**/auth/csrf', route => route.fulfill({ json: { requestToken: 'synthetic-antiforgery-fixture' } }));
}
async function authenticated(page: Page, items = organizations) {
  await baseline(page);
  await page.route('**/api/v1/session', route => route.fulfill({ json: session() }));
  await page.route('**/api/v1/organizations', route => route.fulfill({ json: { items } }));
}
test('anonymous login uses a same-origin POST form with antiforgery, not browser OIDC tokens', async ({ page }) => {
  await baseline(page);
  await page.route('**/api/v1/session', route => route.fulfill({ status: 401, json: { status: 401 } }));
  await page.goto('/');
  const login = page.getByRole('button', { name: 'Iniciar sesión', exact: true });
  await expect(login).toBeEnabled();
  const form = page.locator('form[action="/auth/login"]');
  await expect(form).toHaveAttribute('method', 'post');
  await expect(form.locator('[name="__RequestVerificationToken"]')).toHaveValue('synthetic-antiforgery-fixture');
  expect(await page.evaluate(() => localStorage.length + sessionStorage.length)).toBe(0);
  await expect(page.locator('#organization')).toHaveCount(0);
});
test('authenticated user without memberships has no organization or implicit privileges', async ({ page }) => {
  await authenticated(page, []);
  await page.goto('/');
  await expect(page.getByText('No tienes una organización activa asignada. Solicita acceso al operador de KDI.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Cerrar sesión' })).toBeEnabled();
  await expect(page.locator('#organization')).toHaveCount(0);
});
test('organization changes cancel old access requests and clear previous permissions', async ({ page }) => {
  await authenticated(page);
  let releaseA = () => {};
  const gate = new Promise<void>(resolve => { releaseA = resolve; });
  let completeA = () => {};
  const doneA = new Promise<void>(resolve => { completeA = resolve; });
  await page.route(`**/organizations/${a}/access`, async route => {
    await gate;
    try { await route.fulfill({ json: { organizationId: a, permissions: [{ code: 'clients.manage', scopeType: 'Organization', scopeId: a }] } }); }
    catch { /* Abort is the expected safe behavior when the selection changes. */ }
    finally { completeA(); }
  });
  await page.route(`**/organizations/${b}/access`, route => route.fulfill({ json: {
    organizationId: b, permissions: [{ code: 'projects.read', scopeType: 'Organization', scopeId: b }],
  } }));
  await page.goto('/');
  const requestA = page.waitForRequest(request => request.url().endsWith(`/organizations/${a}/access`));
  await page.getByLabel('Organización', { exact: true }).selectOption(a); await requestA;
  await page.getByLabel('Organización', { exact: true }).selectOption(b);
  await expect(page.getByText('projects.read · organización', { exact: true })).toBeVisible();
  releaseA(); await doneA;
  await expect(page.getByText('clients.manage · organización', { exact: true })).toHaveCount(0);
  await expect(page.getByText('projects.read · organización', { exact: true })).toBeVisible();
});
test('revoked organization returns no old permission list', async ({ page }) => {
  await authenticated(page);
  await page.route(`**/organizations/${a}/access`, route => route.fulfill({ json: {
    organizationId: a, permissions: [{ code: 'clients.read', scopeType: 'Organization', scopeId: a }],
  } }));
  await page.route(`**/organizations/${b}/access`, route => route.fulfill({ status: 404, json: { status: 404 } }));
  await page.goto('/');
  await page.getByLabel('Organización', { exact: true }).selectOption(a);
  await expect(page.getByText('clients.read · organización', { exact: true })).toBeVisible();
  await page.getByLabel('Organización', { exact: true }).selectOption(b);
  await expect(page.getByRole('alert')).toHaveText('Esta organización ya no está disponible para tu sesión.');
  await expect(page.getByText('clients.read · organización', { exact: true })).toHaveCount(0);
});
test('dependency failure does not request or display organization data', async ({ page }) => {
  await baseline(page);
  let organizationRequests = 0;
  await page.route('**/api/v1/session', route => route.fulfill({ status: 503, json: { status: 503 } }));
  await page.route('**/api/v1/organizations', route => { organizationRequests++; return route.fulfill({ json: { items: organizations } }); });
  await page.goto('/');
  await expect(page.getByRole('alert')).toContainText('No se pudo verificar tu sesión');
  await expect(page.locator('#organization')).toHaveCount(0);
  expect(organizationRequests).toBe(0);
});
test('different tabs keep organization selection independent', async ({ page, context }) => {
  const second = await context.newPage();
  for (const tab of [page, second]) {
    await authenticated(tab);
    for (const [id, code] of [[a, 'clients.read'], [b, 'projects.read']]) {
      await tab.route(`**/organizations/${id}/access`, route => route.fulfill({ json: {
        organizationId: id, permissions: [{ code, scopeType: 'Organization', scopeId: id }],
      } }));
    }
    await tab.goto('/');
  }
  await page.getByLabel('Organización', { exact: true }).selectOption(a);
  await second.getByLabel('Organización', { exact: true }).selectOption(b);
  await expect(page.getByText('clients.read · organización', { exact: true })).toBeVisible();
  await expect(second.getByText('projects.read · organización', { exact: true })).toBeVisible();
  await expect(page.getByLabel('Organización', { exact: true })).toHaveValue(a);
});
