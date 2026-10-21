import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: true,
  retries: 0,
  globalTimeout: 120000,
  globalSetup: './tests/global-setup.ts',
  outputDir: '../../docs/stage3/d2b/evidence/browser-artifacts',
  reporter: [['list'], ['json', { outputFile: '../../docs/stage3/d2b/evidence/browser-contracts.json' }]],
  use: { baseURL: 'http://127.0.0.1:4173', trace: 'off', screenshot: 'off', video: 'off' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
