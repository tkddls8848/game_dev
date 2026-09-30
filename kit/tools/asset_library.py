#!/usr/bin/env python3
"""Collect public CC0 packs and build a non-destructive, reproducible local asset catalog."""
import argparse
import collections
import concurrent.futures
import csv
import hashlib
import html
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import time
import urllib.parse
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'AssetDownloads'
CATALOG = ASSETS / '_catalog'
AGENT = 'LocalGameAssetCollector/1.0 (public asset downloads; 3 concurrent requests)'


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda: f.read(1024 * 1024), b''): h.update(b)
    return h.hexdigest()


def get(url):
    req = urllib.request.Request(url, headers={'User-Agent': AGENT})
    with urllib.request.urlopen(req, timeout=60) as r: return r.read()


def download(url, path, max_bytes=1024 * 1024 * 1024):
    temp = path.with_name(path.name + '.part')
    total = 0
    try:
        req = urllib.request.Request(url, headers={'User-Agent': AGENT})
        with urllib.request.urlopen(req, timeout=90) as r, temp.open('wb') as f:
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


def kenney(slug):
    url = 'https://kenney.nl/assets/' + slug
    folder = ASSETS / 'library/kenney' / slug
    record = folder / 'provenance.json'
    try:
        if record.exists():
            old = json.loads(record.read_text(encoding='utf-8'))
            if sha(folder / old['archive']) == old['sha256']:
                return dict(slug=slug, status='already-present')
            raise ValueError('Existing archive hash mismatch; refusing overwrite')
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
        (folder / 'source.html').write_text(source, encoding='utf-8')
        archive_name = Path(urllib.parse.urlparse(archive_url).path).name
        archive = folder / archive_name
        if not archive.exists(): download(archive_url, archive)
        contents = folder / 'contents'
        extract(archive, contents)
        license_files = [p for p in contents.rglob('*') if p.is_file() and re.search(r'licen[sc]e|copying', p.name, re.I)]
        if not license_files: raise ValueError('No bundled license file; manual review required')
        if not any('CC0' in p.read_text(encoding='utf-8', errors='replace') or 'creativecommons.org/publicdomain/zero' in p.read_text(encoding='utf-8', errors='replace') for p in license_files):
            raise ValueError('Bundled license does not confirm CC0')
        previews = [u for u in urls if re.search(r'/(?:preview|sample)\.(?:png|jpg)$', u)]
        preview_name = None
        if previews:
            preview_name = 'preview' + Path(previews[0]).suffix
            download(previews[0], folder / preview_name, 30 * 1024**2)
        title = re.search(r'<h1[^>]*>(.*?)</h1>', source, re.S)
        files = [p for p in contents.rglob('*') if p.is_file()]
        model_exts = {'.glb','.gltf','.obj','.fbx','.blend','.dae','.stl'}
        info = dict(provider='Kenney', slug=slug, title=html.unescape(re.sub('<[^>]+>', '', title[1])).strip() if title else slug,
                    source=url, download=archive_url, license='CC0-1.0',
                    licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/',
                    licenseFiles=[p.relative_to(folder).as_posix() for p in license_files],
                    acquiredAt=time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
                    archive=archive_name, sha256=sha(archive), archiveBytes=archive.stat().st_size,
                    fileCount=len(files), modelFiles=sum(p.suffix.lower() in model_exts for p in files),
                    extensions=dict(collections.Counter(p.suffix.lower() for p in files)),
                    preview=preview_name, previewIsDocumentation=True,
                    category='effects' if slug in ('particle-pack','smoke-particles','light-masks','skyboxes') else '3d')
        record.write_text(json.dumps(info, ensure_ascii=False, indent=2), encoding='utf-8')
        return dict(slug=slug, status='downloaded', files=len(files), bytes=info['archiveBytes'])
    except Exception as e:
        return dict(slug=slug, status='failed', error=str(e)[:300], source=url)


def collect():
    slugs = set()
    for page in range(1, 5):
        url = 'https://kenney.nl/assets/category:3D' + (f'/page:{page}' if page > 1 else '')
        body = get(url).decode('utf-8')
        slugs.update(re.findall(r'https://kenney\.nl/assets/([a-z0-9]+(?:-[a-z0-9]+)*)["\x27]', body))
        time.sleep(.35)
    slugs.update(['particle-pack','smoke-particles','light-masks','skyboxes'])
    print(f'Discovered {len(slugs)} public packs', flush=True)
    results = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        for result in pool.map(kenney, sorted(slugs)):
            results.append(result)
            print(json.dumps(result, ensure_ascii=True), flush=True)
    CATALOG.mkdir(parents=True, exist_ok=True)
    (CATALOG / 'collection-run.json').write_text(json.dumps(results, indent=2), encoding='utf-8')


GROUPS = {
    '3D 모델': {'.glb','.gltf','.obj','.fbx','.blend','.dae','.stl','.vox'},
    '텍스처·이미지': {'.png','.jpg','.jpeg','.webp','.tga','.bmp','.exr','.hdr','.dds'},
    '벡터': {'.svg','.eps'}, '음성·효과음': {'.wav','.mp3','.ogg','.flac'},
    '폰트': {'.ttf','.otf','.woff','.woff2'}, '압축 원본': {'.zip','.7z','.rar'},
    '자료·라이선스': {'.md','.txt','.html','.pdf','.pb','.tsv'},
}


def kind(path):
    return next((k for k,v in GROUPS.items() if path.suffix.lower() in v), '재질·설정·기타')


def index():
    CATALOG.mkdir(parents=True, exist_ok=True)
    packs=[]
    for p in sorted((ASSETS/'library').rglob('provenance.json')) if (ASSETS/'library').exists() else []:
        info=json.loads(p.read_text(encoding='utf-8'));info['folder']=p.parent.relative_to(ASSETS).as_posix();packs.append(info)
    evidence=[p for p in ASSETS.rglob('*') if p.is_file() and '_catalog' not in p.relative_to(ASSETS).parts and re.search(r'licen[sc]e|ofl|copying',p.name,re.I)]
    source_map={}
    for log in ASSETS.rglob('_download_log.tsv'):
        with log.open(encoding='utf-8-sig') as stream:
            for row in csv.DictReader(stream,delimiter='\t'):
                if row.get('dest') and row.get('url'):
                    local=(log.parent/row['dest']).resolve()
                    if local.is_relative_to(ASSETS.resolve()):source_map[local.relative_to(ASSETS.resolve()).as_posix()]=row['url']
    evidence_by_parent=collections.defaultdict(list)
    for item in evidence:evidence_by_parent[item.parent].append(item.relative_to(ASSETS).as_posix())
    records=[];hashes=collections.defaultdict(list)
    for p in sorted(ASSETS.rglob('*')):
        if not p.is_file() or '_catalog' in p.relative_to(ASSETS).parts or p.name in ('README.md','index.html') and p.parent==ASSETS:continue
        rel=p.relative_to(ASSETS).as_posix()
        if p.suffix=='.part':continue
        pack=next((a for a in packs if rel.startswith(a['folder']+'/')),None)
        licenses=[]
        parent=p.parent
        while parent!=ASSETS.parent:
            licenses=evidence_by_parent.get(parent,[])
            if licenses or parent==ASSETS:break
            parent=parent.parent
        status='CC0-1.0 / source + archive verified' if pack else ('Local license evidence; review before reuse' if licenses else 'No file-level license mapping yet')
        if pack and not rel.startswith(pack['folder']+'/contents/') and p.name!=pack['archive']:
            status='Catalog/source documentation; not a licensed game asset'
        if pack:licenses=[pack['folder']+'/'+name for name in pack['licenseFiles']]
        if rel.startswith('audition/'):status='Generated voice audition; provider terms not audited'
        h=sha(p)
        row=dict(path=rel,collection=pack['title'] if pack else rel.split('/')[0],category=kind(p),extension=p.suffix.lower(),bytes=p.stat().st_size,sha256=h,
                 source=pack['source'] if pack else source_map.get(rel,''),licenseStatus=status,licenseEvidence=licenses,packRoot=pack['folder'] if pack else '')
        records.append(row);hashes[h].append(row)
    duplicates=[dict(sha256=h,bytes=rows[0]['bytes'],copies=len(rows),redundantBytes=rows[0]['bytes']*(len(rows)-1),paths=[r['path'] for r in rows]) for h,rows in hashes.items() if len(rows)>1]
    duplicates.sort(key=lambda r:r['redundantBytes'],reverse=True)
    summary=dict(files=len(records),bytes=sum(r['bytes'] for r in records),verifiedPacks=len(packs),
                 categories=dict(collections.Counter(r['category'] for r in records)),
                 duplicateGroups=len(duplicates),redundantBytes=sum(r['redundantBytes'] for r in duplicates),
                 missingLicenseMapping=sum(r['licenseStatus'].startswith('No file') for r in records))
    issues=dict(emptyFiles=[r['path'] for r in records if r['bytes']==0],
                missingLicenseMapping=[r['path'] for r in records if r['licenseStatus'].startswith('No file')],
                note='Review list only. Files were not removed or relicensed.')
    for name,value in [('inventory.json',records),('packs.json',packs),('duplicates.json',duplicates),('summary.json',summary),('review-needed.json',issues)]:
        (CATALOG/name).write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
    with (CATALOG/'inventory.csv').open('w',encoding='utf-8-sig',newline='') as f:
        writer=csv.DictWriter(f,fieldnames=list(records[0]) if records else ['path']);writer.writeheader()
        writer.writerows({**r,'licenseEvidence':' | '.join(r['licenseEvidence'])} for r in records)
    # Compact records for an offline catalog; no remote scripts, no fetch/file:// restrictions.
    data=json.dumps(dict(summary=summary,packs=packs,files=[{k:r[k] for k in ('path','category','collection','bytes','licenseStatus')} for r in records]),ensure_ascii=False).replace('<','\\u003c')
    template=(ROOT/'kit/templates/asset-library.html').read_text(encoding='utf-8')
    (ASSETS/'index.html').write_text(template.replace('__ASSET_DATA__',data),encoding='utf-8')
    md=['# AssetDownloads — 에셋 라이브러리','', '[검색·미리보기 열기](index.html)','',
        '기존 게임·도구·청취실의 참조를 유지하기 위해 기존 폴더와 파일명은 보존했다. 새로 수집한 팩은 제작자/팩별 표준 구조로 보관한다. 중복은 내용의 SHA-256으로 기록하며 자동 삭제하지 않는다.','',
        '## 폴더','', '| 폴더 | 용도 |','|---|---|',
        '| `library/kenney/<팩>/` | 신규 3D·장면 연출 팩: 원본 ZIP, contents, source.html, 라이선스, provenance.json, preview |',
        '| `audition/` | 기존 음성 청취본. 유료/무료·서비스 약관 판단은 별도 |',
        '| `concepts/<게임>/` | 원안 10개용 폰트·라이선스 |',
        '| `deckbuilder/`, `farming/`, `hybrid/`, `tactics/` | 기존 장르별 에셋과 출처 기록 |',
        '| `Kenney/` | 기존에 받은 공용 2D/음향 ZIP 원본 |',
        '| `_catalog/` | 전체 CSV/JSON, 팩 목록, SHA-256 중복 목록, 수집 결과 |','',
        f'총 파일 **{summary["files"]:,}개**, **{summary["bytes"]/1024**3:.2f} GiB**. 출처와 압축본 라이선스를 대조한 신규 팩 **{len(packs)}개**.',
        f'동일 내용 중복 그룹 **{len(duplicates):,}개**, 중복 사본 용량 **{summary["redundantBytes"]/1024**2:.1f} MiB**. 포맷 변형·압축 내부 파일은 서로 다른 리소스로 셀 수 있으므로 고유 모델 수가 아니다.','',
        '라이선스가 발견됐다는 것과 해당 파일의 사용권이 확인됐다는 것은 구분한다. 기존 파일의 출처를 모르면 추정으로 CC0를 붙이지 않았다. 페이지 미리보기와 source.html은 출처 확인용 자료이며 게임에 가져갈 대상은 contents 안의 에셋이다.','',
        '갱신: `python kit/tools/asset_library.py index`','수집/재개: `python kit/tools/asset_library.py collect-kenney`','',
        '이 디렉터리는 gitignore 대상이다. 다른 컴퓨터에서 쓰려면 원본을 별도로 보관·복사한다. 수집 도구와 카탈로그 템플릿은 저장소의 kit에 있다.']
    (ASSETS/'README.md').write_text('\n'.join(md)+'\n',encoding='utf-8')
    print(json.dumps(summary,ensure_ascii=True),flush=True)


if __name__=='__main__':
    import sys
    sys.stdout.reconfigure(encoding='utf-8')
    ap=argparse.ArgumentParser();ap.add_argument('action',choices=['collect-kenney','index']);args=ap.parse_args()
    if args.action=='collect-kenney':collect()
    else:index()
