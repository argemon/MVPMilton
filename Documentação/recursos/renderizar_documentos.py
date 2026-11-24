"""Gera, pagina e renderiza os três documentos com o renderizador do skill.

Exemplo e dependências estão em Documentação/README.md.
O PDF é sempre convertido do Word, evitando duas versões de conteúdo.
"""
from pathlib import Path
import os, subprocess, argparse, json, shutil

BASE=Path(__file__).resolve().parents[1]; ROOT=BASE.parent
parser=argparse.ArgumentParser()
parser.add_argument('--renderer',type=Path,required=True)
parser.add_argument('--poppler-dir',type=Path,required=True)
parser.add_argument('--soffice-dir',type=Path,default=ROOT/'.local/documentacao-tools/libreoffice/program')
parser.add_argument('--python',type=Path,default=Path(__import__('sys').executable))
parser.add_argument('--qa',type=Path,default=ROOT/'.local/documentacao-qa')
parser.add_argument('--max-passes',type=int,default=4)
args=parser.parse_args()
for p in [args.renderer,args.poppler_dir,args.soffice_dir,args.python]:
    if not p.exists(): raise SystemExit('Dependência ausente: '+str(p))
qa=args.qa.resolve();qa.mkdir(parents=True,exist_ok=True);temp=qa/'temp';temp.mkdir(exist_ok=True)
env=os.environ.copy();env['PATH']=str(args.soffice_dir.resolve())+os.pathsep+str(args.poppler_dir.resolve())+os.pathsep+env.get('PATH','')
env['TMP']=str(temp);env['TEMP']=str(temp)
log=qa/'renderizacao.log'
def run(cmd):
    result=subprocess.run([str(x) for x in cmd],env=env,cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace')
    with log.open('a',encoding='utf-8') as f:f.write('\n'+result.stdout)
    if result.returncode:raise RuntimeError('Falha de geração/renderização; consultar '+str(log))
    return result.stdout
run([args.python,BASE/'recursos/consolidar_fontes.py'])
mapsfile=qa/'mapas-paginas.json';stable=False
for attempt in range(1,args.max_passes+1):
    before=mapsfile.read_bytes() if mapsfile.exists() else None
    cmd=[args.python,BASE/'recursos/gerar_documentos.py']
    if before:cmd+=['--mapas',mapsfile]
    print('Passagem',attempt,': gerar Word e revisar paginação',flush=True);run(cmd)
    for name in ['monografia','documentacao-tecnica','manual-do-usuario']:
        print('Renderizando',name,flush=True)
        run([args.python,args.renderer,BASE/'entrega'/f'{name}.docx','--output_dir',qa/name,'--emit_pdf','--dpi','110','--verbose'])
    print(run([args.python,BASE/'recursos/mapear_paginas.py','--qa',qa]),flush=True)
    if before==mapsfile.read_bytes():stable=True;break
if not stable:raise RuntimeError('Paginação ainda não estabilizou; revisar antes de entregar.')
for name in ['monografia','documentacao-tecnica','manual-do-usuario']:
    shutil.copy2(qa/name/(name+'.pdf'),BASE/'entrega'/(name+'.pdf'))
(BASE/'evidencias/paginacao.json').write_bytes((qa/'inspecao-estrutural.json').read_bytes())
print('Paginação estabilizada e PDFs copiados. Revisão visual ainda necessária.',flush=True)
