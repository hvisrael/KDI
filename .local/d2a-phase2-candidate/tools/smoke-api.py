"""Run bounded HTTP checks against the built API, using no external packages or database.

This supplements, and never replaces, the xUnit/API/architecture suites.
"""
import json
import os
from pathlib import Path
import socket
import subprocess
import time
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / 'src/Kulani.KDI.Api/bin/Release/net10.0/Kulani.KDI.Api.dll'
assert DLL.is_file(), 'Build the API in Release first'
with socket.socket() as sock:
    sock.bind(('127.0.0.1', 0))
    port = sock.getsockname()[1]
base = f'http://127.0.0.1:{port}'
env = dict(os.environ, ASPNETCORE_URLS=base, ASPNETCORE_ENVIRONMENT='Production',
           DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_GENERATE_ASPNET_CERTIFICATE='false')
local = ROOT / '.local'
local.mkdir(exist_ok=True)
checks = []

def request(path, method='GET', correlation=None):
    headers = {'Accept': 'application/json'}
    if correlation is not None:
        headers['X-Correlation-ID'] = correlation
    req = urllib.request.Request(base + path, headers=headers, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=3)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        text = response.read().decode()
        return response.status, response.headers, json.loads(text)

with (local / 'api-smoke-host.log').open('w', encoding='utf-8') as log:
    process = subprocess.Popen(['dotnet', str(DLL)], cwd=DLL.parent, env=env, stdout=log, stderr=log,
                               creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    try:
        for _ in range(100):
            if process.poll() is not None:
                raise RuntimeError('API exited before readiness; inspect .local/api-smoke-host.log')
            try:
                status, headers, body = request('/health/live')
                break
            except (OSError, ValueError):
                time.sleep(0.1)
        else:
            raise RuntimeError('API did not start within 10 seconds')
        assert status == 200 and body == {'status': 'healthy'}
        assert headers.get('Server') is None
        assert headers['Cache-Control'] == 'no-store'
        checks.append('liveness-public-no-server-header-no-cache')

        status, _, body = request('/api/v1/system/info')
        assert status == 200 and body == {'product': 'Kulani Delivery Intelligence', 'apiVersion': 'v1'}
        checks.append('metadata-exact-contract')

        for path, method, expected, code in [('/missing', 'GET', 404, 'not_found'),
                ('/api/v1/organizations/1/clients', 'GET', 404, 'not_found'),
                ('/health/live', 'POST', 405, 'method_not_allowed')]:
            status, headers, body = request(path, method, 'smoke-request-123')
            assert status == expected and body['status'] == expected
            assert headers.get_content_type() == 'application/problem+json'
            assert body['code'] == code
            assert body['correlationId'] == headers['X-Correlation-ID'] == 'smoke-request-123'
            assert 'detail' not in body and 'stackTrace' not in body
            checks.append(f'{method}-{path}-{expected}-correlated-problem')

        for supplied in ['one,two', 'x' * 65, 'with space']:
            status, headers, _ = request('/health/live', correlation=supplied)
            assert status == 200
            assert uuid.UUID(headers['X-Correlation-ID']).hex == headers['X-Correlation-ID']
        checks.append('untrusted-correlation-identifiers-replaced')

        status, _, doc = request('/openapi/v1.json')
        assert status == 200 and doc['openapi'].startswith('3.')
        assert set(doc['paths']) == {'/health/live', '/health/ready', '/api/v1/system/info'}
        for path in doc['paths'].values():
            assert set(path) == {'get'}
            schema = path['get']['responses']['200']['content']['application/json']['schema']
            assert '$ref' in schema or 'properties' in schema
        assert doc['servers'] == [{'url': '/'}]
        api_docs = ROOT / 'docs/stage3/d2a/evidence'
        api_docs.mkdir(exist_ok=True, parents=True)
        (api_docs / 'openapi-v1.json').write_text(json.dumps(doc, indent=2) + '\n', encoding='utf-8')
        checks.append('generated-openapi-exact-paths-and-success-schemas')
        status, _, body = request('/health/ready')
        assert status == 503 and body == {'status': 'not_ready'}
        checks.append('readiness-fails-closed-without-runtime-identity')
    finally:
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=5)

result = {'suite': 'author-http-smoke', 'status': 'PASS', 'checks': checks,
          'limits': ['Not xUnit, independent QA or review', 'No SQL/auth/business E2E', 'No exception injection']}
(ROOT / 'docs/stage3/d2a/evidence/api-smoke-result.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result, indent=2))
