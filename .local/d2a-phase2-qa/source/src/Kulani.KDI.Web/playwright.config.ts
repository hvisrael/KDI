import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: true,
  retries: 0,
  globalTimeout: 60000,
  globalSetup: './tests/global-setup.ts',
  reporter: [['list'], ['html', { open: 'never' }], ['json', { outputFile: '../../docs/stage3/d2a/evidence/playwright-result.json' }]],
  use: { baseURL: 'http://127.0.0.1:4173', trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
