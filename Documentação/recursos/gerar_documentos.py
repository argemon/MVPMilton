"""Gera Word a partir das fontes Markdown; conversão/revisão via render_docx.py.

Usar o Python do runtime disponibilizado por load_workspace_dependencies.
--mapas usa a paginação extraída dos PDFs para preencher campos e índices.
"""
from pathlib import Path
import re, json, argparse
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK, WD_LINE_SPACING
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from PIL import Image

BASE=Path(__file__).resolve().parents[1]
OUT=BASE/'entrega';OUT.mkdir(exist_ok=True)
REFS=json.loads((BASE/'fontes/referencias.json').read_text(encoding='utf-8'))
DATE='24 nov. 2026'
DOCS=[('monografia','TCC Gestão Comercial',2),('documentacao-tecnica','Documentação técnica',1),('manual-do-usuario','Manual do usuário',2)]
parser=argparse.ArgumentParser();parser.add_argument('--mapas',type=Path);args=parser.parse_args()
MAPS=json.loads(args.mapas.read_text(encoding='utf-8')) if args.mapas else {}

def el(name,attrs={}):
    e=OxmlElement(name)
    for k,v in attrs.items():e.set(qn(k),str(v))
    return e
def bookmark(p,name,ident):
    p._p.insert(0,el('w:bookmarkStart',{'w:id':ident,'w:name':name}));p._p.append(el('w:bookmarkEnd',{'w:id':ident}))
def field(p,instruction,cached=''):
    p.add_run()._r.append(el('w:fldChar',{'w:fldCharType':'begin'}))
    instr=el('w:instrText',{'xml:space':'preserve'});instr.text=instruction;p.add_run()._r.append(instr)
    p.add_run()._r.append(el('w:fldChar',{'w:fldCharType':'separate'}));p.add_run(str(cached))
    p.add_run()._r.append(el('w:fldChar',{'w:fldCharType':'end'}))
def breakable(s):
    # Quebra sem hifenização de identificadores longos, sem alterar o Markdown.
    s=re.sub(r'([/_.])',r'\1\u200b',s) if False else re.sub(r'([/_.])',lambda m:m.group(1)+'\u200b',s)
    s=re.sub(r'(?<=[a-z])(?=[A-Z])','\u200b',s)
    s=re.sub(r'([0-9a-f]{16})(?=[0-9a-f]{16})',r'\1\u200b',s) if False else s
    return s
def inline(p,text,small=False):
    for part in re.split(r'(\*\*.*?\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))',text):
        if not part:continue
        bold=part.startswith('**');code=part.startswith('`')
        if bold: part=part[2:-2]
        elif code:part=part[1:-1]
        elif part.startswith('[') and '](' in part:part=re.sub(r'\[([^\]]+)\]\(([^)]+)\)',r'\1 (\2)',part)
        r=p.add_run(breakable(part));r.bold=bold
        if small:r.font.size=Pt(10)
        if code:r.font.name='Consolas';r.font.size=Pt(9)

for key,short,toclevel in DOCS:
    md=(BASE/'fontes'/f'{key}.md').read_text(encoding='utf-8')
    lines=md.splitlines()
    headings=[(len(m.group(1)),m.group(2)) for l in lines if (m:=re.match(r'^(#{1,3}) (.+)',l))]
    main_started=False;headingmeta=[];counter=0
    for level,title in headings:
        if re.match(r'^1 ',title):main_started=True
        if main_started and level<=toclevel:
            counter+=1;headingmeta.append((level,title,f'h{counter}'))
    figuremeta=[]
    for l in lines:
        if (m:=re.match(r'!\[([^\]]+)\]\(([^)]+)\)',l)):
            figuremeta.append(m.group(1).replace('Figura - ',''))
    d=Document(); sec=d.sections[0]
    sec.page_width=Cm(21);sec.page_height=Cm(29.7)
    sec.top_margin=Cm(3);sec.left_margin=Cm(3);sec.right_margin=Cm(2);sec.bottom_margin=Cm(2)
    sec.header_distance=Cm(1.4);sec.footer_distance=Cm(1.0)
    normal=d.styles['Normal'];normal.font.name='Times New Roman';normal.font.size=Pt(12)
    pf=normal.paragraph_format;pf.line_spacing=1.5;pf.space_after=Pt(6);pf.first_line_indent=Cm(1.25);pf.widow_control=True
    normal.font.color.rgb=RGBColor(0,0,0)
    for name in ['Title','Heading 1','Heading 2','Heading 3']:
        s=d.styles[name];s.font.name='Times New Roman';s.font.color.rgb=RGBColor(0,0,0)
        s.font.size=Pt(12 if name!='Title' else 16);s.font.bold=True
        s.paragraph_format.first_line_indent=Cm(0);s.paragraph_format.space_before=Pt(14);s.paragraph_format.space_after=Pt(9);s.paragraph_format.line_spacing=1.5;s.paragraph_format.keep_with_next=True
    for name in ['Caption','TOC Heading']:
        if name in d.styles:d.styles[name].font.name='Times New Roman';d.styles[name].font.color.rgb=RGBColor(0,0,0)
    d.styles['Caption'].font.size=Pt(10);d.styles['Caption'].font.italic=False;d.styles['Caption'].font.bold=False
    # O modelo interno do runtime pode conter fontes temáticas e borda azul.
    # Remover também a herança visual, além de definir a fonte nominal.
    for style in d.styles:
        for border in list(style.element.iter(qn('w:pBdr'))):border.getparent().remove(border)
        for fonts in style.element.iter(qn('w:rFonts')):
            for attr in list(fonts.attrib):
                if attr.endswith('Theme'):del fonts.attrib[attr]
            fonts.set(qn('w:ascii'),'Times New Roman');fonts.set(qn('w:hAnsi'),'Times New Roman');fonts.set(qn('w:eastAsia'),'Times New Roman')
    for name in ['List Number','List Bullet']:
        d.styles[name].font.name='Times New Roman';d.styles[name].font.size=Pt(12)
        d.styles[name].paragraph_format.line_spacing=1.5;d.styles[name].paragraph_format.first_line_indent=Cm(0)
    settings=d.settings.element;settings.append(el('w:updateFields',{'w:val':'false'}))
    lang=el('w:lang',{'w:val':'pt-BR'});normal.element.get_or_add_rPr().append(lang)
    # Pré-texto sem número; contagem reinicia na folha de rosto via seção principal.
    head=sec.header.paragraphs[0];head.alignment=WD_ALIGN_PARAGRAPH.RIGHT
    if key!='monografia':field(head,' PAGE ','1')
    footer=sec.footer.paragraphs[0];footer.paragraph_format.first_line_indent=Cm(0);footer.alignment=WD_ALIGN_PARAGRAPH.CENTER
    rr=footer.add_run('Versão para revisão acadêmica • 24/11/2026');rr.font.size=Pt(9)
    headings_iter=iter(headingmeta);nextheading=next(headings_iter,None);fig_no=0;table_no=0;i=0;first=True;main_started=False
    while i<len(lines):
        l=lines[i].strip();i+=1
        if not l:continue
        if l=='<!-- page -->':d.add_page_break();continue
        if l=='<!-- toc -->':
            d.add_page_break();p=d.add_paragraph('Sumário','Heading 1')
            # Campo TOC com cache reproduzível. Atualizável no Word pelo usuário.
            start=d.add_paragraph();start.paragraph_format.space_after=Pt(0);start.paragraph_format.line_spacing=1
            start.add_run()._r.append(el('w:fldChar',{'w:fldCharType':'begin'}));instr=el('w:instrText',{'xml:space':'preserve'});instr.text=f' TOC \\o "1-{toclevel}" \\h \\z \\u ';start.add_run()._r.append(instr);start.add_run()._r.append(el('w:fldChar',{'w:fldCharType':'separate'}))
            for level,title,bm in headingmeta:
                p=d.add_paragraph();p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.left_indent=Cm((level-1)*0.5);p.paragraph_format.space_after=Pt(3);p.paragraph_format.line_spacing=1
                p.paragraph_format.tab_stops.add_tab_stop(Cm(15.6),WD_ALIGN_PARAGRAPH.RIGHT)
                p.add_run(title+'\t');field(p,f' PAGEREF {bm} \\h ',MAPS.get(key,{}).get(title,'—'))
            end=d.add_paragraph();end.add_run()._r.append(el('w:fldChar',{'w:fldCharType':'end'}));end.paragraph_format.space_after=Pt(0)
            if key!='monografia':d.add_page_break()
            continue
        if l=='<!-- lists -->':
            d.add_page_break();d.add_paragraph('Lista de ilustrações','Heading 1')
            for n,title in enumerate(figuremeta,1):
                p=d.add_paragraph();p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.line_spacing=1;p.paragraph_format.space_after=Pt(6)
                p.add_run(f'Figura {n} — {title}\t');field(p,f' PAGEREF fig{n} \\h ',MAPS.get(key,{}).get(f'Figura {n} — {title}','—'))
            d.add_paragraph('Lista de tabelas','Heading 1');p=d.add_paragraph('Tabela 1 — Resultados históricos identificados\t');p.paragraph_format.first_line_indent=Cm(0);field(p,' PAGEREF tab1 \\h ',MAPS.get(key,{}).get('Tabela 1 — Resultados históricos identificados','—'))
            d.add_paragraph('Lista de siglas','Heading 1')
            for t in ['API — Interface de programação de aplicações','BFF — Backend for Frontend','BRL — Real brasileiro','BSON — Binary JSON','CSV — Comma Separated Values','DTO — Data Transfer Object','HTTP — Hypertext Transfer Protocol','TLS — Transport Layer Security','UML — Unified Modeling Language','UUID — Universally Unique Identifier']:
                p=d.add_paragraph(t);p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.line_spacing=1;p.paragraph_format.space_after=Pt(3)
            continue
        if l=='<!-- references -->':
            used=REFS if key=='monografia' else [r for r in REFS if r['id']!='sommerville' and r['id']!='normalizacao' and r['id']!='swebok']
            for ref in sorted(used,key=lambda r:(r['autor'],r['citacao'])):
                p=d.add_paragraph();p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.line_spacing=1;p.paragraph_format.space_after=Pt(12)
                p.add_run(ref['autor']+'. ');r=p.add_run(ref['titulo']);r.bold=True
                suffix=ref['citacao'].split('s.d.')[-1] if 's.d.' in ref['citacao'] else ''
                p.add_run(f'. {ref["edicao"]}. {ref["entidade"]}, '+('['+ref['ano']+']'+suffix if ref['ano']=='s.d.' else ref['ano'])+f'. Disponível em: {ref["url"]}. Acesso em: {DATE}.')
            continue
        if l.startswith('<!--'):continue
        if (m:=re.match(r'^(#{1,3}) (.+)',l)):
            level=len(m.group(1));title=m.group(2)
            if first:
                if key=='monografia':
                    sec.different_first_page_header_footer=True
                    for value,gap in [('[INSTITUIÇÃO A PREENCHER]',0),('[CURSO A PREENCHER]',0),('[AUTOR A PREENCHER]',1.8)]:
                        p=d.add_paragraph(value);p.alignment=WD_ALIGN_PARAGRAPH.CENTER;p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.space_before=Cm(gap)
                p=d.add_paragraph(title,'Title');p.alignment=WD_ALIGN_PARAGRAPH.CENTER;first=False
                if key=='monografia':
                    p.paragraph_format.space_before=Cm(3);p.paragraph_format.space_after=Cm(3.5)
                    for value in ['[CIDADE A PREENCHER]','[ANO ACADÊMICO A PREENCHER]']:
                        p=d.add_paragraph(value);p.alignment=WD_ALIGN_PARAGRAPH.CENTER;p.paragraph_format.first_line_indent=Cm(0)
                    while i<len(lines) and lines[i].strip()!='<!-- page -->':i+=1
                continue
            if title.startswith('1 ') and not main_started:
                main_started=True
                if key=='monografia':
                    # Nova seção sem quebra adicional (sumário já termina em quebra).
                    from docx.enum.section import WD_SECTION_START
                    s=d.add_section(WD_SECTION_START.NEW_PAGE);s.header.is_linked_to_previous=False
                    s.different_first_page_header_footer=False
                    s.header.paragraphs[0].alignment=WD_ALIGN_PARAGRAPH.RIGHT
                    field(s.header.paragraphs[0],' PAGE ','1')
                    count=sum(1 for z in lines[:i] if z.strip()=='<!-- page -->') # valor provisório; mapas corrigem o início.
                    startpage=MAPS.get(key,{}).get('__inicio__',10)-1
                    s._sectPr.append(el('w:pgNumType',{'w:start':max(1,startpage)}))
            chapter_break=level==1 and main_started and title!='1 Introdução' and not (key!='monografia' and title.startswith('1 '))
            p=d.add_paragraph(title,'Heading '+str(level))
            if chapter_break:p.paragraph_format.page_break_before=True
            if nextheading and title==nextheading[1]:bookmark(p,nextheading[2],100+int(nextheading[2][1:]));nextheading=next(headings_iter,None)
            continue
        if (m:=re.match(r'!\[([^\]]+)\]\(([^)]+)\)',l)):
            fig_no+=1;title=m.group(1).replace('Figura - ','');path=(BASE/'fontes'/m.group(2)).resolve()
            cap=d.add_paragraph(style='Caption');cap.paragraph_format.first_line_indent=Cm(0);cap.paragraph_format.keep_with_next=True
            cap.add_run('Figura ');field(cap,' SEQ Figura \\* ARABIC ',str(fig_no));cap.add_run(' — '+title);bookmark(cap,'fig'+str(fig_no),1000+fig_no)
            w,h=Image.open(path).size;maxw=15.7;maxh=19.5 if key=='documentacao-tecnica' else 17.5
            scale=min(maxw/w,maxh/h)
            p=d.add_paragraph();p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.keep_with_next=True;p.paragraph_format.line_spacing=1;p.alignment=WD_ALIGN_PARAGRAPH.CENTER
            p.add_run().add_picture(str(path),width=Cm(w*scale),height=Cm(h*scale))
            p=d.add_paragraph('Fonte: elaboração própria a partir do código analisado.',style='Caption');p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.space_after=Pt(10)
            continue
        if l.startswith('|'):
            rows=[l]
            while i<len(lines) and lines[i].strip().startswith('|'):rows.append(lines[i].strip());i+=1
            parsed=[[c.strip() for c in row.strip('|').split('|')] for row in rows]
            parsed=[row for row in parsed if not all(re.fullmatch(r'[- :]+',c) for c in row)]
            table_no+=1
            title='Resultados históricos identificados' if key=='monografia' else parsed[0][0]+' e '+parsed[0][-1]
            cap=d.add_paragraph(style='Caption');cap.paragraph_format.first_line_indent=Cm(0);cap.paragraph_format.keep_with_next=True
            cap.add_run('Tabela ');field(cap,' SEQ Tabela \\* ARABIC ',str(table_no));cap.add_run(' — '+title);bookmark(cap,'tab'+str(table_no),2000+table_no)
            t=d.add_table(rows=0,cols=len(parsed[0]));t.alignment=WD_TABLE_ALIGNMENT.CENTER;t.autofit=False
            # Colunas adaptadas ao conteúdo para evitar siglas e identificadores comprimidos.
            if len(parsed[0])==2: widths=[5.5,10.2]
            elif len(parsed[0])==3: widths=[4.2,5.2,6.3]
            else:widths=[15.7/len(parsed[0])]*len(parsed[0])
            for col,width in zip(t.columns,widths):col.width=Cm(width)
            for ri,row in enumerate(parsed):
                cells=t.add_row().cells
                t.rows[-1]._tr.get_or_add_trPr().append(el('w:cantSplit'))
                if ri==0:t.rows[-1]._tr.get_or_add_trPr().append(el('w:tblHeader'))
                for j,(c,content) in enumerate(zip(cells,row)):
                    c.width=Cm(widths[j]);c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
                    tcPr=c._tc.get_or_add_tcPr();marg=el('w:tcMar')
                    for side in ['top','left','bottom','right']:marg.append(el('w:'+side,{'w:w':90,'w:type':'dxa'}))
                    tcPr.append(marg)
                    if ri==0:tcPr.append(el('w:shd',{'w:fill':'EAEAEA'}))
                    p=c.paragraphs[0];p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.line_spacing=1;p.paragraph_format.space_after=Pt(3);p.paragraph_format.space_before=Pt(3)
                    inline(p,content,small=True)
                    if ri==0:
                        for r in p.runs:r.bold=True
            p=d.add_paragraph('Fonte: '+('registros históricos do projeto; ver limites no texto.' if key=='monografia' else 'elaboração própria com base nos artefatos da versão analisada.'),style='Caption');p.paragraph_format.first_line_indent=Cm(0)
            continue
        if l.startswith('```'):
            code=[]
            while i<len(lines) and not lines[i].strip().startswith('```'):code.append(lines[i]);i+=1
            i+=1
            for codeline in code:
                p=d.add_paragraph();p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.line_spacing=1;p.paragraph_format.space_after=Pt(5)
                r=p.add_run(codeline);r.font.name='Consolas';r.font.size=Pt(9)
            continue
        # Um parágrafo Markdown por bloco; listas sequenciais recebem número literal para reiniciar por procedimento.
        content=[l]
        while i<len(lines) and lines[i].strip() and not re.match(r'^(#|\||!\[|<!--|```|\d+\. |[-*] )',lines[i].strip()):content.append(lines[i].strip());i+=1
        value=' '.join(content);p=d.add_paragraph();inline(p,value)
        p.alignment=WD_ALIGN_PARAGRAPH.JUSTIFY
        if value.startswith('Fonte:') or value.startswith('**Implementação:**'):
            p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.line_spacing=1
            for r in p.runs:r.font.size=Pt(10)
        if re.match(r'^\d+\. ',value):p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.left_indent=Cm(0.4)
        if key=='monografia' and not main_started:
            if value.startswith('['):p.alignment=WD_ALIGN_PARAGRAPH.CENTER;p.paragraph_format.first_line_indent=Cm(0)
            if value.startswith('Trabalho de Conclusão') or value.startswith('Orientador'):
                p.paragraph_format.left_indent=Cm(7);p.paragraph_format.first_line_indent=Cm(0);p.paragraph_format.line_spacing=1
    d.core_properties.title=lines[0].lstrip('# ');d.core_properties.subject='Protótipo acadêmico Gestão Comercial';d.core_properties.author='[AUTOR A PREENCHER]';d.core_properties.keywords='TCC; gestão comercial; revisão acadêmica'
    d.save(OUT/(key+'.docx'))
    print(key, 'Word gerado',len(d.paragraphs),'parágrafos',len(d.tables),'tabelas',fig_no,'figuras')
