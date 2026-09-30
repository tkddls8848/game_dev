#!/usr/bin/env python3
"""Download Quaternius packs (CC0) from the public Google Drive folders linked on quaternius.com.

Requires `gdown` (run with a venv python). No account or API key; public share links only.
Usage: python collect_quaternius.py <target_dir> <pack>[=concepts] ...
Keeps one engine format per pack (glTF > FBX > OBJ) + textures; .blend and duplicate formats are skipped to save space/time.
"""
import collections, hashlib, json, re, sys, time, urllib.request
from pathlib import Path
import gdown, concurrent.futures

FMT_TOKENS = {'blend': 'blend', 'blends': 'blend', 'fbx': 'fbx', 'obj': 'obj', 'gltf': 'gltf', 'glb': 'gltf', 'dae': 'dae', 'stl': 'stl'}
PREFER = ['gltf', 'fbx', 'obj', 'dae']


def fmt_of(path):
    for part in Path(path).parts[:-1]:
        k = re.sub(r'[^a-z]', '', part.lower())
        for tok, fmt in FMT_TOKENS.items():
            if k == tok or k.startswith(tok) or k.endswith(tok): return fmt
    return None

AGENT = 'Mozilla/5.0 (LocalGameAssetCollector/1.0)'
SKIP = {'.blend', '.blend1'}


def page(slug):
    req = urllib.request.Request(f'https://quaternius.com/packs/{slug}.html', headers={'User-Agent': AGENT})
    return urllib.request.urlopen(req, timeout=60).read().decode('utf-8', 'replace')


def one(target, slug, used):
    folder = target / slug
    if (folder / 'SOURCE.json').exists(): return dict(slug=slug, status='already-present')
    try:
        src = page(slug)
        if 'creativecommons.org/publicdomain/zero/' not in src: raise ValueError('CC0 link not on page')
        m = re.search(r'https://drive\.google\.com/drive/folders/([A-Za-z0-9_-]+)', src)
        if not m: raise ValueError('No public Google Drive folder (itch.io only?)')
        title = re.search(r'<title>(.*?)</title>', src, re.S)
        fid = m[1]
        listing = gdown.download_folder(id=fid, skip_download=True, quiet=True)
        folder.mkdir(parents=True, exist_ok=True)
        h = hashlib.sha256(); n = 0; skipped = 0; total = 0; fails = []
        present = {fmt_of(f.path) for f in listing} - {None, 'blend'}
        chosen = next((x for x in PREFER if x in present), None)
        todo = []
        for f in sorted(listing, key=lambda f: f.path):
            fm = fmt_of(f.path)
            if Path(f.path).suffix.lower() in SKIP or (fm is not None and fm != chosen): skipped += 1; continue
            todo.append(f)

        def fetch(f):
            dst = folder / 'contents' / Path(f.path)
            dst.parent.mkdir(parents=True, exist_ok=True)
            for attempt in range(3):
                if dst.exists(): return True
                try:
                    r = urllib.request.Request(f'https://drive.usercontent.google.com/download?id={f.id}&export=download&confirm=t',
                                               headers={'User-Agent': AGENT})
                    with urllib.request.urlopen(r, timeout=300) as resp:
                        if 'text/html' in resp.headers.get('Content-Type', ''): raise ValueError('drive returned html (quota?)')
                        data = resp.read()
                    tmp = dst.with_name(dst.name + '.part'); tmp.write_bytes(data); tmp.replace(dst)
                except Exception: time.sleep(3 + attempt * 5)
            return dst.exists()
        with concurrent.futures.ThreadPoolExecutor(6) as pool:
            oks = list(pool.map(fetch, todo))
        for f, ok in zip(todo, oks):
            if not ok: fails.append(f.path); continue
            data = (folder / 'contents' / Path(f.path)).read_bytes(); h.update(data); total += len(data); n += 1
        files = [p for p in (folder / 'contents').rglob('*') if p.is_file()]
        lic = [p.relative_to(folder).as_posix() for p in files if re.search('licen', p.name, re.I)]
        if not lic:
            (folder / 'LICENSE.txt').write_text(
                f'{title[1].strip() if title else slug} by Quaternius. License: CC0 1.0 Universal (public domain).\n'
                f'Stated on https://quaternius.com/packs/{slug}.html (links https://creativecommons.org/publicdomain/zero/1.0/).\n')
            lic = ['LICENSE.txt (written from source page)']
        rec = dict(name=(title[1].strip() if title else slug), provider='Quaternius', author='Quaternius (quaternius.com)',
                   slug=slug, sourceUrl=f'https://quaternius.com/packs/{slug}.html',
                   downloadUrl=f'https://drive.google.com/drive/folders/{fid}', license='CC0-1.0',
                   licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/', licenseFile=lic,
                   acquiredAt=time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()), sha256=h.hexdigest(),
                   sha256Note='over downloaded files in sorted Drive path order', bytes=total,
                   fileCount=len(files), keptFormat=chosen, skippedOtherFormatFiles=skipped, failedFiles=fails,
                   extensions=dict(collections.Counter(p.suffix.lower() for p in files)), usedFor=used)
        (folder / 'SOURCE.json').write_text(json.dumps(rec, indent=2, ensure_ascii=False), encoding='utf-8')
        return dict(slug=slug, status='downloaded' if not fails else 'partial', files=len(files), bytes=total, failed=len(fails))
    except Exception as e:
        return dict(slug=slug, status='failed', error=str(e)[:300])


def main():
    target = Path(sys.argv[1]).resolve(); target.mkdir(parents=True, exist_ok=True)
    results = []
    for arg in sys.argv[2:]:
        slug, _, c = arg.partition('=')
        r = one(target, slug, [int(x) if x.isdigit() else x for x in c.split(',') if x])
        results.append(r); print(json.dumps(r), flush=True)
    log = target / '_collection-run.json'
    old = json.loads(log.read_text()) if log.exists() else []
    log.write_text(json.dumps(old + results, indent=2))


if __name__ == '__main__':
    main()
