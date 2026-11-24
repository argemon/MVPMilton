"""Extrai paginação e controles do PDF gerado do Word, sem reconstruir seu conteúdo."""
from pathlib import Path
import json,re,argparse
from pypdf import PdfReader
BASE=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--qa',type=Path,default=BASE.parent/'.local/documentacao-qa');args=parser.parse_args()
maps={};report={}
for key in ['monografia','documentacao-tecnica','manual-do-usuario']:
    pdf=args.qa/key/(key+'.pdf');reader=PdfReader(pdf)
    found={}
    def walk(entries):
        for x in entries:
            if isinstance(x,list):walk(x)
            else:
                try:found[x.title]=reader.get_destination_page_number(x)+1
                except Exception:pass
    walk(reader.outline)
    start=next((page for title,page in found.items() if title.startswith('1 ')),1)
    mapped={title:page-(1 if key=='monografia' else 0) for title,page in found.items()}
    mapped['__inicio__']=start
    for page_num,page in enumerate(reader.pages,1):
        txt=page.extract_text() or ''
        for m in re.finditer(r'((?:Figura|Tabela)\s+\d+\s*[—–-]\s*[^\n]+)',txt):
            title=re.sub(r'\s+',' ',m.group(1)).strip()
            # Lista de ilustrações inclui a entrada antes da figura real; a última ocorrência é a legenda.
            if not re.search(r'\s\d+$',title):mapped[title]=page_num-(1 if key=='monografia' else 0)
    maps[key]=mapped
    def body_text(page):
        return '\n'.join(line for line in (page.extract_text() or '').splitlines() if not line.strip().isdigit() and not line.startswith('Versão para revisão acadêmica'))
    report[key]={'paginas':len(reader.pages),'titulos_pdf':len(found),'inicio_textual_folha_pdf':start,'paginas_sem_texto':[i for i,p in enumerate(reader.pages,1) if len(body_text(p).strip())<20]}
out=args.qa/'mapas-paginas.json';out.write_text(json.dumps(maps,ensure_ascii=False,indent=2),encoding='utf-8')
(args.qa/'inspecao-estrutural.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
