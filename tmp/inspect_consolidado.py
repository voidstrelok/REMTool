from pathlib import Path
from zipfile import ZipFile
import xml.etree.ElementTree as ET
from collections import Counter
import json, re, base64, io, struct

ROOT = Path('C:/Users/Usuario/Desktop/2025/Consolidados')
NS = {'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
results=[]
def out(x): results.append(x)
def scrub(s):
    s = re.sub(r'(?i)(password|pwd)\s*=\s*[^;"<\r\n]*', r'\1=[REDACTED]', s)
    s = re.sub(r'(PostgreSQL\.Database\(")[^"]+(",\s*")[^"]+',r'\1[SERVER]\2[DATABASE]',s)
    return s
for fname in ['Consolidado REM Serie A 2026.xlsx','BaseConsolidado2026.xlsx']:
    with ZipFile(ROOT/fname) as z:
        out({'file':fname,'bytes':(ROOT/fname).stat().st_size,'largest_parts': sorted([(i.filename,i.file_size,i.compress_size) for i in z.infolist()],key=lambda x:x[1],reverse=True)[:5]})
        wb=ET.fromstring(z.read('xl/workbook.xml'))
        rel={r.attrib['Id']:r.attrib['Target'] for r in ET.fromstring(z.read('xl/_rels/workbook.xml.rels'))}
        strings=[]
        if 'xl/sharedStrings.xml' in z.namelist():
            strings=[''.join(si.itertext()) for si in ET.fromstring(z.read('xl/sharedStrings.xml'))]
        def val(c):
            v=c.find('s:v',NS)
            return strings[int(v.text)] if c.get('t')=='s' and v is not None else (v.text if v is not None else '')
        for sh in wb.find('s:sheets',NS):
            target=rel[sh.get('{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id')]
            part=target.lstrip('/') if target.startswith('/') else 'xl/'+target
            # Large source sheet needs only headers/dimensions; don't load all raw rows.
            if fname.startswith('Base'):
                with z.open(part) as f:
                    head=f.read(18000).decode('utf-8')
                out({'sheet':sh.get('name'),'part':part,'head':head[:2200]})
                continue
            tree=ET.fromstring(z.read(part)); cells=tree.findall('.//s:sheetData/s:row/s:c',NS)
            forms=[(c.get('r'),c.find('s:f',NS).text or '',c.find('s:f',NS).attrib) for c in cells if c.find('s:f',NS) is not None]
            lookups=[x for x in forms if 'VLOOKUP' in x[1]]
            errors=[(c.get('r'),val(c),c.find('s:f',NS).text if c.find('s:f',NS) is not None else '') for c in cells if c.get('t')=='e']
            refs=Counter(re.findall(r'FILTRO!\$?[A-Z]+\$?\d*:\$?[A-Z]+\$?\d*',' '.join(f[1] for f in forms)))
            out({'sheet':sh.get('name'),'dimension':tree.find('s:dimension',NS).get('ref'),'formulas':len(forms),'explicit_vlookups':len(lookups),'formula_samples':lookups[:3] or forms[:3],'lookup_ranges':dict(refs),'errors':errors,'codes_A':[(c.get('r'),val(c)) for c in cells if re.fullmatch(r'A\d+',c.get('r')) and val(c)][:5]})
        for name in z.namelist():
            if name=='xl/connections.xml' or re.fullmatch(r'xl/tables/table\d+.xml',name):
                tree=ET.fromstring(z.read(name))
                if 'tables' in name: out({'part':name,'name':tree.get('name'),'ref':tree.get('ref'),'headers':[c.get('name') for c in tree.find('s:tableColumns',NS)]})
                else: out({'part':name,'xml':scrub(z.read(name).decode())})
            if name.startswith('customXml/') and name.endswith('.xml'):
                raw=z.read(name)
                tree=ET.fromstring(raw)
                if not tree.tag.endswith('DataMashup'): continue
                data=base64.b64decode(tree.text)
                package_len=struct.unpack('<I',data[4:8])[0]
                try:
                    with ZipFile(io.BytesIO(data[8:8+package_len])) as mz:
                        for p in mz.namelist():
                            if p.endswith('.m'): out({'mashup':p,'source':scrub(mz.read(p).decode('utf-8-sig'))})
                except Exception as e: out({'mashup_error':str(e)})
Path('tmp/consolidado_inspection.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
for r in results:
    if 'mashup' in r:
        chunks=re.split(r'(?m)^shared ',r['source'])
        for chunk in chunks:
            if chunk.startswith('#"REM2026-BDD A"'): print(chunk)
        print('Queries:',[c.split(' =')[0] for c in chunks[1:]])
    if 'mashup_error' in r: print(r)
