#!/usr/bin/env python3
"""Tras una actualizacion de Worldfall: lista los textos que aun no estan en worldfall_es.txt.

  1. Descompila el Worldfall.dll nuevo (solo lectura, fuera del repo):
       dotnet tool install -g ilspycmd
       DOTNET_ROLL_FORWARD=Major ilspycmd -p -o /tmp/wf Worldfall.dll
  2. python3 dev/herramientas/extrae_textos.py /tmp/wf /tmp/wf_textos.json
  3. python3 dev/herramientas/textos_nuevos.py /tmp/wf_textos.json > nuevos.txt
  4. Traduce nuevos.txt (formato «Ingles => Espanol») y anadelo a Traducciones/worldfall_es.txt.
"""
import json, sys, os
base = os.path.join(os.path.dirname(__file__), '..', '..', 'worldfall-expansion', 'Traducciones', 'worldfall_es.txt')
conocidas = set()
for l in open(base, encoding='utf-8'):
    if l.startswith('#') or ' => ' not in l: continue
    conocidas.add(l.split(' => ', 1)[0].strip())
d = json.load(open(sys.argv[1], encoding='utf-8'))
def esc(t): return t.replace('\\', '\\\\').replace('\n', '\\n').replace('\t', '\\t').replace('\r', '')
n = 0
for grupo in ('exact', 'temps'):
    for t, f in d[grupo].items():
        if esc(t) not in conocidas:
            print('%s => ' % esc(t)); n += 1
print('# %d textos nuevos' % n, file=sys.stderr)
