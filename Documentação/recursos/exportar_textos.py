"""Exporta versões Markdown completas para leitura sem executar o gerador Word."""
from pathlib import Path
import json,re
BASE=Path(__file__).resolve().parents[1]
refs=json.loads((BASE/'fontes/referencias.json').read_text(encoding='utf-8'))
for name in ['monografia','documentacao-tecnica','manual-do-usuario']:
    md=(BASE/'fontes'/(name+'.md')).read_text(encoding='utf-8')
    selected=refs if name=='monografia' else [r for r in refs if r['id'] not in ['sommerville','swebok','normalizacao']]
    bibliography=[]
    for r in sorted(selected,key=lambda r:(r['autor'],r['citacao'])):
        suffix=r['citacao'].split('s.d.')[-1] if 's.d.' in r['citacao'] else ''
        year='['+r['ano']+']'+suffix if r['ano']=='s.d.' else r['ano']
        bibliography.append(f'{r["autor"]}. **{r["titulo"]}**. {r["edicao"]}. {r["entidade"]}, {year}. Disponível em: [{r["titulo"]}]({r["url"]}). Acesso em: 24 nov. 2026.')
    md=md.replace('<!-- references -->','\n\n'.join(bibliography))
    headings=re.findall(r'^#{1,2} (.+)',md,re.M)
    toc='\n'.join('- '+h for h in headings[1:])
    md=md.replace('<!-- toc -->','## Sumário de navegação\n\n'+toc)
    md=md.replace('<!-- lists -->','## Ilustrações\n\n'+ '\n'.join('- '+m for m in re.findall(r'!\[Figura - ([^\]]+)\]',md)))
    md=re.sub(r'<!--.*?-->','',md,flags=re.S)
    (BASE/'entrega'/(name+'.md')).write_text(md,encoding='utf-8')
print('Três textos Markdown completos exportados.')
