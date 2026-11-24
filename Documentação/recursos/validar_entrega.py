"""Verificações de integridade documental, sem testes da aplicação ou acesso MongoDB."""
from pathlib import Path
from zipfile import ZipFile
import re,json,hashlib,xml.etree.ElementTree as ET
from PIL import Image
from pypdf import PdfReader

BASE=Path(__file__).resolve().parents[1];ROOT=BASE.parent
NS={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
errors=[];checks={}
def check(name,ok,detail=''):
    checks[name]={'aprovado':bool(ok),'detalhe':detail}
    if not ok:errors.append(name)
diagrams=json.loads((BASE/'diagramas/indice.json').read_text(encoding='utf-8'))
for stem in diagrams:
    for suffix in ['.puml','.png','.svg']:
        path=BASE/'diagramas'/(stem+suffix)
        check(stem+suffix,path.is_file() and path.stat().st_size>100)
    svg=(BASE/'diagramas'/(stem+'.svg')).read_text(encoding='utf-8')
    check(stem+' sintaxe','Syntax Error' not in svg and 'An error has occured' not in svg)
    Image.open(BASE/'diagramas'/(stem+'.png')).verify()
    ET.fromstring(svg)
for name in ['monografia','documentacao-tecnica','manual-do-usuario']:
    path=BASE/'entrega'/(name+'.docx')
    with ZipFile(path) as z:
        xml=ET.fromstring(z.read('word/document.xml'))
        styles=ET.fromstring(z.read('word/styles.xml'))
        text=' '.join(t.text or '' for t in xml.findall('.//w:t',NS))
        fields=' '.join(t.text or '' for t in xml.findall('.//w:instrText',NS))
        check(name+' sem marcadores internos','<!--' not in text and 'turn0' not in text and 'turn1' not in text)
        check(name+' sumario Word','TOC ' in fields)
        sects=xml.findall('.//w:sectPr',NS)
        check(name+' A4',all(s.find('w:pgSz',NS).get('{'+NS['w']+'}w')=='11906' and s.find('w:pgSz',NS).get('{'+NS['w']+'}h')=='16838' for s in sects))
        check(name+' sem borda de titulo',not styles.findall('.//w:pBdr',NS))
        bookmarks={x.get('{'+NS['w']+'}name') for x in xml.findall('.//w:bookmarkStart',NS)}
        targets=re.findall(r'PAGEREF (\w+)',fields)
        check(name+' referencias cruzadas',all(t in bookmarks for t in targets))
        # Campos com valor não resolvido no cache são encontrados pela paginação final.
    pdf=PdfReader(BASE/'entrega'/(name+'.pdf'))
    check(name+' PDF A4',all(abs(float(p.mediabox.width)-595.3)<2 and abs(float(p.mediabox.height)-841.9)<2 for p in pdf.pages))
    alltext='\n'.join((p.extract_text() or '') for p in pdf.pages)
    check(name+' texto PDF',len(alltext)>1000,str(len(pdf.pages))+' páginas')
    check(name+' sem glifo substituto','\ufffd' not in alltext)
    if name=='monografia':check('identificacao pendente explicita','AUTOR A PREENCHER' in alltext)

cases=json.loads((BASE/'fontes/casos-de-uso.json').read_text(encoding='utf-8'))
check('29 casos unicos',len(cases)==29 and len({c['id'] for c in cases})==29)
for c in cases:
    check(c['id']+' completo',all(c.get(k) for k in ['atores','rastreio','pre','fluxo','alternativas','pos','codigo']))
    for file in c['codigo'].split('; '):check(c['id']+' '+file,(ROOT/file).is_file())
tech=(BASE/'fontes/documentacao-tecnica.md').read_text(encoding='utf-8')
check('RF01 a20',all(f'RF-{i:02d}' in tech for i in range(1,21)))
check('RN01 a20',all(f'RN-{i:02d}' in tech for i in range(1,21)))
check('CA01 a18',all(f'CA-{i:02d}' in tech for i in range(1,19)))
refs=json.loads((BASE/'fontes/referencias.json').read_text(encoding='utf-8'))
check('referencias identificadas',len(refs)==len({r['id'] for r in refs}) and all(r['url'].startswith('https://') and r['consulta']=='2026-11-24' and r['uso'] for r in refs))
for name in ['monografia','documentacao-tecnica']:
    prose=(BASE/'fontes'/(name+'.md')).read_text(encoding='utf-8')
    selected=refs if name=='monografia' else [r for r in refs if r['id'] not in ['sommerville','swebok','normalizacao']]
    check(name+' referencias citadas',all(r['citacao'] in prose for r in selected))
for md in (BASE/'fontes').glob('*.md'):
    for label,target in re.findall(r'!?\[([^\]]+)\]\(([^)]+)\)',md.read_text(encoding='utf-8')):
        if not target.startswith(('http:','https:','#')):check(md.name+' link '+target,(md.parent/target.split('#')[0]).exists())
manifest=[]
for folder in ['entrega','diagramas','fontes','recursos']:
    for p in sorted((BASE/folder).rglob('*')):
        if p.is_file() and '__pycache__' not in p.parts:
            data=p.read_bytes();manifest.append({'arquivo':p.relative_to(BASE).as_posix(),'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
(BASE/'evidencias/manifesto-entrega.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
report={'data':'2026-11-24','natureza':'Validação documental local; nenhum teste de software executado','verificacoes':checks,'erros':errors}
(BASE/'evidencias/validacao-documental.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'verificacoes':len(checks),'erros':errors,'arquivos_manifesto':len(manifest)},ensure_ascii=False))
if errors:raise SystemExit(1)
