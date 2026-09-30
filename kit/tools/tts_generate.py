#!/usr/bin/env python3
# ⚠️ 이 도구의 기본 대상 게임(games/mystery-blackwood)은 다른 저장소로 갔다 —
#    github.com/tkddls8848/game . 여기서 쓰려면 대상 게임을 인자로 주거나
#    아래 기본값을 이 저장소에 있는 게임으로 바꾼다.
# -*- coding: utf-8 -*-
"""
script.json의 발화를 상업 TTS로 합성해 파일로 굽는다. (초안)

    python tools/tts_generate.py --dry-run     # 무엇을 호출할지만 본다
    python tools/tts_generate.py               # 190발화 전량 생성

공급자는 **ElevenLabs 하나**다. Azure·Google 경로는 지웠다 — 쓰지 않는 분기가 남아 있으면
어느 쪽이 진짜인지 읽는 사람이 매번 판단해야 한다. 다시 필요해지면 `Provider`를 하나 더
써서 `PROVIDERS`에 넣으면 된다(그 구조는 남겨 뒀다).

설계 원칙
---------
1. **네트워크를 만지는 코드는 `Provider.synth` 하나뿐이다.** 나머지는 전부 순수 계산이므로
   `--dry-run`으로 끝까지 검사할 수 있다.
2. **API 키는 환경변수에서만 읽는다.** 코드·문서·로그·리포트 어디에도 키를 적지 않는다.
   오류 메시지에 URL을 실을 때는 `_redact()`를 통과시킨다.
3. **기본은 dry-run이 아니라 실호출이지만, 키가 없으면 실행 자체를 거부한다.**
   `--dry-run`은 무엇을 호출할지만 출력하고 네트워크를 만지지 않으며 파일도 쓰지 않는다.

필요한 키
---------
    ELEVENLABS_API_KEY

저장소 뿌리의 `.env`(gitignore에 있다)나 셸 환경변수로 넣는다 — `.env.example` 참고.
**Starter 이상 유료 플랜이어야 한다.** 무료 플랜 출력에는 상업 이용 권리가 없다.

출력
----
    games/<게임>/unity/Assets/Resources/Audio/Voice/case_02/<utteranceId>.mp3

Unity에서는 확장자를 뺀 Resources 경로로 읽는다 → `Audio/Voice/case_02/u001`.
`Utterance.clip`(Assets/Scripts/Eavesdrop/UtteranceSchema.cs)에 넣을 값이 그것이다.
`--clip-map`을 주면 id → clip 경로 대응표를 JSON으로 따로 뽑아 준다
(이 스크립트는 script.json을 **수정하지 않는다**).

파일을 새로 넣은 뒤에는 Unity가 `.meta`를 만든다 → 그 뒤 커밋해서 GUID를 고정할 것.

표준 라이브러리만 쓴다. 외부 SDK 설치가 필요 없다.
"""

from __future__ import annotations

import argparse
import base64
import collections
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

# 저장소 루트 = 이 파일의 부모의 부모
REPO_ROOT = Path(__file__).resolve().parents[2]      # kit/tools/x.py -> 저장소 뿌리

# 게임이 여러 개다. --game 으로 바꾼다.
DEFAULT_GAME = "games/mystery-blackwood"
GAME_ROOT = REPO_ROOT / DEFAULT_GAME

DEFAULT_SCRIPT = GAME_ROOT / "unity/Assets/Resources/GameData/cases/case_02/script.json"
DEFAULT_OUT_DIR = GAME_ROOT / "unity/Assets/Resources/Audio/Voice/case_02"

# 시청(audition)용 출력. Assets 밖이다 — Unity가 임포트하면 안 되는 버리는 파일들이다.
AUDITION_OUT_DIR = REPO_ROOT / "tts_audition"

# 배역별 시청 대사. 대본에서 고른 실제 발화 id이고, 본문은 실행 시 script.json에서 읽는다
# (여기 텍스트를 복사해 두면 대본이 바뀔 때 어긋난다).
#   v1 클라라 — 담담한 설명 밑에 뭔가 깔린 줄
#   v2 마르코 — 의문문 + 불평. 억양이 드러난다
#   v3 줄리안 — 말줄임표로 시작하는 비꼼. 휴지 처리를 본다
#   v4 헬렌   — 평서 + 의문이 한 줄에. 침착한 톤
#   v5 에드먼드 — 숫자·말줄임표·자문자답. 노년 톤과 휴지를 한꺼번에 본다
# 대본에서 고른 실제 발화. **극적인 줄로 고른다** — 4차 청취본에서 배운 것:
# 1차가 차분했던 건 지시가 없어서가 아니라 대사가 차분해서였다(진찰·수프).
# v3 는 ElevenLabs 4차에 쓴 것과 같은 줄이라 바로 견줄 수 있다.
AUDITION_IDS = {
    "v1": "a_dining_17",   # 클라라 — 감추는 사람의 부탁
    "v2": "c_dining_11",   # 마르코 — 목격 증언, 53자
    "v3": "c_hall_05",     # 줄리안 — 속삭이는 경고 (4차와 동일)
    "v4": "a_study_18",    # 헬렌 — 진료 지시
    "v5": "a_study_11",    # 에드먼드 — 추궁
}

# Resources.Load에 쓰는 경로의 접두사 (Assets/Resources/ 아래 기준, 확장자 없음)
RESOURCES_PREFIX = "Audio/Voice/case_02"

HTTP_TIMEOUT_SEC = 60


# --------------------------------------------------------------------------
# 비밀값 취급
# --------------------------------------------------------------------------

def _redact(s: str) -> str:
    """URL/메시지에서 키처럼 보이는 것을 가린다. 로그로 나가는 모든 문자열은 여기를 통과한다."""
    s = re.sub(r"([?&](?:key|api_key|apikey|token)=)[^&\s]+", r"\1<redacted>", s, flags=re.I)
    s = re.sub(r"(sk_|xi-api-key:\s*)[A-Za-z0-9_\-]{8,}", r"\1<redacted>", s)
    return s


def _load_dotenv(path: Path = None) -> dict[str, str]:
    """
    저장소 뿌리의 `.env`를 읽는다. 셸을 새로 열 때마다 키를 다시 넣지 않아도 되게 한다.

    `.env`는 **gitignore에 들어 있다**(`.env.example`만 커밋한다). 그래도 이 함수는
    값을 어디에도 출력하지 않는다 — 실수로 로그에 찍히는 경로를 남기지 않는다.

    형식은 흔한 dotenv 문법의 최소 집합이다:
        KEY=value            · KEY="value" · KEY='value'
        # 주석               · 빈 줄
        export KEY=value     (앞의 export는 무시한다)

    파일이 없으면 빈 딕셔너리를 돌려준다 — 없는 것이 오류는 아니다.
    """
    if path is None:
        path = REPO_ROOT / ".env"
    if not path.is_file():
        return {}

    values = {}
    for raw in path.read_text(encoding="utf-8-sig").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("export "):
            line = line[len("export "):].lstrip()
        if "=" not in line:
            continue
        key, _, value = line.partition("=")
        key = key.strip()
        value = value.strip()
        # 따옴표로 감싼 값은 벗긴다. 안쪽 주석은 건드리지 않는다(키에 #이 들어갈 수 있다).
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            value = value[1:-1]
        if key:
            values[key] = value
    return values


def _require_env(names: list[str]) -> dict[str, str]:
    """
    키를 읽는다. 값은 절대 출력하지 않는다.

    찾는 순서: **실제 환경변수 → `.env`**. 환경변수가 이기는 것은 dotenv의 관례이고,
    CI나 일회성 실행에서 파일을 고치지 않고 덮어쓸 수 있어야 하기 때문이다.
    """
    from_file = _load_dotenv()
    out = {}
    missing = []
    for n in names:
        v = (os.environ.get(n) or from_file.get(n) or "").strip()
        if not v:
            missing.append(n)
        else:
            out[n] = v
    if missing:
        raise SystemExit(
            "키가 없다: " + ", ".join(missing) + "\n"
            "\n둘 중 하나로 넣는다. 어느 쪽이든 커밋되지 않는다(.env는 gitignore에 있다).\n"
            "\n  1) 저장소 뿌리에 .env 파일 — 한 번 넣으면 계속 쓴다\n"
            "       " + str(REPO_ROOT / ".env") + "\n"
            "       형식은 .env.example 을 그대로 복사해 값만 채운다\n"
            "\n  2) 셸 환경변수 — 이번 셸에서만 쓴다\n"
            "       PowerShell:  $env:" + names[0] + " = '...'\n"
            "       bash:        export " + names[0] + "='...'"
        )
    return out


# 일시적 실패에만 다시 건다. 429(속도 제한)·5xx·연결 끊김이 그것이다.
# 401(키 문제)·400(요청이 틀림)은 다시 걸어도 같은 답이 오므로 즉시 올린다.
RETRY_STATUS = frozenset({429, 500, 502, 503, 504})
RETRY_MAX = 4
RETRY_BACKOFF_SEC = 2.0


def _post(url: str, data: bytes, headers: dict) -> bytes:
    """
    POST 한 번. 발화 190개를 순차로 부르므로 **일시적 실패는 여기서 삼킨다** —
    전량 생성이 중간에 죽어 처음부터 다시 돌리는 상황을 만들지 않는다.
    (이미 받은 파일은 건너뛰므로 재실행 자체도 안전하다. 재시도는 그럴 일을 줄인다.)
    """
    last = ""
    for attempt in range(1, RETRY_MAX + 1):
        req = urllib.request.Request(url, data=data, headers=headers, method="POST")
        try:
            with urllib.request.urlopen(req, timeout=HTTP_TIMEOUT_SEC) as resp:
                return resp.read()
        except urllib.error.HTTPError as e:
            body = e.read()[:400].decode("utf-8", "replace")
            last = "HTTP %d — %s\n%s" % (e.code, _redact(url), _redact(body))
            if e.code not in RETRY_STATUS:
                raise RuntimeError(last) from None
        except urllib.error.URLError as e:
            last = "연결 실패 — %s: %s" % (_redact(url), e.reason)
        except TimeoutError:
            last = "%d초 안에 응답이 없다 — %s" % (HTTP_TIMEOUT_SEC, _redact(url))

        if attempt < RETRY_MAX:
            wait = RETRY_BACKOFF_SEC * (2 ** (attempt - 1))
            print("    다시 시도 %d/%d (%.0f초 뒤) — %s"
                  % (attempt, RETRY_MAX - 1, wait, last.splitlines()[0]), file=sys.stderr)
            time.sleep(wait)

    raise RuntimeError("%d번 시도 모두 실패 — %s" % (RETRY_MAX, last))


# --------------------------------------------------------------------------
# 공급자 — 여기가 전부다. 교체하려면 이 아래에 하나 더 쓴다.
# --------------------------------------------------------------------------

class Provider:
    """공급자 하나. 네트워크를 만지는 것은 self.synth 뿐이다."""

    def __init__(self, name, env, ext, price_per_million, max_chars,
                 default_voices, synth, billed_chars, candidates=(), note=""):
        self.name = name
        self.env = env                      # 필요한 환경변수 이름들
        self.ext = ext                      # 출력 확장자
        self.price_per_million = price_per_million   # USD / 1M자 (0 = 크레딧제)
        self.max_chars = max_chars          # 1회 호출 최대 문자 수
        self.default_voices = default_voices  # voiceId -> 공급자 음성 이름
        self.synth = synth                  # (creds, text, voice, style) -> bytes  ← 유일한 호출 지점
        self.billed_chars = billed_chars    # (text, voice, style) -> int
        self.candidates = list(candidates)  # 시청해 볼 만한 ko 음성 전체 (--list-voices)
        self.note = note


# ---- ElevenLabs -----------------------------------------------------------

# 모델. `eleven_v3`는 한국어를 지원하고 비용 계수가 1.0이다(문자 1 = 크레딧 1).
# SSML을 받지 않는다 — 휴지는 대본의 문장부호가 만든다. 대본에 감정 태그를 쓰지 않는
# 이유는 4차 청취본 기록에 있다(AssetDownloads/audition/artlist-plain/듣는_순서.md):
# 태그를 박으면 "감정을 느끼는" 대신 "감정을 연기하는" 소리가 났다.
ELEVEN_MODEL = "eleven_v3"

# v3의 stability는 연속값이 아니라 세 지점이다: 0.0 Creative / 0.5 Natural / 1.0 Robust.
# 0.2까지 내렸던 3차가 "발연기"로 들린 원인이 이것이다 — 낮은 값은 감정이 아니라 불안정이다.
ELEVEN_STABILITY = 0.5

# 배역. **이름이 아니라 voice_id로 지정한다.**
#
# 목소리를 구별하는 것이 이 게임의 퍼즐이므로 고른 기준은 음색 취향이 아니라
# **성별·연령·음색이 서로 겹치지 않는 것**이다. 다섯이 각각 다른 칸을 차지한다:
#
#     노년 남 / 중년 남(허스키) / 청년 남 / 중년 여(연기 톤) / 중년 여(명료)
#
# 4차 청취본의 배역 이름(Stride·Edge·Hype·Precision·Professor)은 Artlist가 붙인
# 라벨이고 ElevenLabs 쪽에는 없다. 그래서 같은 성격의 칸을 계정 음성으로 다시 채웠다.
ELEVEN_VOICES = {
    # 클라라 보스 — 비서. 횡령을 들킨 쪽이라 담담한 밑에 뭔가 깔려야 한다 → 연기 폭이 있는 쪽
    "v1": "pFZP5JQG7iQjIQuC4Bku",   # Lily — 여, 중년, velvety actress
    # 마르코 벨리니 — 요리사. 허스키한 음색이 다른 넷과 가장 멀다
    "v2": "N2lVS1w4EtoT3dr4eOWO",   # Callum — 남, 중년, husky
    # 줄리안 헤일 — 조카. 유일한 청년 남자 톤
    "v3": "IKne3meq5aSn9XLyUdCD",   # Charlie — 남, 청년
    # 헬렌 모로 — 의사. 진찰·지시가 많아 또박또박해야 한다. 클라라와 음색이 갈린다
    "v4": "Xb7hH8MSUJpSbSDYk0k2",   # Alice — 여, 중년, clear
    # 에드먼드 헤일 — 저택 주인. 계정에서 유일한 노년 음성이다
    "v5": "pqHfZKP75CvOlQylNhV4",   # Bill — 남, 노년, wise/mature
}


def _eleven_synth(creds: dict, text: str, voice: str, style: dict) -> bytes:
    # voice는 voice_id다(이름이 아니다).
    url = ("https://api.elevenlabs.io/v1/text-to-speech/" + voice
           + "?output_format=mp3_44100_128")
    settings = {
        "stability": float(style.get("stability", ELEVEN_STABILITY)),
        "similarity_boost": float(style.get("similarity_boost", 0.75)),
    }
    # speed는 준 배역에만 보낸다. 안 보내면 공급자 기본값이고, 값이 틀리면 400이 와서 바로 안다.
    if style.get("speed"):
        settings["speed"] = float(style["speed"])

    payload = {
        "text": text,
        "model_id": style.get("model_id", ELEVEN_MODEL),
        # **반드시 명시한다.** 빼면 영어로 알아듣고 한국어를 영어식으로 읽는다.
        "language_code": style.get("language_code", "ko"),
        "voice_settings": settings,
    }
    return _post(url, json.dumps(payload).encode("utf-8"), {
        "xi-api-key": creds["ELEVENLABS_API_KEY"],
        "Content-Type": "application/json",
        "Accept": "audio/mpeg",
    })


ELEVENLABS = Provider(
    name="elevenlabs",
    env=["ELEVENLABS_API_KEY"],
    ext=".mp3",                      # ogg를 주지 않는다. Unity는 mp3를 그대로 읽는다
    price_per_million=0.0,           # 크레딧제 (1문자 = 1크레딧)
    max_chars=5000,                  # eleven_v3 한 번 호출 상한
    default_voices=dict(ELEVEN_VOICES),
    synth=_eleven_synth,
    billed_chars=lambda text, voice, style: len(text),
    candidates=sorted(ELEVEN_VOICES.values()),
    note="Starter 이상 유료 플랜에서만 상업 이용 가능. 목소리는 voice_id로 지정한다. "
         "약관 4(d)에 따라 **입력 대본**에 영구·취소불가 재사용 라이선스를 준다"
         "(docs/TTS_OPTIONS.md 3장).",
)


# ---- Google Gemini TTS -----------------------------------------------------

# 2026-09 에 나온 `gemini-3.8-flash-tts`. 예전에 탈락시킨 Google **Cloud** TTS
# (Chirp 3: HD)와 같은 회사지만 다른 제품이다 — 탈락 이유였던 "감정 지정이 없다"를
# 이것이 없앴다. 지시를 **자연어로 텍스트 앞에 붙여** 억양·속도·감정을 준다.
#
# 실측(2026-09-27): 완전한 WAV(RIFF, 24kHz 모노 16bit)를 돌려주므로 변환이 필요 없고,
# 오디오 토큰은 **초당 약 32개**다. 본편 음성 약 875초 = 28,000토큰 ≈ $0.25.
#
# ⚠️ **무료 티어는 프롬프트를 제품 개선에 쓴다.** 청취본은 괜찮지만 본편 전량은
# 유료 티어로 돌려야 한다 — games/mystery-blackwood/docs/PLAN_RELEASE.md §1.3
GEMINI_MODEL = "gemini-3.8-flash-tts"

# 배역. Gemini 기본 음성 30종에서 성격이 맞는 칸을 골랐다.
# ElevenLabs 쪽은 영어권 기본 음성을 빌려 써서 한국어 억양이 샐 위험이 남아 있었는데,
# 이쪽은 음성 자체에 성격 이름이 붙어 있어 배역을 맞추기 쉽다.
GEMINI_VOICES = {
    "v1": "Despina",    # 클라라 — 여, smooth. 담담한 밑에 뭔가 깔린 비서
    "v2": "Algenib",    # 마르코 — 남, gravelly. 허스키한 요리사
    "v3": "Puck",       # 줄리안 — 남, upbeat. 유일한 청년 톤
    "v4": "Erinome",    # 헬렌 — 여, clear. 또박또박한 의사
    "v5": "Gacrux",     # 에드먼드 — 남, mature. 노년 저택 주인
}

# 배역별 자연어 연기 지시.
#
# ⚠️ **속도를 반드시 명시한다.** 실측(2026-09-27): 같은 대사를
#   지시 없음 4.6초 / "한 박자 늦게" 12.4초 / "빠르게" 7.0초 / "초당 약 6자" 4.0초.
#   ElevenLabs 는 태그를 빼야 연기가 살았는데 Gemini 는 반대로 **지시를 너무 잘 따른다** —
#   "느리게"라고 쓰면 정말 2.8배 느려져 회차 타이밍이 무너진다.
#   설계는 850ms + 128ms/자(초당 약 7.8자)를 가정하고, 1.3~1.6배까지만 견딘다.
#
# 그리고 **결과가 아니라 상황을 준다** — "화내라"고 하면 흉내가 나온다(ElevenLabs 3차에서 배운 것).
GEMINI_PACE = "한국어 자연스러운 대화 속도(초당 약 6자)로"

GEMINI_DIRECTION = {
    "v1": GEMINI_PACE + ", 담담하게. 감추는 것이 있는 사람이지만 티를 내지 않는다",
    "v2": GEMINI_PACE + ", 낮고 거친 목소리로. 주방에서 손을 놀리며 말하듯",
    "v3": GEMINI_PACE + ", 젊고 조금 들뜬 목소리로",
    "v4": GEMINI_PACE + ", 또박또박. 진료하듯",
    "v5": GEMINI_PACE + ", 노년의 낮은 목소리로",
}


def _gemini_synth(creds: dict, text: str, voice: str, style: dict) -> bytes:
    """
    Gemini TTS 한 번. 완전한 WAV 바이트를 돌려준다.

    연기 지시는 **본문 앞에 자연어로 붙인다**. SSML 이 아니라 모델이 읽는 지시다.
    """
    direction = style.get("direction")
    prompt = (direction + ": " + text) if direction else text

    payload = {
        "contents": [{"parts": [{"text": prompt}]}],
        "generationConfig": {
            "responseModalities": ["AUDIO"],
            "speechConfig": {
                "voiceConfig": {"prebuiltVoiceConfig": {"voiceName": voice}}
            },
        },
    }
    url = ("https://generativelanguage.googleapis.com/v1beta/models/"
           + style.get("model", GEMINI_MODEL) + ":generateContent")
    raw = _post(url, json.dumps(payload).encode("utf-8"), {
        "x-goog-api-key": creds["GEMINI_API_KEY"],
        "Content-Type": "application/json",
    })

    doc = json.loads(raw.decode("utf-8"))
    try:
        part = doc["candidates"][0]["content"]["parts"][0]
        inline = part.get("inlineData") or part.get("inline_data")
        return base64.b64decode(inline["data"])
    except (KeyError, IndexError, TypeError):
        # 안전 필터에 걸리거나 모달리티가 빠지면 여기로 온다. 무엇이 왔는지 보여 준다.
        raise RuntimeError("오디오가 없는 응답: " + _redact(raw.decode("utf-8", "replace")[:400]))


GEMINI = Provider(
    name="gemini",
    env=["GEMINI_API_KEY"],
    ext=".wav",                      # 완전한 WAV(RIFF). Unity 가 그대로 읽는다
    price_per_million=9.0,           # 오디오 출력 $9/1M 토큰 (2026-12-31 까지). 입력 텍스트는 $0.50
    max_chars=5000,
    default_voices=dict(GEMINI_VOICES),
    synth=_gemini_synth,
    # 과금은 문자가 아니라 **오디오 토큰**이다. 실측 초당 32토큰, 한국어 약 6.2자/초 기준
    # 문자당 대략 5토큰으로 어림잡는다 — 정확한 값은 응답의 usageMetadata 에 있다.
    billed_chars=lambda text, voice, style: len(text) * 5,
    candidates=sorted(set(GEMINI_VOICES.values())),
    note="WAV 는 무압축이라 클립당 ~500KB다(190개면 ~95MB). 저장소에 넣기 전에 ogg/mp3 로 "
         "줄일지 정할 것. **무료 티어는 프롬프트를 제품 개선에 쓴다** — 본편 전량은 유료 티어로.",
)


PROVIDERS = {p.name: p for p in (ELEVENLABS, GEMINI)}


# --------------------------------------------------------------------------
# 대본 읽기
# --------------------------------------------------------------------------

def load_script(path: Path) -> dict:
    if not path.is_file():
        raise SystemExit(f"대본이 없다: {path}")
    data = json.loads(path.read_text(encoding="utf-8"))
    utts = data.get("utterances") or []
    if not utts:
        raise SystemExit(f"발화가 없다: {path}")

    seen = set()
    for u in utts:
        uid = u.get("id") or ""
        if not uid:
            raise SystemExit("id가 빈 발화가 있다.")
        if uid in seen:
            raise SystemExit(f"발화 id가 겹친다: {uid}")
        seen.add(uid)
        if not (u.get("text") or "").strip():
            raise SystemExit(f"{uid}: text가 비어 있다.")
        if not u.get("voiceId"):
            raise SystemExit(f"{uid}: voiceId가 없다.")
    return data


def load_json_map(path: str | None, what: str) -> dict:
    if not path:
        return {}
    p = Path(path)
    if not p.is_file():
        raise SystemExit(f"{what} 파일이 없다: {p}")
    return json.loads(p.read_text(encoding="utf-8"))


# --------------------------------------------------------------------------
# 계획 세우기
# --------------------------------------------------------------------------

Job = collections.namedtuple("Job", "uid voice_id provider_voice text style out_path chars billed")


def build_jobs(data, provider, voice_map, style_map, out_dir, args) -> list[Job]:
    only = {s.strip() for s in args.only.split(",")} if args.only else None
    jobs = []
    for u in data["utterances"]:
        uid, vid = u["id"], u["voiceId"]
        if only and uid not in only:
            continue
        if args.voice and vid != args.voice:
            continue
        pv = voice_map.get(vid)
        if not pv:
            raise SystemExit(
                f"{uid}: 목소리 '{vid}'에 대응하는 {provider.name} 음성이 없다.\n"
                f"--voice-map 으로 넘기거나 PROVIDERS['{provider.name}'].default_voices 를 채워라."
            )
        text = u["text"].strip()
        style = dict(style_map.get(vid, {}))
        style.update(style_map.get(uid, {}))     # 발화 단위 override
        out = out_dir / (uid + provider.ext)
        jobs.append(Job(uid, vid, pv, text, style, out,
                        len(text), provider.billed_chars(text, pv, style)))
        if args.limit and len(jobs) >= args.limit:
            break
    return jobs


def _safe(name: str) -> str:
    """음성 이름을 파일명에 쓸 수 있게 만든다 (ko-KR-Haena:MAI-Voice-2 → ko-KR-Haena_MAI-Voice-2)."""
    return re.sub(r"[^0-9A-Za-z._-]", "_", name)


def build_audition_jobs(data, provider, voice_map, style_map, out_dir, args) -> list[Job]:
    """
    배역별 시청용 대사 한 줄씩만 만든다.

    기본  : 배역 5개 × 각자의 대사 1줄 (현재 voice map 기준)
    비교용: --audition-voices 로 음성들을 지정하면 같은 대사 한 줄을 그 음성 전부로 만든다
    """
    by_id = {u["id"]: u for u in data["utterances"]}
    jobs = []

    if args.audition_voices:
        # 한 대사를 여러 음성으로 — 배역에 누구를 앉힐지 고를 때 쓴다
        line_id = args.audition_line or AUDITION_IDS.get(args.voice or "v5", "u067")
        u = by_id.get(line_id)
        if not u:
            raise SystemExit(f"--audition-line: 대본에 없는 발화 id다: {line_id}")
        names = [n.strip() for n in args.audition_voices.split(",") if n.strip()]
        if names == ["all"]:
            names = provider.candidates
            if not names:
                raise SystemExit(f"{provider.name}에는 등록된 후보 음성 목록이 없다. 이름을 직접 넘겨라.")
        for pv in names:
            style = dict(style_map.get(u["voiceId"], {}))
            out = out_dir / f"{line_id}__{_safe(pv)}{provider.ext}"
            jobs.append(Job(f"{line_id}@{pv}", u["voiceId"], pv, u["text"], style, out,
                            len(u["text"]), provider.billed_chars(u["text"], pv, style)))
        return jobs

    # 배역별 한 줄씩
    for vid in sorted(voice_map):
        if args.voice and vid != args.voice:
            continue
        line_id = AUDITION_IDS.get(vid)
        if not line_id:
            print(f"[건너뜀] {vid}: AUDITION_IDS에 시청 대사가 정해져 있지 않다.")
            continue
        u = by_id.get(line_id)
        if not u:
            raise SystemExit(f"{vid}: 대본에 없는 발화 id다: {line_id}")
        pv = voice_map[vid]
        style = dict(style_map.get(vid, {}))
        style.update(style_map.get(line_id, {}))
        out = out_dir / f"{vid}__{_safe(pv)}{provider.ext}"
        jobs.append(Job(f"{line_id}({vid})", vid, pv, u["text"], style, out,
                        len(u["text"]), provider.billed_chars(u["text"], pv, style)))
    return jobs


def report(jobs, provider, out_dir, existing, args) -> dict:
    # 한 대사를 여러 음성으로 굽는 중이면 배역이 아니라 음성이 비교 축이다
    per_provider_voice = bool(getattr(args, "audition_voices", None))
    by_voice = collections.defaultdict(lambda: {"calls": 0, "chars": 0, "billed": 0, "voice": ""})
    for j in jobs:
        b = by_voice[j.provider_voice if per_provider_voice else j.voice_id]
        b["calls"] += 1
        b["chars"] += j.chars
        b["billed"] += j.billed
        b["voice"] = j.provider_voice

    total_chars = sum(j.chars for j in jobs)
    total_billed = sum(j.billed for j in jobs)
    cost = total_billed * provider.price_per_million / 1_000_000.0
    too_long = [j.uid for j in jobs if j.billed > provider.max_chars]

    audition = getattr(args, "audition", False)
    print(f"\n=== TTS {'시청(audition)' if audition else '생성'} 계획 — provider={provider.name} ===")
    print(f"출력 디렉터리 : {out_dir}")
    if audition:
        print("               (Assets 밖이다. 다 듣고 나면 통째로 지워도 된다)")
    else:
        print(f"Resources 경로: {RESOURCES_PREFIX}/<id>   (확장자 없음 · Utterance.clip에 넣을 값)")
    print(f"확장자        : {provider.ext}")
    print(f"호출 수       : {len(jobs)}"
          + (f"  (이미 있어 건너뜀 {existing})" if existing else ""))
    print(f"문자 수       : {total_chars:,}  (과금 기준 추정 {total_billed:,})")
    if provider.price_per_million:
        print(f"예상 비용     : ${cost:.4f}  (@ ${provider.price_per_million:.2f} / 1M자, 무료 한도 미반영)")
    else:
        print(f"예상 소모     : {total_billed:,} 크레딧 (1문자 = 1크레딧)")

    if per_provider_voice:
        print("\n음성별 분포")
        print(f"  {'발화':>5} {'문자':>7} {'과금추정':>9}  공급자 음성")
        for key in sorted(by_voice):
            b = by_voice[key]
            print(f"  {b['calls']:>5} {b['chars']:>7,} {b['billed']:>9,}  {key}")
    else:
        print("\n목소리별 분포")
        print(f"  {'id':<5} {'발화':>5} {'문자':>7} {'과금추정':>9}  공급자 음성")
        for vid in sorted(by_voice):
            b = by_voice[vid]
            print(f"  {vid:<5} {b['calls']:>5} {b['chars']:>7,} {b['billed']:>9,}  {b['voice']}")

    if too_long:
        print(f"\n[경고] 1회 호출 한도({provider.max_chars}자)를 넘는 발화: {', '.join(too_long)}")
    if provider.ext != ".mp3":
        print(f"\n[참고] 출력이 {provider.ext}다. Unity가 읽는 형식인지 확인할 것.")
    if provider.note:
        print(f"\n[참고] {provider.note}")

    if args.verbose:
        print("\n발화별")
        for j in jobs:
            print(f"  {j.uid}  {j.voice_id}→{j.provider_voice:<32} {j.chars:>3}자  "
                  f"style={j.style or '-'}  →  {j.out_path.name}")

    return {
        "provider": provider.name,
        "calls": len(jobs),
        "skipped_existing": existing,
        "total_chars": total_chars,
        "billed_chars_estimate": total_billed,
        "estimated_usd": round(cost, 4) if provider.price_per_million else None,
        "extension": provider.ext,
        "out_dir": str(out_dir),
        "resources_prefix": RESOURCES_PREFIX,
        "by_voice": {k: dict(v) for k, v in by_voice.items()},
        "oversize": too_long,
    }


# --------------------------------------------------------------------------
# main
# --------------------------------------------------------------------------

def _force_utf8_console() -> None:
    """Windows 콘솔 기본 코드페이지(cp949)에서 한글·기호가 터지는 것을 막는다."""
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except Exception:  # noqa: BLE001 — 재설정이 안 되는 환경이면 그냥 둔다
            pass


def _get_json(url: str, headers: dict):
    req = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=HTTP_TIMEOUT_SEC) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        body = e.read()[:300].decode("utf-8", "replace")
        raise RuntimeError("HTTP %d — %s\n%s" % (e.code, _redact(url), _redact(body))) from None
    except urllib.error.URLError as e:
        raise RuntimeError("연결 실패 — %s: %s" % (_redact(url), e.reason)) from None


def list_voices(provider) -> int:
    """
    계정에서 **실제로 쓸 수 있는** 음성을 공급자에게 물어 출력한다.

    이름을 코드에 박아 두지 않는 이유가 이것이다 — 목록은 계정과 시점에 따라 다르고,
    없는 이름을 넣으면 실행 시 알기 어려운 오류가 난다. 배역을 다시 고를 때 여기서 출발한다.

    키가 없으면 API를 부르지 않고 **현재 배역만** 출력한다. 키 없이 훑어보는 경로를 남겨 둔다.
    """
    try:
        creds = _require_env(provider.env)
    except SystemExit:
        print("키가 없어 목록을 물어볼 수 없다. 현재 배역만 보여 준다.\n")
        for vid in sorted(provider.default_voices):
            print("  %-4s %s" % (vid, provider.default_voices[vid]))
        print("\n키를 넣으면 계정의 전체 음성을 물어본다 (.env.example 참고).")
        return 0

    data = _get_json("https://api.elevenlabs.io/v1/voices",
                     {"xi-api-key": creds["ELEVENLABS_API_KEY"]})
    voices = data.get("voices", [])

    # voice_id → 우리 배역. 지금 쓰는 것에 표시를 달아 준다.
    assigned = {v: k for k, v in provider.default_voices.items()}

    print("계정에서 쓸 수 있는 음성 %d종" % len(voices))
    print("  (eleven_v3는 70여 언어를 지원한다 — 한국어는 language_code로 지정하므로")
    print("   목록이 ko로 따로 갈리지 않는다. 억양은 들어 봐야 안다.)\n")
    print("  %-4s %-22s %-30s %s" % ("배역", "voice_id", "이름", "성별 · 연령 · 성격"))
    for v in voices:
        lab = v.get("labels") or {}
        vid = v.get("voice_id", "?")
        print("  %-4s %-22s %-30s %s" % (
            assigned.get(vid, ""), vid, (v.get("name") or "?")[:30],
            " · ".join(x for x in (lab.get("gender"), lab.get("age"),
                                   lab.get("descriptive") or lab.get("description")) if x)))

    print("\n한 대사로 후보들을 들어 보기:")
    print("  python tools/tts_generate.py --audition --audition-voices <id1>,<id2>")
    print("배역을 바꾸려면 ELEVEN_VOICES(이 파일)를 고치거나 --voice-map JSON을 준다.")
    return 0


def main(argv=None) -> int:
    _force_utf8_console()
    ap = argparse.ArgumentParser(
        description="script.json의 발화를 상업 TTS로 합성해 파일로 굽는다.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="API 키는 환경변수로만 넘긴다. 절대 인자로 받지 않는다.",
    )
    ap.add_argument("--provider", choices=sorted(PROVIDERS), default="elevenlabs")
    ap.add_argument("--script", type=Path, default=DEFAULT_SCRIPT)
    ap.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    ap.add_argument("--voice-map", help='JSON: {"v1": "<공급자 음성 이름>", ...}')
    ap.add_argument("--style-map",
                    help='JSON: {"v5": {"rate": "-8%%", "pitch": "-2st"}, "u123": {"style": "angry"}} '
                         "— 키는 voiceId 또는 발화 id")
    ap.add_argument("--dry-run", action="store_true",
                    help="무엇을 호출할지만 출력한다. 네트워크도 파일 쓰기도 없다.")
    ap.add_argument("--force", action="store_true", help="이미 있는 파일도 다시 만든다")
    ap.add_argument("--only", help="쉼표로 구분한 발화 id만")
    ap.add_argument("--voice", help="이 목소리(v1 등)만")
    ap.add_argument("--limit", type=int, help="앞에서 N개만 (시험용)")
    ap.add_argument("--clip-map", type=Path,
                    help="id → Resources clip 경로 대응표를 이 JSON으로 쓴다 (script.json은 건드리지 않는다)")
    ap.add_argument("--report", type=Path, help="계획/결과 요약을 이 JSON으로 쓴다")
    ap.add_argument("--verbose", action="store_true", help="발화별로 전부 출력")
    ap.add_argument("--audition", action="store_true",
                    help="전량 생성 대신 배역별 시청 대사 한 줄씩만 만든다 (기본 출력: <repo>/tts_audition)")
    ap.add_argument("--audition-voices",
                    help="같은 대사를 여러 음성으로 만든다. 쉼표로 구분한 음성 이름, 또는 'all'")
    ap.add_argument("--audition-line", help="시청에 쓸 발화 id (기본: AUDITION_IDS)")
    ap.add_argument("--list-voices", action="store_true",
                    help="이 공급자의 한국어 후보 음성을 출력하고 끝낸다")
    args = ap.parse_args(argv)

    provider = PROVIDERS[args.provider]

    if args.list_voices:
        _force_utf8_console()
        return list_voices(provider)

    data = load_script(args.script)

    voice_map = dict(provider.default_voices)
    voice_map.update(load_json_map(args.voice_map, "--voice-map"))
    style_map = load_json_map(args.style_map, "--style-map")
    if provider.name == "gemini":
        # 배역별 연기 지시를 기본으로 깔고, --style-map 이 있으면 그것이 이긴다.
        for vid, direction in GEMINI_DIRECTION.items():
            entry = dict(style_map.get(vid) or {})
            entry.setdefault("direction", direction)
            style_map[vid] = entry

    if args.audition or args.audition_voices:
        args.audition = True
        # 시청 파일은 Assets 밖에 떨군다 — Unity가 임포트하면 안 된다
        out_dir = args.out_dir if args.out_dir != DEFAULT_OUT_DIR else AUDITION_OUT_DIR
        jobs = build_audition_jobs(data, provider, voice_map, style_map, out_dir, args)
    else:
        out_dir = args.out_dir
        jobs = build_jobs(data, provider, voice_map, style_map, out_dir, args)

    existing = 0
    if not args.force:
        kept = []
        for j in jobs:
            if j.out_path.is_file() and j.out_path.stat().st_size > 0:
                existing += 1
            else:
                kept.append(j)
        jobs = kept

    summary = report(jobs, provider, out_dir, existing, args)

    if args.clip_map:
        clips = {u["id"]: f"{RESOURCES_PREFIX}/{u['id']}" for u in data["utterances"]}
        if not args.dry_run:
            args.clip_map.parent.mkdir(parents=True, exist_ok=True)
            args.clip_map.write_text(json.dumps(clips, ensure_ascii=False, indent=2), encoding="utf-8")
            print(f"\nclip 대응표 → {args.clip_map}")
        else:
            print(f"\n[dry-run] clip 대응표 {len(clips)}줄을 {args.clip_map}에 쓸 예정")

    if args.dry_run:
        print("\n[dry-run] 네트워크 호출도 파일 쓰기도 하지 않았다.")
        if args.report:
            args.report.parent.mkdir(parents=True, exist_ok=True)
            args.report.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
            print(f"리포트 → {args.report}")
        return 0

    if not jobs:
        print("\n할 일이 없다.")
        return 0

    creds = _require_env(provider.env)          # 키가 없으면 여기서 멈춘다
    out_dir.mkdir(parents=True, exist_ok=True)

    ok, failed = 0, []
    for i, j in enumerate(jobs, 1):
        try:
            audio = provider.synth(creds, j.text, j.provider_voice, j.style)   # ← 유일한 호출 지점
            if not audio:
                raise RuntimeError("빈 응답")
            j.out_path.write_bytes(audio)
            ok += 1
            print(f"[{i}/{len(jobs)}] {j.uid} {j.voice_id} {len(audio):>7,}B  → {j.out_path.name}")
        except Exception as e:                                   # noqa: BLE001
            failed.append(j.uid)
            print(f"[{i}/{len(jobs)}] {j.uid} 실패: {_redact(str(e))}", file=sys.stderr)

    print(f"\n완료 {ok} / 실패 {len(failed)}")
    if failed:
        print("실패한 발화: " + ", ".join(failed), file=sys.stderr)
    if args.audition:
        print(f"들어 볼 것: {out_dir}")
        print("마음에 드는 음성을 정했으면 --voice-map JSON에 적어서 전량 생성으로 넘어간다.")
    else:
        print("Unity에서 Resources 폴더를 다시 임포트하고, 새로 생긴 .meta를 커밋해 GUID를 고정할 것.")

    summary["written"] = ok
    summary["failed"] = failed
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"리포트 → {args.report}")

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
