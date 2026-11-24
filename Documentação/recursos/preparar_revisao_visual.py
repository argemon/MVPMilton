"""Monta painéis de revisão sem reduzir os pixels das páginas renderizadas."""
from pathlib import Path
import json,argparse
from PIL import Image,ImageDraw
BASE=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--qa',type=Path,default=BASE.parent/'.local/documentacao-qa');args=parser.parse_args()
out=args.qa/'paineis';out.mkdir(exist_ok=True)
index=[]
for key in ['monografia','documentacao-tecnica','manual-do-usuario']:
    # Usar quantidade real do PDF; ignora PNG antigo excedente de uma passagem anterior.
    from pypdf import PdfReader
    count=len(PdfReader(args.qa/key/(key+'.pdf')).pages)
    pages=[args.qa/key/f'page-{n}.png' for n in range(1,count+1)]
    for start in range(0,len(pages),6):
        group=pages[start:start+6];images=[Image.open(p).convert('RGB') for p in group]
        w=max(p.width for p in images);h=max(p.height for p in images)
        canvas=Image.new('RGB',(w*3,(h+28)*2),'#B9BDC2');draw=ImageDraw.Draw(canvas)
        for j,img in enumerate(images):
            x=(j%3)*w;y=(j//3)*(h+28);draw.text((x+10,y+6),f'{key} / folha PDF {start+j+1}',fill='black');canvas.paste(img,(x,y+28))
        dest=out/f'{key}-{start+1:03d}.png';canvas.save(dest)
        index.append({'painel':str(dest.resolve()),'documento':key,'folhas':list(range(start+1,start+len(images)+1)),'escala':'pixels originais, sem redução'})
(args.qa/'paineis.json').write_text(json.dumps(index,ensure_ascii=False,indent=2),encoding='utf-8')
print(len(index),'painéis preparados, preservando resolução das páginas.')
