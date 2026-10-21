"""Copy built web assets into the isolated candidate for the same-origin pilot."""
from pathlib import Path
import shutil

root = Path(__file__).resolve().parents[1]
source = root / 'src/Kulani.KDI.Web/dist'
destination = root / 'src/Kulani.KDI.Api/wwwroot'
if not (source / 'index.html').is_file():
    raise SystemExit('Build the web application first.')
if destination.exists():
    raise SystemExit('wwwroot already exists; inspect it before replacing generated assets.')
shutil.copytree(source, destination)
print('Prepared same-origin assets; no server, SQL or authentication started.')
