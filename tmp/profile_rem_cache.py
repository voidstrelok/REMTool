from zipfile import ZipFile
from collections import Counter,defaultdict
import xml.etree.ElementTree as ET
import json

path='C:/Users/Usuario/Desktop/2025/Consolidados/Consolidado REM Serie A 2026.xlsx'
ns={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
with ZipFile(path) as z:
    d=ET.fromstring(z.read('xl/pivotCache/pivotCacheDefinition1.xml'))
    fields=list(d.find('s:cacheFields',ns))
    shared=[[x.get('v') for x in f.find('s:sharedItems',ns)] if f.find('s:sharedItems',ns) is not None else [] for f in fields]
    def value(c,i):
        v=c.get('v')
        return shared[i][int(v)] if c.tag.endswith('}x') else v
    series=Counter(); zeros=Counter(); codes=defaultdict(set); keys=set(); duplicates=0; types=Counter(); months=set()
    with z.open('xl/pivotCache/pivotCacheRecords1.xml') as f:
        it=ET.iterparse(f,events=('start','end'))
        _,root=next(it)
        for event,e in it:
            if event!='end' or not e.tag.endswith('}r'): continue
            cs=list(e)
            s=value(cs[53],53) or '(blank)'; series[s]+=1
            codes[s].add(value(cs[51],51)); months.add(value(cs[54],54))
            if all(c.get('v') in (None,'0','0.0') for c in cs[3:51]): zeros[s]+=1
            key=(value(cs[1],1),value(cs[2],2))
            if key in keys: duplicates+=1
            keys.add(key)
            for i in (48,49,50): types[(i-2,cs[i].tag.split('}')[-1])]+=1
            root.clear()
    print(json.dumps({'rows_by_series':dict(series),'all_zero_rows_by_series':dict(zeros),'codes_by_series':{s:len(c) for s,c in codes.items()},'duplicate_prestacion_reporte':duplicates,'months':sorted(months,key=lambda x:int(x or 0)),'last_value_column_types':{str(k):v for k,v in types.items()}},ensure_ascii=False))
    print('Slicers:',[(n,ET.fromstring(z.read(n)).attrib) for n in z.namelist() if n.startswith('xl/slicerCaches/') and n.endswith('.xml')])
