"""Compara dos manifiestos de candidato y reporta altas, bajas y cambios.

Cierra una debilidad reconocida en la evidencia D2-B: el manifiesto se regeneraba
en la misma ruta sin conservar la version anterior, por lo que no se afirmaba un
diff exhaustivo entre los hashes de dos intentos.

Uso:
    python docs/stage3/d2b/preparation-20261020/manifest-diff.py \
        --old <manifiesto-anterior.json> --new <manifiesto-nuevo.json> [--json]

Solo lee archivos. No modifica nada, no regenera manifiestos y no abre conexiones.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]


def load(path: Path) -> dict[str, str]:
    data = json.loads(path.read_text(encoding='utf-8'))
    entries = data['entries']
    return {e['path']: e['sha256'] for e in entries}


def main() -> int:
    parser = argparse.ArgumentParser(description='Diff entre manifiestos de candidato KDI.')
    parser.add_argument('--old', required=True, type=Path, help='manifiesto anterior')
    parser.add_argument('--new', required=True, type=Path, help='manifiesto nuevo')
    parser.add_argument('--json', action='store_true', help='salida en JSON')
    args = parser.parse_args()

    for label, path in (('anterior', args.old), ('nuevo', args.new)):
        if not path.is_absolute():
            path = (ROOT / path).resolve()
        if not path.is_file():
            print(f'ERROR: manifiesto {label} no encontrado: {path}', file=sys.stderr)
            return 2
        args.__dict__[f'_{label}'] = path

    old = load(args._anterior)
    new = load(args._nuevo)

    added = sorted(set(new) - set(old))
    removed = sorted(set(old) - set(new))
    changed = sorted(p for p in (set(old) & set(new)) if old[p] != new[p])
    unchanged = len(set(old) & set(new)) - len(changed)

    report = {
        'old_manifest': str(args._anterior),
        'old_manifest_sha256': hashlib.sha256(args._anterior.read_bytes()).hexdigest(),
        'old_file_count': len(old),
        'new_manifest': str(args._nuevo),
        'new_manifest_sha256': hashlib.sha256(args._nuevo.read_bytes()).hexdigest(),
        'new_file_count': len(new),
        'added': added,
        'removed': removed,
        'changed': changed,
        'unchanged_count': unchanged,
    }

    if args.json:
        print(json.dumps(report, indent=2, ensure_ascii=False))
        return 0

    print(f"anterior : {report['old_manifest_sha256']}  ({report['old_file_count']} archivos)")
    print(f"nuevo    : {report['new_manifest_sha256']}  ({report['new_file_count']} archivos)")
    print(f"sin cambio: {unchanged}")
    print(f"altas    : {len(added)}")
    for p in added:
        print(f"   + {p}")
    print(f"bajas    : {len(removed)}")
    for p in removed:
        print(f"   - {p}")
    print(f"cambios  : {len(changed)}")
    for p in changed:
        print(f"   ~ {p}")
        print(f"       {old[p]} -> {new[p]}")
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
