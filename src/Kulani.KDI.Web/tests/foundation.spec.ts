import { expect, test } from '@playwright/test';

test('loads shell, checks connection and permits keyboard retry', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.route('**/health/live', route => route.fulfill({ json: { status: 'healthy' } }));
  await page.goto('/');
  await expect(page.getByRole('heading', { level: 1 })).toContainText('De la intención');
  await expect(page.getByRole('status')).toHaveText('API conectada');
  const retry = page.getByRole('button', { name: 'Volver a comprobar' });
  await retry.focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('status')).toHaveText('API conectada');
  await expect(page.getByRole('button')).toHaveCount(1);
  await page.screenshot({ path: '../../docs/stage3/d1/screenshots/desktop.png', fullPage: true });
  expect(errors).toEqual([]);
});

test('shows loading, unavailability and recovery without stale success', async ({ page }) => {
  let healthy = false;
  await page.route('**/health/live', async route => {
    await new Promise(resolve => setTimeout(resolve, 300));
    await route.fulfill({ status: healthy ? 200 : 503, json: healthy ? { status: 'healthy' } : {} });
  });
  await page.goto('/');
  await expect(page.getByRole('status')).toHaveText('Comprobando conexión…');
  await expect(page.getByRole('button')).toBeDisabled();
  await expect(page.getByRole('status')).toHaveText('API no disponible');
  healthy = true;
  await page.getByRole('button').click();
  await expect(page.getByRole('status')).toHaveText('API conectada');
});

test('does not report connection for an unexpected response', async ({ page }) => {
  await page.route('**/health/live', route => route.fulfill({ json: { status: 'unknown' } }));
  await page.goto('/');
  await expect(page.getByRole('status')).toHaveText('API no disponible');
});

test('fits a mobile viewport', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await page.route('**/health/live', route => route.fulfill({ json: { status: 'healthy' } }));
  await page.goto('/');
  await expect(page.getByRole('status')).toHaveText('API conectada');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: '../../docs/stage3/d1/screenshots/mobile.png', fullPage: true });
});

test('connects the built web shell to the real API through the same-origin proxy', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('status')).toHaveText('API conectada');
  const response = await page.request.get('/api/v1/system/info');
  expect(response.ok()).toBe(true);
  expect(await response.json()).toEqual({ product: 'Kulani Delivery Intelligence', apiVersion: 'v1' });
});
