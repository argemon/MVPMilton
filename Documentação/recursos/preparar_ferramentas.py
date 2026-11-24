"""Baixa ferramentas oficiais para cache local; não instala nem acessa o sistema de negócio."""
from pathlib import Path
import urllib.request
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
CACHE = ROOT / '.local' / 'documentacao-tools'
CACHE.mkdir(parents=True, exist_ok=True)
FILES = {
    'plantuml-1.2024.8.jar': 'https://repo.maven.apache.org/maven2/net/sourceforge/plantuml/plantuml/1.2024.8/plantuml-1.2024.8.jar',
    'LibreOffice_25.2.7.2_Win_x86-64.msi': 'https://downloadarchive.documentfoundation.org/libreoffice/old/25.2.7.2/win/x86_64/LibreOffice_25.2.7.2_Win_x86-64.msi',
}
manifest = []
for name, url in FILES.items():
    dest = CACHE / name
    if not dest.exists():
        print('Baixando', name, flush=True)
        with urllib.request.urlopen(url, timeout=60) as response, dest.open('wb') as stream:
            while chunk := response.read(1024 * 1024):
                stream.write(chunk)
    manifest.append({'arquivo': name, 'url': url, 'bytes': dest.stat().st_size, 'sha256': hashlib.sha256(dest.read_bytes()).hexdigest()})
    print(name, dest.stat().st_size, flush=True)
(CACHE / 'downloads.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
