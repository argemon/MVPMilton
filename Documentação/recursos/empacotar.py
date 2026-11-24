"""Compacta somente a entrega documental, sem caches, segredos ou aplicação."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
BASE=Path(__file__).resolve().parents[1]
dest=BASE/'Pacote-documentacao.zip'
with ZipFile(dest,'w',ZIP_DEFLATED) as z:
    for folder in ['entrega','diagramas','fontes','recursos','evidencias']:
        for p in sorted((BASE/folder).rglob('*')):
            if p.is_file() and '__pycache__' not in p.parts:
                z.write(p,Path('Documentação')/p.relative_to(BASE))
    z.write(BASE/'README.md','Documentação/README.md')
print('Pacote criado:',dest.name,dest.stat().st_size,'bytes')
