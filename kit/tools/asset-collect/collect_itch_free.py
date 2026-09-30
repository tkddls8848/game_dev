#!/usr/bin/env python3
"""Download the FREE ($0) uploads of a public itch.io page (no account) — used for Quaternius itch-only packs.

Usage: python collect_itch_free.py <target_dir> <itch_url>=<slug>=<concepts> ...
Only uploads offered without payment on the public download page are fetched; paid tiers are never touched.
The page must state CC0. Zip kept only if < 20 MB; always extracted to contents/.
"""
import collections, hashlib, http.cookiejar, json, re, sys, time, urllib.parse, urllib.request, zipfile
from pathlib import Path, PurePosixPath

AGENT = 'Mozilla/5.0 (LocalGameAssetCollector/1.0)'


def opener():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


def req(op, url, data=None):
    r = urllib.request.Request(url, data=urllib.parse.urlencode(data).encode() if data else None,
                               headers={'User-Agent': AGENT, 'X-Requested-With': 'XMLHttpRequest'} if data else {'User-Agent': AGENT})
    return op.open(r, timeout=300).read()


def safe_extract(archive, dest):
    base = dest.resolve()
    with zipfile.ZipFile(archive) as z:
        for i in z.infolist():
            n = PurePosixPath(i.filename.replace('\\', '/'))
            if n.is_absolute() or '..' in n.parts or not (base / str(n)).resolve().is_relative_to(base): raise ValueError('unsafe path')
        z.extractall(dest)


def one(target, url, slug, used):
    folder = target / slug
    if (folder / 'SOURCE.json').exists(): return dict(slug=slug, status='already-present')
    try:
        op = opener()
        page = req(op, url).decode('utf-8', 'replace')
        if 'Creative Commons Zero' not in page and 'CC0' not in page: raise ValueError('CC0 not stated on page')
        title = re.search(r'<title>(.*?)</title>', page, re.S)
        csrf0 = re.search(r'name="csrf_token" value="([^"]+)"', page)[1]
        dl = json.loads(req(op, url + '/download_url', {'csrf_token': csrf0}))['url']
        key = dl.rstrip('/').split('/download/')[1]
        dpage = req(op, dl).decode('utf-8', 'replace')
        csrf = re.search(r'name="csrf_token" value="([^"]+)"', dpage)[1]
        uploads = re.findall(r'data-upload_id="(\d+)".*?title="([^"]+)"', dpage, re.S)
        if not uploads: raise ValueError('no free uploads')
        folder.mkdir(parents=True, exist_ok=True)
        recs = []
        for uid, name in uploads:
            j = json.loads(req(op, f'{url}/file/{uid}?source=view_game&as_props=1', {'csrf_token': csrf0}))
            data = req(op, j['url'])
            p = folder / name; p.write_bytes(data)
            digest = hashlib.sha256(data).hexdigest()
            if name.lower().endswith('.zip'):
                safe_extract(p, folder / 'contents')
                kept = len(data) < 20 * 1024**2
                if not kept: p.unlink()
            else:
                kept = True
            recs.append(dict(file=name, uploadId=uid, sha256=digest, bytes=len(data), archiveKept=kept))
        files = [p for p in (folder / 'contents').rglob('*') if p.is_file()] if (folder / 'contents').exists() else []
        lic = [p.relative_to(folder).as_posix() for p in files if re.search('licen', p.name, re.I)]
        if not lic:
            (folder / 'LICENSE.txt').write_text(f'{slug} by Quaternius — CC0 1.0 Universal, as stated on {url}\n')
            lic = ['LICENSE.txt (written from source page)']
        rec = dict(name=re.sub(r'\s+', ' ', title[1]).strip() if title else slug, provider='Quaternius (itch.io free tier)',
                   author='Quaternius', sourceUrl=url, downloadUrl=url + '/download_url (free upload, no account)',
                   license='CC0-1.0', licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/', licenseFile=lic,
                   acquiredAt=time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
                   sha256=recs[0]['sha256'], uploads=recs, fileCount=len(files),
                   extensions=dict(collections.Counter(p.suffix.lower() for p in files)), usedFor=used,
                   note='Only the free tier was downloaded; paid tiers (Pro/Source) not included.')
        (folder / 'SOURCE.json').write_text(json.dumps(rec, indent=2, ensure_ascii=False), encoding='utf-8')
        return dict(slug=slug, status='downloaded', files=len(files), uploads=[r['file'] for r in recs])
    except Exception as e:
        return dict(slug=slug, status='failed', error=str(e)[:300])


def main():
    target = Path(sys.argv[1]).resolve(); target.mkdir(parents=True, exist_ok=True)
    for arg in sys.argv[2:]:
        url, slug, c = arg.rsplit('=', 2)
        print(json.dumps(one(target, url, slug, [int(x) if x.isdigit() else x for x in c.split(',') if x])), flush=True)


if __name__ == '__main__':
    main()
