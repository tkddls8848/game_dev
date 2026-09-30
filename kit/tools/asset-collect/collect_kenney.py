#!/usr/bin/env python3
"""Download Kenney packs (CC0) into a target directory with verified provenance.

Adapted from kit/tools/asset_library.py `kenney()`; kit/ is not modified.

Usage:
  python collect_kenney.py <target_dir> <slug>[=concepts] ...
  e.g. python collect_kenney.py ../models/kenney furniture-kit=3,17,19 train-kit=15

Per pack: <target>/<slug>/contents/ (extracted), SOURCE.json, preview image,
and the original zip only if < 20 MB.
"""
import collections, concurrent.futures, hashlib, html, json, re, sys, time
import urllib.parse, urllib.request, zipfile
from pathlib import Path, PurePosixPath

AGENT = 'LocalGameAssetCollector/1.0 (public asset downloads; 3 concurrent requests)'
KEEP_ZIP_BELOW = 20 * 1024**2


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda: f.read(1024 * 1024), b''): h.update(b)
    return h.hexdigest()


def get(url):
    req = urllib.request.Request(url, headers={'User-Agent': AGENT})
    with urllib.request.urlopen(req, timeout=60) as r: return r.read()


def download(url, path, max_bytes=1024**3):
    temp = path.with_name(path.name + '.part'); total = 0
    try:
        req = urllib.request.Request(url, headers={'User-Agent': AGENT})
        with urllib.request.urlopen(req, timeout=120) as r, temp.open('wb') as f:
            for block in iter(lambda: r.read(1024 * 1024), b''):
                total += len(block)
                if total > max_bytes: raise ValueError('File exceeds collection size limit')
                f.write(block)
        temp.replace(path)
    finally:
        if temp.exists(): temp.unlink()


def extract(archive, destination):
    base = destination.resolve()
    with zipfile.ZipFile(archive) as z:
        if sum(i.file_size for i in z.infolist()) > 8 * 1024**3: raise ValueError('Archive expands beyond 8 GiB')
        for i in z.infolist():
            name = PurePosixPath(i.filename.replace('\\', '/'))
            target = (base / str(name)).resolve()
            if name.is_absolute() or '..' in name.parts or not target.is_relative_to(base):
                raise ValueError('Unsafe archive path')
            if (i.external_attr >> 16) & 0o170000 == 0o120000: raise ValueError('Archive symlink')
        if z.testzip() is not None: raise ValueError('Archive CRC failed')
        z.extractall(destination)


def kenney(target, slug, used_for):
    url = 'https://kenney.nl/assets/' + slug
    folder = target / slug
    record = folder / 'SOURCE.json'
    try:
        if record.exists():
            return dict(slug=slug, status='already-present')
        source = get(url).decode('utf-8')
        if 'creativecommons.org/publicdomain/zero/' not in source:
            raise ValueError('Explicit CC0 license link not found on source page')
        urls = re.findall(r'href=["\x27]([^"\x27]+)', source)
        archives = [html.unescape(u) for u in urls if re.search(r'\.zip(?:\?|$)', u)]
        if not archives: raise ValueError('No public ZIP download link')
        archive_url = archives[0]
        if urllib.parse.urlparse(archive_url).hostname not in ('kenney.nl', 'www.kenney.nl'):
            raise ValueError('Download is not hosted by the creator')
        folder.mkdir(parents=True, exist_ok=True)
        archive_name = Path(urllib.parse.urlparse(archive_url).path).name
        archive = folder / archive_name
        if not archive.exists(): download(archive_url, archive)
        contents = folder / 'contents'
        extract(archive, contents)
        license_files = [p for p in contents.rglob('*') if p.is_file() and re.search(r'licen[sc]e|copying', p.name, re.I)]
        if not license_files: raise ValueError('No bundled license file; manual review required')
        if not any('CC0' in p.read_text(encoding='utf-8', errors='replace') or
                   'creativecommons.org/publicdomain/zero' in p.read_text(encoding='utf-8', errors='replace')
                   for p in license_files):
            raise ValueError('Bundled license does not confirm CC0')
        previews = [u for u in urls if re.search(r'/(?:preview|sample)\.(?:png|jpg)$', u)]
        preview_name = None
        if previews:
            preview_name = 'preview' + Path(previews[0]).suffix
            try: download(previews[0], folder / preview_name, 30 * 1024**2)
            except Exception: preview_name = None
        title = re.search(r'<h1[^>]*>(.*?)</h1>', source, re.S)
        files = [p for p in contents.rglob('*') if p.is_file()]
        model_exts = {'.glb', '.gltf', '.obj', '.fbx', '.blend', '.dae', '.stl'}
        digest, size = sha(archive), archive.stat().st_size
        kept = size < KEEP_ZIP_BELOW
        if not kept: archive.unlink()
        info = dict(name=html.unescape(re.sub('<[^>]+>', '', title[1])).strip() if title else slug,
                    provider='Kenney', author='Kenney (kenney.nl)', slug=slug,
                    sourceUrl=url, downloadUrl=archive_url, license='CC0-1.0',
                    licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/',
                    licenseFile=[p.relative_to(folder).as_posix() for p in license_files],
                    acquiredAt=time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
                    archive=archive_name, archiveKept=kept, sha256=digest, archiveBytes=size,
                    fileCount=len(files), modelFiles=sum(p.suffix.lower() in model_exts for p in files),
                    extensions=dict(collections.Counter(p.suffix.lower() for p in files)),
                    preview=preview_name, usedFor=used_for)
        record.write_text(json.dumps(info, ensure_ascii=False, indent=2), encoding='utf-8')
        return dict(slug=slug, status='downloaded', files=len(files), bytes=size)
    except Exception as e:
        return dict(slug=slug, status='failed', error=str(e)[:300], source=url)


def main():
    target = Path(sys.argv[1]).resolve(); target.mkdir(parents=True, exist_ok=True)
    jobs = []
    for arg in sys.argv[2:]:
        slug, _, concepts = arg.partition('=')
        jobs.append((slug, [int(c) if c.isdigit() else c for c in concepts.split(',') if c]))
    results = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        for r in pool.map(lambda j: kenney(target, *j), jobs):
            results.append(r); print(json.dumps(r), flush=True)
    log = target / '_collection-run.json'
    old = json.loads(log.read_text()) if log.exists() else []
    log.write_text(json.dumps(old + results, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
