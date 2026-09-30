#!/usr/bin/env python3
"""Build a per-concept asset index from every SOURCE.json / sources.json `usedFor` field.

Outputs (under assets/_catalog/):
  concept_index.json   concept number -> [{path, category, license, files, bytes}]
  CONCEPT_PACKS.md     the same, as a readable list (generated; do not edit by hand)
"""
import collections
import json
from pathlib import Path

ASSETS = Path(__file__).resolve().parents[1]
CATALOG = ASSETS / '_catalog'
CATEGORIES = ['fonts', 'shaders', 'audio', 'images', 'models', 'icons']
PROVENANCE = {'SOURCE.json', 'provenance.json', 'sources.json'}


def concepts(values):
    out = set()
    for v in values or []:
        if isinstance(v, int) or (isinstance(v, str) and v.isdigit()):
            out.add(int(v))
        elif v == 'all':
            out.update(range(1, 21))
    return out


def stats(folder):
    files = [p for p in folder.rglob('*') if p.is_file() and p.name not in PROVENANCE]
    return len(files), sum(p.stat().st_size for p in files)


def main():
    entries = []
    for category in CATEGORIES:
        for record in sorted((ASSETS / category).rglob('*.json')):
            if record.name not in ('SOURCE.json', 'sources.json'):
                continue
            data = json.loads(record.read_text(encoding='utf-8'))
            if record.name == 'SOURCE.json':
                folder = record.parent
                n, size = stats(folder)
                entries.append(dict(path=folder.relative_to(ASSETS).as_posix(), category=category,
                                    license=str(data.get('license', '?')), files=n, bytes=size,
                                    concepts=sorted(concepts(data.get('usedFor')))))
            else:
                # Per-file photo collections: group by first sub-folder.
                groups = collections.defaultdict(lambda: dict(c=set(), lic=collections.Counter(), files=[]))
                for item in data.get('files', []):
                    sub = item['file'].split('/')[0] if '/' in item['file'] else '.'
                    g = groups[sub]
                    g['c'] |= concepts(item.get('usedFor'))
                    g['lic'][str(item.get('license', '?')).split(' (')[0]] += 1
                    g['files'].append(item['file'])
                for sub, g in sorted(groups.items()):
                    folder = record.parent / sub
                    size = sum((record.parent / f).stat().st_size for f in g['files'] if (record.parent / f).exists())
                    lic = ', '.join(f'{k} {v}' for k, v in g['lic'].most_common())
                    entries.append(dict(path=folder.relative_to(ASSETS).as_posix(), category=category,
                                        license=lic, files=len(g['files']), bytes=size, concepts=sorted(g['c'])))

    index = {n: [] for n in range(1, 21)}
    for e in entries:
        for n in e['concepts']:
            index[n].append({k: e[k] for k in ('path', 'category', 'license', 'files', 'bytes')})

    CATALOG.mkdir(exist_ok=True)
    (CATALOG / 'concept_index.json').write_text(json.dumps(index, ensure_ascii=False, indent=1), encoding='utf-8')

    lines = ['# 컨셉별 에셋 전체 목록 (자동 생성)', '',
             '`_tools/build_concept_index.py`가 각 `SOURCE.json`·`sources.json`의 `usedFor`에서 만든다. 손으로 고치지 말 것.',
             '큐레이션한 요약은 `../INDEX.md`.', '']
    for n in range(1, 21):
        items = index[n]
        lines.append(f'## {n}번 — 항목 {len(items)}개 · 파일 {sum(i["files"] for i in items):,}개 · '
                     f'{sum(i["bytes"] for i in items) / 1024**2:,.0f} MB')
        for category in CATEGORIES:
            rows = [i for i in items if i['category'] == category]
            if not rows:
                continue
            lines.append(f'\n**{category}** ({len(rows)})\n')
            for i in sorted(rows, key=lambda r: r['path']):
                lines.append(f'- `{i["path"]}` — {i["files"]}개, {i["license"]}')
        lines.append('')
    (CATALOG / 'CONCEPT_PACKS.md').write_text('\n'.join(lines), encoding='utf-8')
    unmapped = sum(1 for e in entries if not e['concepts'])
    print(f'{len(entries)} entries, {unmapped} without concept mapping')
    for n in range(1, 21):
        print(n, len(index[n]), sum(i['files'] for i in index[n]))


if __name__ == '__main__':
    main()
