#!/usr/bin/env python3
# Extrae los textos visibles del Worldfall descompilado (ver textos_nuevos.py). Uso:
#   python3 extrae_textos.py <carpeta_descompilada> <salida.json>
import os, re, sys, json, collections
SRC = sys.argv[1]
OUT = sys.argv[2]
SKIPF = re.compile(r'(Test|Dump|PerfLog|Log|GpuRenderer|GpuShaders|MusicSynth|Synth|Device|ShotsTest|Gallery|ModelRoom)\.cs$')
SKIPLINE = re.compile(r'Debug\.Log|Log\.(Write|Info|Warn|Error|Line|Say|Add|Note)\b|LogInfo|LogWarning|"service:|throw new|PerfLog|Console\.Write|\.Append\(|AppendLine|StringBuilder|PlayerPrefs|Shader\.|GetType\(|\.Find\(|Resources\.Load|Settings\.(Get|Set)|Prefs')

def unesc(s):
    out=[];i=0
    while i<len(s):
        c=s[i]
        if c=='\\' and i+1<len(s):
            d=s[i+1]; i+=2
            if d=='n': out.append('\n')
            elif d=='t': out.append('\t')
            elif d=='"': out.append('"')
            elif d=='\\': out.append('\\')
            elif d=='\'': out.append('\'')
            elif d=='u': out.append(chr(int(s[i:i+4],16))); i+=4
            elif d=='0': pass
            else: out.append(d)
        else: out.append(c); i+=1
    return ''.join(out)

def tokens(line):
    """yield (kind, text) kinds: str, istr (interpolated raw content), other char"""
    i=0;n=len(line)
    while i<n:
        c=line[i]
        if c=='/' and line[i:i+2]=='//': return
        if c=='$' and i+1<n and line[i+1]=='"':
            j=i+2;depth=0;buf=[]
            while j<n:
                ch=line[j]
                if depth==0 and ch=='\\': buf.append(line[j:j+2]); j+=2; continue
                if depth==0 and ch=='"': break
                if ch=='{':
                    if depth==0 and line[j:j+2]=='{{': buf.append('{{'); j+=2; continue
                    depth+=1
                elif ch=='}':
                    if depth==0 and line[j:j+2]=='}}': buf.append('}}'); j+=2; continue
                    depth-=1
                elif ch=='"' and depth>0:
                    # string inside interpolation expression
                    k=j+1
                    while k<n and line[k]!='"':
                        if line[k]=='\\': k+=1
                        k+=1
                    buf.append(line[j:k+1]); j=k+1; continue
                buf.append(ch); j+=1
            yield ('istr',''.join(buf)); i=j+1; continue
        if c=='@' and i+1<n and line[i+1]=='"':
            j=i+2
            while j<n and not (line[j]=='"' and line[j+1:j+2]!='"'):
                j+= 2 if line[j]=='"' else 1
            yield ('other','@str'); i=j+1; continue
        if c=='"':
            j=i+1
            while j<n and line[j]!='"':
                if line[j]=='\\': j+=1
                j+=1
            yield ('str',line[i+1:j]); i=j+1; continue
        if c=="'":
            j=i+1
            while j<n and line[j]!="'":
                if line[j]=='\\': j+=1
                j+=1
            yield ('other','chr'); i=j+1; continue
        yield ('other',c); i+=1

def interp_template(raw):
    out=[];i=0;k=0;n=len(raw)
    while i<n:
        if raw.startswith('{{',i): out.append('{{'); i+=2; continue
        if raw.startswith('}}',i): out.append('}}'); i+=2; continue
        if raw[i]=='{':
            depth=1;j=i+1
            while j<n and depth: 
                if raw[j]=='{': depth+=1
                elif raw[j]=='}': depth-=1
                j+=1
            out.append('{%d}'%k); k+=1; i=j; continue
        out.append(raw[i]); i+=1
    t=unesc(''.join(out)).replace('{{','{').replace('}}','}')
    return t,k

def chains(toks):
    """find concat chains: items separated by top-level '+' with >=1 str"""
    res=[]
    # build flat list of items: split sequence into segments at depth-0 boundaries
    i=0;n=len(toks)
    while i<n:
        if toks[i][0]=='str':
            # expand left: previous operand if preceded by '+'
            # collect chain going right
            items=[('s',toks[i][1])]
            j=i+1
            while True:
                k=j
                while k<n and toks[k]==('other',' '): k+=1
                if k<n and toks[k]==('other','+') and not (k+1<n and toks[k+1][1] in '+='):
                    k+=1
                    while k<n and toks[k]==('other',' '): k+=1
                    if k<n and toks[k][0]=='str':
                        items.append(('s',toks[k][1])); j=k+1; continue
                    # operand: until top-level + , ; ) ? : 
                    depth=0;m=k
                    while m<n:
                        t=toks[m]
                        if t[0]=='other':
                            ch=t[1]
                            if ch in '([{': depth+=1
                            elif ch in ')]}':
                                if depth==0: break
                                depth-=1
                            elif depth==0 and ch in '+,;?:': break
                        m+=1
                    if m==k: break
                    items.append(('e',None)); j=m; continue
                break
            # left operand
            back=i-1
            while back>=0 and toks[back]==('other',' '): back-=1
            if back>=0 and toks[back]==('other','+'):
                items.insert(0,('e',None))
            if len(items)>1: res.append(items)
            i=j if j>i else i+1
        else: i+=1
    return res

def chain_template(items):
    out=[];k=0
    for kind,v in items:
        if kind=='s': out.append(unesc(v).replace('{','{{').replace('}','}}') if False else unesc(v))
        else: out.append('{%d}'%k); k+=1
    return ''.join(out),k

WORD=re.compile(r"[A-Za-z]{2,}")
def visible(t, has_holes):
    s=t.strip()
    if len(s)<2: return False
    core=re.sub(r'\{\d\}','',s)
    if not re.search(r'[A-Za-z]{2,}',core): return False
    if re.fullmatch(r'[a-z0-9_.:/\\\-#<>=|]+',core.strip()): return False   # ids, rutas
    if re.fullmatch(r'[A-Za-z0-9_]+',core.strip()) and '_' in core: return False
    if re.search(r'\.(png|wav|ogg|bin|json|txt|dll|asset)\b',core): return False
    if re.match(r'^(ui|fx|sfx|ambient|music|event|tile|biome|actor|unit|status|trait|item|power)/',core): return False
    if re.fullmatch(r'<[^>]+>',core.strip()): return False
    if '|' in core and ' ' not in core: return False
    if core.startswith('event:') or 'FirstPerson' in core: return False
    if re.fullmatch(r'[A-Z][a-z]+([A-Z][a-z0-9]+)+',core.strip()): return False
    if re.fullmatch(r'[A-Z.#]{4,}',core.strip()): return False
    # must look like language: uppercase start, or contain space between words, or common short words
    words=WORD.findall(core)
    if not words: return False
    if ' ' not in core.strip() and core.strip()[0].islower() and len(words)==1:
        return False  # single lowercase token: almost always an id
    return True

exact=collections.OrderedDict(); temps=collections.OrderedDict()
for root,_,files in os.walk(SRC):
    for f in sorted(files):
        if not f.endswith('.cs') or SKIPF.search(f): continue
        src=os.path.join(root,f)
        for ln,line in enumerate(open(src,encoding='utf-8',errors='replace')):
            if SKIPLINE.search(line): continue
            st=line.strip()
            if st.startswith('case ') or st.startswith('[') : 
                # case labels are ids; but "case X: return "text"" keep
                if ' return ' not in st and '=>' not in st: continue
            toks=list(tokens(line))
            enCadena=set()
            for items in chains(toks):
                for kind,v in items:
                    if kind=='s': enCadena.add(v)
            if any(t[0]=='istr' for t in toks) or any(t[0]=='str' for t in toks):
                for t in toks:
                    if t[0]=='istr':
                        tp,k=interp_template(t[1])
                        if visible(tp,k>0):
                            (temps if k else exact).setdefault(tp.strip(),f)
                    elif t[0]=='str':
                        if t[1] in enCadena: continue
                        s=unesc(t[1])
                        if visible(s,False):
                            if re.search(r'\{\d(:[^}]*)?\}',s):
                                s2=re.sub(r'\{(\d)(:[^}]*)?\}',r'{\1}',s)
                                temps.setdefault(s2.strip(),f)
                            else: exact.setdefault(s.strip(),f)
                for items in chains(toks):
                    tp,k=chain_template(items)
                    if k and visible(tp,True) and len(re.sub(r'\{\d\}','',tp).strip())>=3:
                        temps.setdefault(tp.strip(),f)
# drop templates with adjacent holes or no anchor word >=3
def okt(t):
    if re.search(r'\{\d\}\{\d\}',t): return False
    lits=re.sub(r'\{\d\}',' ',t)
    return re.search(r'[A-Za-z\']{3,}',lits) is not None
temps=collections.OrderedDict((k,v) for k,v in temps.items() if okt(k))
json.dump({'exact':exact,'temps':temps},open(OUT,'w'),ensure_ascii=False,indent=0)
print(len(exact),'exactas',len(temps),'plantillas')
c=collections.Counter(list(exact.values())+list(temps.values()))
print(c.most_common(25))
