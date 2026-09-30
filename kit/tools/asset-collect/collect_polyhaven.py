#!/usr/bin/env python3
"""Download Poly Haven models (CC0) as glTF with 1k textures.

Usage: python collect_polyhaven.py <target_dir>
Selects models whose categories overlap WANT (props/furniture/electronics...).
Per model: <target>/<id>/<id>_1k.gltf + .bin + textures/, SOURCE.json.
"""
import concurrent.futures, hashlib, json, sys, time, urllib.request
from pathlib import Path

AGENT = 'LocalGameAssetCollector/1.0 (public asset downloads)'
WANT = {'props', 'furniture', 'decorative', 'tools', 'containers', 'seating', 'electronics', 'lighting',
        'table', 'appliances', 'food', 'shelves', 'dishes', 'office', 'vases', 'wall decoration',
        'instrument', 'bed', 'books', 'rigged', 'creature'}

# concept mapping by keyword in id/tags
MAP = [(('radio', 'transceiver', 'microphone', 'speaker', 'headphone'), [2, 12, 18]),
       (('telephone', 'phone', 'switch'), [10]),
       (('camera', 'tripod', 'frame', 'photo', 'lens'), [4]),
       (('lamp', 'lantern', 'light', 'candle', 'flashlight', 'torch'), [3, 11, 19]),
       (('box', 'crate', 'cardboard', 'suitcase', 'bag', 'case', 'container', 'barrel', 'basket'), [13]),
       (('book', 'typewriter', 'letter', 'paper', 'desk', 'pen', 'ink'), [9, 7]),
       (('bed', 'chair', 'sofa', 'couch', 'table', 'cabinet', 'shelf', 'drawer', 'wardrobe', 'dresser', 'nightstand'), [3, 11, 17, 19]),
       (('statue', 'bust', 'vase', 'sculpture', 'clock', 'chess', 'ornament', 'decorative', 'painting'), [17, 13]),
       (('tape', 'recorder', 'cassette', 'tv', 'television', 'computer', 'monitor'), [18, 1, 12]),
       (('bench', 'seating', 'street', 'bin'), [20]),
       (('puppet', 'doll', 'toy', 'mannequin'), [5])]


def get_json(url):
    req = urllib.request.Request(url, headers={'User-Agent': AGENT})
    with urllib.request.urlopen(req, timeout=60) as r: return json.load(r)


def fetch(url, path, md5):
    req = urllib.request.Request(url, headers={'User-Agent': AGENT})
    with urllib.request.urlopen(req, timeout=180) as r: data = r.read()
    if md5 and hashlib.md5(data).hexdigest() != md5: raise ValueError('md5 mismatch ' + url)
    path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(data)
    return data


def used_for(k, v):
    text = ' '.join([k.lower()] + v.get('tags', []) + v.get('categories', []))
    out = set()
    for keys, concepts in MAP:
        if any(w in text for w in keys): out.update(concepts)
    return sorted(out) or ['general-props']


def one(target, k, v):
    folder = target / k
    if (folder / 'SOURCE.json').exists(): return dict(id=k, status='already-present')
    try:
        files = get_json('https://api.polyhaven.com/files/' + k)
        g = files['gltf']['1k']['gltf']
        h = hashlib.sha256()
        data = fetch(g['url'], folder / Path(g['url']).name, g.get('md5')); h.update(data)
        total = len(data)
        for rel, info in sorted(g.get('include', {}).items()):
            data = fetch(info['url'], folder / rel, info.get('md5')); h.update(data); total += len(data)
        rec = dict(name=v['name'], id=k, sourceUrl='https://polyhaven.com/a/' + k, downloadUrl=g['url'],
                   license='CC0-1.0', licenseUrl='https://polyhaven.com/license', licenseFile='../LICENSE.txt',
                   author=', '.join(v.get('authors', {})), acquiredAt=time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
                   sha256=h.hexdigest(), sha256Note='over gltf + included files in sorted order', bytes=total,
                   categories=v['categories'], tags=v.get('tags', []), polycount=v.get('polycount'),
                   dimensionsMm=v.get('dimensions'), format='glTF 2.0 (.gltf + .bin + 1k jpg/png textures)',
                   usedFor=used_for(k, v))
        (folder / 'SOURCE.json').write_text(json.dumps(rec, indent=2, ensure_ascii=False), encoding='utf-8')
        return dict(id=k, status='downloaded', bytes=total)
    except Exception as e:
        return dict(id=k, status='failed', error=str(e)[:200])


def main():
    target = Path(sys.argv[1]).resolve(); target.mkdir(parents=True, exist_ok=True)
    (target / 'LICENSE.txt').write_text(
        'All Poly Haven assets are CC0 1.0 Universal (public domain). https://polyhaven.com/license\n'
        'https://creativecommons.org/publicdomain/zero/1.0/\nAttribution not required; credit "Poly Haven" appreciated.\n')
    assets = get_json('https://api.polyhaven.com/assets?t=models')
    sel = {k: v for k, v in assets.items() if WANT & set(v['categories'])}
    with concurrent.futures.ThreadPoolExecutor(6) as pool:
        results = list(pool.map(lambda kv: one(target, *kv), sel.items()))
    for r in results:
        if r['status'] == 'failed': print(json.dumps(r))
    print('ok', sum(r['status'] != 'failed' for r in results), 'failed', sum(r['status'] == 'failed' for r in results))
    (target / '_collection-run.json').write_text(json.dumps(results, indent=1))


if __name__ == '__main__':
    main()
