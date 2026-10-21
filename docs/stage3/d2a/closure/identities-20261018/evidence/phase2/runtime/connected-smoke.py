"""Bounded HTTP verification of an already-running local API. No secrets or SQL."""
import argparse
import json
from pathlib import Path
import urllib.error
import urllib.request


def run(port):
    if not 1024 <= port <= 65535:
        raise ValueError('Unprivileged local port required')
    checks = []

    def request(path, method='GET'):
        req = urllib.request.Request(f'http://127.0.0.1:{port}{path}', method=method,
            headers={'Accept': 'application/json', 'X-Correlation-ID': 'connected-smoke-123'})
        try:
            response = urllib.request.urlopen(req, timeout=5)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            assert response.headers.get('Server') is None
            assert response.headers.get('Cache-Control') == 'no-store'
            assert response.headers.get('X-Correlation-ID') == 'connected-smoke-123'
            return response.status, json.loads(response.read(262144).decode())

    for path, expected in [('/health/live', {'status': 'healthy'}),
            ('/health/ready', {'status': 'ready'}),
            ('/api/v1/system/info', {'product': 'Kulani Delivery Intelligence', 'apiVersion': 'v1'})]:
        status, body = request(path)
        assert status == 200 and body == expected
        checks.append(path)
    for path, method, expected, code in [('/missing', 'GET', 404, 'not_found'),
            ('/health/live', 'POST', 405, 'method_not_allowed')]:
        status, body = request(path, method)
        assert status == expected and body.get('status') == expected
        assert body.get('code') == code and body.get('correlationId') == 'connected-smoke-123'
        assert 'detail' not in body and 'stackTrace' not in body
        checks.append(f'{method} {path} {expected}')
    status, body = request('/openapi/v1.json')
    assert status == 200 and body['openapi'].startswith('3.')
    assert set(body['paths']) == {'/health/live', '/health/ready', '/api/v1/system/info'}
    assert all(set(item) == {'get'} for item in body['paths'].values())
    checks.append('openapi-paths')
    return checks


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, required=True)
    args = parser.parse_args()
    try:
        result = {'status': 'PASS', 'checks': run(args.port)}
    except Exception:
        # Never persist response bodies/provider errors or exception strings.
        result = {'status': 'FAIL', 'checks': [], 'reason': 'HTTP contract verification failed'}
    result['limits'] = ['No SQL identity proof', 'No business persistence proof', 'Not independent QA']
    Path(__file__).with_name('connected-smoke-result.json').write_text(
        json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result))
    raise SystemExit(0 if result['status'] == 'PASS' else 1)
