import { spawn, type ChildProcess } from 'node:child_process';
import { closeSync, existsSync, mkdirSync, openSync } from 'node:fs';
import { createServer } from 'node:net';
import { resolve } from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';

// Spawn the executables directly so teardown owns their handles on Windows.
// No shell/process-tree termination and no reuse of existing application servers.
export default async function setup() {
  const webRoot = resolve(import.meta.dirname, '..');
  const apiRoot = resolve(webRoot, '../Kulani.KDI.Api');
  const apiDll = resolve(apiRoot, 'bin/Release/net10.0/Kulani.KDI.Api.dll');
  const logRoot = resolve(webRoot, '../../.local/e2e');
  if (!existsSync(apiDll)) throw new Error('Build the API in Release before running browser tests.');
  if (!existsSync(resolve(webRoot, 'dist/index.html'))) throw new Error('Build the web application before running browser tests.');
  mkdirSync(logRoot, { recursive: true });
  const children: ChildProcess[] = [];

  async function close() {
    await Promise.all(children.map(async child => {
      if (child.exitCode !== null || child.signalCode !== null) return;
      await new Promise<void>((done, reject) => {
        const timeout = setTimeout(() => reject(new Error(`Test server ${child.pid} failed to stop`)), 5000);
        child.once('exit', () => { clearTimeout(timeout); done(); });
        child.kill();
      });
    }));
  }

  async function start(executable: string, args: string[], cwd: string, port: number, readyPath: string, logName: string) {
    await new Promise<void>((done, reject) => {
      const probe = createServer();
      probe.once('error', () => reject(new Error(`Test port ${port} is already in use; no existing server will be stopped or reused.`)));
      probe.listen(port, '127.0.0.1', () => probe.close(() => done()));
    });
    const descriptor = openSync(resolve(logRoot, logName), 'w');
    const child = spawn(executable, args, {
      cwd, windowsHide: true, stdio: ['ignore', descriptor, descriptor],
      env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Production', DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_GENERATE_ASPNET_CERTIFICATE: 'false' },
    });
    closeSync(descriptor);
    children.push(child);
    let launchError: Error | undefined;
    child.once('error', error => { launchError = error; });
    for (let attempt = 0; attempt < 100; attempt++) {
      if (launchError) throw launchError;
      if (child.exitCode !== null) throw new Error(`Test server exited; inspect .local/e2e/${logName}`);
      try {
        const response = await fetch(`http://127.0.0.1:${port}${readyPath}`, { signal: AbortSignal.timeout(500) });
        await response.body?.cancel();
        if (response.ok) return;
      } catch { /* Allow bounded startup time. */ }
      await delay(100);
    }
    throw new Error(`Test server startup timed out; inspect .local/e2e/${logName}`);
  }

  try {
    await start('dotnet', [apiDll, '--urls', 'http://127.0.0.1:5080', '--contentRoot', apiRoot], apiRoot, 5080, '/health/live', 'api.log');
    await start(process.execPath, [resolve(webRoot, 'node_modules/vite/bin/vite.js'), 'preview', '--host', '127.0.0.1', '--port', '4173', '--strictPort'], webRoot, 4173, '/', 'web.log');
    return close;
  } catch (error) {
    await close();
    throw error;
  }
}
