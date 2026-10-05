#!/usr/bin/env python3
"""컨셉 시안 폴더마다 prompts.json 의 두 프롬프트로 목업 이미지를 만든다.

    python docs/poc-gallery/concepts/_tools/gen_images.py            # 없는 것만
    python docs/poc-gallery/concepts/_tools/gen_images.py 03 17      # 번호 지정
    python docs/poc-gallery/concepts/_tools/gen_images.py --force 03 # 다시 생성
    python docs/poc-gallery/concepts/_tools/gen_images.py --force 60b 71b  # 이미지 하나만 다시

생성기는 Codex CLI(`codex exec`)의 내장 image_gen 이다 (ChatGPT 로그인 필요).
Codex 샌드박스는 파일 복사를 막으므로 생성 경로만 받아 이 스크립트가 옮긴다.
결과: <NN>-<slug>/mock-a.jpg, mock-b.jpg (1600px 폭 JPEG). AI 생성 시안이지
실행 빌드 화면이 아니다.
"""
import json
import os
import re
import subprocess
import sys
import tempfile
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

from PIL import Image

# 윈도우 콘솔은 기본이 cp949 라 한글 출력이 터진다
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

# CONCEPTS_ROOT 로 다른 폴더(예: _critique/<NN>-<slug>/ 아래 v2)를 대상으로 돌릴 수 있다
ROOT = Path(os.environ.get("CONCEPTS_ROOT") or Path(__file__).resolve().parent.parent).resolve()
LOG = Path(__file__).resolve().parent / "gen-log.json"
WORKERS = 4
# codex 가 내놓는 경로는 설치마다 다르다. POSIX 기본은 ~/.codex/generated_images/ 지만,
# CODEX_HOME 을 옮긴 설치는 .../orca/codex-accounts/<uuid>/home/generated_images/ 에 쓴다.
# `.codex` 를 요구하지 않고 generated_images 폴더만 기준으로 잡는다.
PATH_RE = re.compile(
    r"((?:[A-Za-z]:[\\/]|/)[^\s`'\"]*?[\\/]generated_images[\\/][^\s`'\"]+?\.png)"
)
# codex 는 저장소 밖에서 돌려야 한다(샌드박스가 파일 쓰기를 막는다). /tmp 는 윈도우에 없다.
WORKDIR = tempfile.gettempdir()

INSTRUCTION = (
    "Use your built-in image generation tool exactly once, {aspect}, to create this image. "
    "Do not write files or run commands. After generating, reply with only the absolute path "
    "of the generated PNG file.\n\nIMAGE PROMPT:\n"
)


def jobs(selected, force):
    for d in sorted(p for p in ROOT.iterdir() if p.is_dir() and (re.match(r"\d{2,3}-", p.name) or p.name == "v2")):
        num = d.name.split("-")[0]
        if selected and num not in selected and not any(x.startswith(num) and x[len(num):] in "ab" and len(x) > len(num) for x in selected):
            continue
        pj = d / "prompts.json"
        if not pj.exists():
            continue
        spec = json.loads(pj.read_text(encoding="utf-8"))
        for img in spec["images"]:
            out = d / f"mock-{img['id']}.jpg"
            if selected and num not in selected and f"{num}{img['id']}" not in selected:
                continue
            if force or not out.exists():
                # 화면비는 기본 16:9. 원작 화면비를 따라야 하는 시안은 prompts.json 의 "aspect" 로 바꾼다
                yield d, dict(img, aspect=spec.get("aspect", "landscape 16:9")), out


def run(job):
    d, img, out = job
    for attempt in range(2):
        t0 = time.time()
        try:
            r = subprocess.run(
                ["codex", "exec", "--skip-git-repo-check", "--sandbox", "read-only",
                 INSTRUCTION.format(aspect=img["aspect"]) + img["prompt"]],
                capture_output=True, text=True, timeout=900, cwd=WORKDIR,
                encoding="utf-8", errors="replace",
            )
        except subprocess.TimeoutExpired:
            # 한 장이 멈춰도 배치 전체가 죽지 않게 한다
            print(f"timeout {d.name}/{out.name} (attempt {attempt+1})", flush=True)
            r = subprocess.CompletedProcess([], 1, "", "timeout")
            continue
        found = PATH_RE.findall(r.stdout + r.stderr)
        src = next((Path(p) for p in reversed(found) if Path(p).exists()), None)
        if src:
            im = Image.open(src).convert("RGB")
            if im.width > 1600:
                im = im.resize((1600, round(im.height * 1600 / im.width)), Image.LANCZOS)
            im.save(out, "JPEG", quality=90, optimize=True)
            print(f"ok   {d.name}/{out.name}  {time.time()-t0:.0f}s", flush=True)
            return {"file": str(out.relative_to(Path(__file__).resolve().parent.parent)), "source": str(src), "ok": True}
        print(f"retry {d.name}/{out.name} (attempt {attempt+1})", flush=True)
    print(f"FAIL {d.name}/{out.name}", flush=True)
    return {"file": str(out.relative_to(Path(__file__).resolve().parent.parent)), "ok": False, "tail": (r.stdout + r.stderr)[-400:]}


def main():
    args = sys.argv[1:]
    force = "--force" in args
    selected = {(a.zfill(2) if a.isdigit() else (a.zfill(3) if len(a) == 2 else a)) for a in args if a != "--force"}
    todo = list(jobs(selected, force))
    print(f"{len(todo)} images to generate", flush=True)
    with ThreadPoolExecutor(WORKERS) as ex:
        results = list(ex.map(run, todo))
    log = json.loads(LOG.read_text(encoding="utf-8")) if LOG.exists() else {}
    for res in results:
        log[res["file"]] = res
    # .gitattributes 가 eol=lf 라 작업트리도 LF 로 쓴다 (윈도우 write_text 는 CRLF 를 넣는다)
    LOG.write_bytes(json.dumps(log, ensure_ascii=False, indent=1).replace("\r\n", "\n").encode("utf-8"))
    failed = [r["file"] for r in results if not r["ok"]]
    print(f"done: {len(results)-len(failed)} ok, {len(failed)} failed {failed}")


if __name__ == "__main__":
    main()
