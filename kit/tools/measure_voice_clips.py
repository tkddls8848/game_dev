#!/usr/bin/env python3
# ⚠️ 이 도구의 기본 대상 게임(games/mystery-blackwood)은 다른 저장소로 갔다 —
#    github.com/tkddls8848/game . 여기서 쓰려면 대상 게임을 인자로 주거나
#    아래 기본값을 이 저장소에 있는 게임으로 바꾼다.
# -*- coding: utf-8 -*-
"""
구워 낸 음성 파일의 **실제 길이**를 재서 JSON으로 남긴다.

    python tools/measure_voice_clips.py

왜 필요한가
-----------
`script.json`의 `durationMs`는 생성기가 글자 수로 **추정한** 값이다(build_case02.py).
`startMs`는 그 추정 위에서 짜였다. 실제 TTS 길이는 추정과 다르므로, 음성을 얹는 순간
설계가 조용히 어긋날 수 있다 — 특히 8시 14분의 **두 발화가 겹친다**는 보장이
풀리면 게임은 멀쩡히 돌면서 "되돌려 들을 이유"만 사라진다.

그래서 실측값을 파일로 남기고, `VoiceTimingDriftTests`가 그 파일로 판정한다.
추정 배율을 곱해 보는 기존 테스트와 달리 이쪽은 **실제로 뽑은 음성에 대한 판정**이다.

측정 방법
---------
MPEG1 Layer III 프레임 헤더를 세어 더한다. 파일 크기 ÷ 비트레이트 추정보다 정확하고,
ffmpeg 같은 외부 도구가 필요 없다(이 저장소의 다른 도구들과 같은 제약 — 표준 라이브러리만).

출력
----
    games/<게임>/unity/VoiceMeasurements/<caseId>.json    { "<utteranceId>": <ms>, ... }

`Assets/` 밖에 둔다. Unity가 임포트할 필요가 없는 개발용 산출물이다.
음성이 없는 발화(대본이 `…….`처럼 침묵으로 둔 자리)는 **넣지 않는다** —
`VoiceClipPlan.Check`가 빠진 항목을 작성값으로 대체하고 `Unmeasured`로 보고한다.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]      # kit/tools/x.py -> 저장소 뿌리

# 게임이 여러 개다. --game 으로 바꾼다.
DEFAULT_GAME = "games/mystery-blackwood"
DEFAULT_CASE = "case_02"

# MPEG1 Layer III. 인덱스 0(free)과 15(bad)는 유효하지 않다.
BITRATES_KBPS = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0]
SAMPLE_RATES = [44100, 48000, 32000, 0]
SAMPLES_PER_FRAME = 1152


def mp3_duration_ms(path: Path) -> float:
    """
    프레임을 세어 재생 길이를 ms로 돌려준다. 프레임을 하나도 못 찾으면 0.0.

    ID3v2 태그는 건너뛴다. 동기 워드(0xFFEx)는 오디오 데이터 안에서도 우연히 나올 수 있으므로
    **헤더가 온전할 때만** 프레임으로 인정하고, 아니면 한 바이트 밀어 다시 본다.
    """
    data = path.read_bytes()
    pos = 0

    if data[:3] == b"ID3" and len(data) >= 10:
        size = ((data[6] & 0x7F) << 21) | ((data[7] & 0x7F) << 14) \
             | ((data[8] & 0x7F) << 7) | (data[9] & 0x7F)
        pos = 10 + size

    total_ms = 0.0
    end = len(data)
    while pos + 4 <= end:
        if data[pos] != 0xFF or (data[pos + 1] & 0xE0) != 0xE0:
            pos += 1
            continue

        version = (data[pos + 1] >> 3) & 0x03    # 3 = MPEG1
        layer = (data[pos + 1] >> 1) & 0x03      # 1 = Layer III
        bitrate = BITRATES_KBPS[(data[pos + 2] >> 4) & 0x0F]
        sample_rate = SAMPLE_RATES[(data[pos + 2] >> 2) & 0x03]
        padding = (data[pos + 2] >> 1) & 0x01

        if version != 3 or layer != 1 or bitrate == 0 or sample_rate == 0:
            pos += 1
            continue

        frame_bytes = int(144000 * bitrate / sample_rate) + padding
        total_ms += SAMPLES_PER_FRAME / sample_rate * 1000.0
        pos += max(frame_bytes, 1)

    return total_ms


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description="음성 파일의 실제 길이를 재서 JSON으로 남긴다.")
    ap.add_argument("--case", default=DEFAULT_CASE)
    ap.add_argument("--game", default=DEFAULT_GAME,
                    help="저장소 뿌리 기준 게임 폴더 (기본: " + DEFAULT_GAME + ")")
    ap.add_argument("--quiet", action="store_true", help="표는 생략하고 결론만")
    args = ap.parse_args(argv)

    if sys.stdout.encoding and sys.stdout.encoding.lower() not in ("utf-8", "utf8"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    game = REPO_ROOT / args.game
    script_path = game / "unity/Assets/Resources/GameData/cases" / args.case / "script.json"
    clip_dir = game / "unity/Assets/Resources/Audio/Voice" / args.case
    out_path = game / "unity/VoiceMeasurements" / (args.case + ".json")

    if not script_path.is_file():
        print("대본이 없다: " + str(script_path), file=sys.stderr)
        return 1

    script = json.loads(script_path.read_text(encoding="utf-8"))
    utterances = script["utterances"]

    measured: dict[str, int] = {}
    silent: list[str] = []
    rows = []

    for u in utterances:
        clip = clip_dir / (u["id"] + ".mp3")
        if not clip.is_file() or clip.stat().st_size == 0:
            silent.append(u["id"])
            continue
        actual = mp3_duration_ms(clip)
        if actual <= 0:
            print("프레임을 찾지 못했다(손상?): " + clip.name, file=sys.stderr)
            silent.append(u["id"])
            continue
        measured[u["id"]] = round(actual)
        rows.append((u["id"], u["voiceId"], u["durationMs"], actual, len(u["text"])))

    if not rows:
        print("잴 파일이 없다: " + str(clip_dir), file=sys.stderr)
        return 1

    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(json.dumps(measured, ensure_ascii=False, indent=1, sort_keys=True),
                        encoding="utf-8")

    ratios = sorted(a / e for _, _, e, a, _ in rows if e > 0)
    longer = sorted(((a / e, i, e, a) for i, v, e, a, c in rows if a > e), reverse=True)

    print("잰 클립 %d개 · 음성 없는 발화 %d개" % (len(rows), len(silent)))
    if silent:
        print("  음성 없음: " + ", ".join(silent))
        print("  (대본이 침묵으로 둔 자리라면 정상이다. 아니라면 그 발화만 다시 뽑을 것)")
    print("실제 / 추정 : 최소 %.2f · 중간 %.2f · 평균 %.2f · 최대 %.2f"
          % (ratios[0], ratios[len(ratios) // 2], sum(ratios) / len(ratios), ratios[-1]))
    print("추정보다 긴 것 %d개 / %d" % (len(longer), len(rows)))

    if longer and not args.quiet:
        print("\n가장 많이 초과한 발화")
        for ratio, uid, est, act in longer[:10]:
            print("  %-22s 추정 %6dms  실제 %6.0fms  x%.2f" % (uid, est, act, ratio))

    print("\n실측값 → " + str(out_path))
    print("이제 설계가 버티는지 판정한다:")
    print("  dotnet test games/mystery-blackwood/tests --filter VoiceTimingDriftTests")
    return 0


if __name__ == "__main__":
    sys.exit(main())
