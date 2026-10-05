#!/usr/bin/env python3
"""생성 이미지를 실제 1996년 화면의 해상도와 색 수로 되돌린다.

    python docs/poc-gallery/concepts/_tools/retro_pass.py <입력.png> <출력.jpg> [--w 640 --h 480 --colors 256]

이미지 생성기는 "640x480 · 256색"이라고 시켜도 고해상도로 그린다. 그래서 프롬프트로는 끝나지 않는다 —
실제로 원본 해상도로 줄이고, 색을 팔레트 하나로 묶고(디더링 없이, 원작처럼 띠가 진다),
최근접 보간으로 2배 키워 픽셀이 보이게 한다.
"""
import argparse

from PIL import Image


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("dst")
    ap.add_argument("--w", type=int, default=640)
    ap.add_argument("--h", type=int, default=480)
    ap.add_argument("--colors", type=int, default=256)
    ap.add_argument("--scale", type=int, default=2)
    a = ap.parse_args()

    im = Image.open(a.src).convert("RGB").resize((a.w, a.h), Image.LANCZOS)
    im = im.quantize(colors=a.colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
    im = im.resize((a.w * a.scale, a.h * a.scale), Image.NEAREST)
    # 픽셀 경계가 번지지 않게 JPEG 품질을 높게 둔다
    im.save(a.dst, "JPEG", quality=95, subsampling=0)
    print(a.dst, im.size)


if __name__ == "__main__":
    main()
