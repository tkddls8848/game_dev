#!/usr/bin/env python3
"""Offline collection audit: manifest paths, archive signatures, glTF dependencies and animations."""
import csv
import hashlib
import json
from pathlib import Path
import struct
import sys
import urllib.parse

ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/'AssetDownloads'
CAT=ASSETS/'_catalog'


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    packs=[]
    for p in sorted((ASSETS/'library').rglob('provenance.json')):
        pack=json.loads(p.read_text(encoding='utf-8'));pack['folder']=p.parent.relative_to(ASSETS).as_posix();packs.append(pack)
    failures=[];models=[];clips=[]
    for pack in packs:
        folder=ASSETS/pack['folder'];archive=folder/pack['archive']
        with archive.open('rb') as stream:actual_hash=hashlib.file_digest(stream,'sha256').hexdigest()
        if actual_hash!=pack['sha256']:
            failures.append(str(archive)+' hash mismatch')
        for relative in pack['licenseFiles']:
            if not (folder/relative).is_file():failures.append(pack['slug']+' missing license '+relative)
        if pack['preview'] and not (folder/pack['preview']).is_file():failures.append(pack['slug']+' missing preview')
        for p in (folder/'contents').rglob('*'):
            if p.suffix.lower() not in ('.gltf','.glb'):continue
            try:
                if p.suffix.lower()=='.gltf':doc=json.loads(p.read_text(encoding='utf-8-sig'))
                else:
                    with p.open('rb') as f:
                        magic,version,length=struct.unpack('<4sII',f.read(12))
                        if magic!=b'glTF' or version!=2 or length!=p.stat().st_size:raise ValueError('Invalid GLB header')
                        chunk_size,chunk_type=struct.unpack('<II',f.read(8))
                        if chunk_type!=0x4e4f534a:raise ValueError('GLB JSON chunk missing')
                        doc=json.loads(f.read(chunk_size))
                for item in doc.get('buffers',[])+doc.get('images',[]):
                    uri=item.get('uri','')
                    if not uri or uri.startswith('data:'):continue
                    if urllib.parse.urlparse(uri).scheme:raise ValueError('External dependency: '+uri)
                    dep=(p.parent/urllib.parse.unquote(uri)).resolve()
                    if not dep.is_relative_to(folder.resolve()) or not dep.is_file():raise ValueError('Missing/local-outside dependency: '+uri)
                names=[a.get('name',f'animation_{i}') for i,a in enumerate(doc.get('animations',[]))]
                rel=p.relative_to(ASSETS).as_posix()
                models.append(dict(path=rel,pack=pack['slug'],meshes=len(doc.get('meshes',[])),animations=names))
                for name in names:clips.append(dict(pack=pack['slug'],path=rel,animation=name))
            except Exception as e:failures.append(p.relative_to(ASSETS).as_posix()+': '+str(e))
    (CAT/'model-index.json').write_text(json.dumps(models,ensure_ascii=False,indent=2),encoding='utf-8')
    with (CAT/'animations.csv').open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=['pack','path','animation']);w.writeheader();w.writerows(clips)
    report=dict(packs=len(packs),gltfGlbFiles=len(models),animatedFiles=sum(bool(m['animations']) for m in models),
                animationEntries=len(clips),note='Animation entries include duplicates across models/formats; not unique animation count.',failures=failures)
    (CAT/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(report,ensure_ascii=True))
    return 1 if failures else 0


if __name__=='__main__':raise SystemExit(main())
