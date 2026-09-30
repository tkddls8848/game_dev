using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FarmErosion.Sim;

// 구역 H — 끝난 뒤. DIRECTION.md §2-7 의 네 박자를 그대로 옮긴다.
//
//   1. 마지막 칸이 무너진다 — **아무 일도 일어나지 않는다.** HUD 는 그대로 돈다
//   2. 한참 뒤, 매일 메시지가 뜨던 **같은 자리에** 기록이 지나간다. 사실이 먼저, 질문이 뒤
//   3. 맨 마지막에 한 번만 묻는다 — 게임 전체에서 글을 쓰는 자리는 여기뿐이다
//   4. 다른 사람들이 남긴 말이 한 줄씩 올라온다. 그리고 끝. 아무 말도 덧붙이지 않는다
//
// **하지 않는 것**을 코드로 못박는다: 화면을 어둡게 하지 않고, 음악을 바꾸지 않고,
// 글자를 키우지 않는다. `GAME OVER` 도 점수도 등급도 "다시 도전하세요"도 없다.
// 담담한 문장 위에서 질문이 가장 세게 걸린다(§0-1).
//
// 문장은 여기서 만들지 않는다 — 전부 Chronicle(순수 C#)이 굽고 검사기가 읽는다.
// 여기는 **언제 어디에 띄울지**만 안다.
public sealed partial class LowpolyGame
{
    enum EndPhase { None, Hold, Records, Ask, Others, Done }

    // 초 단위. 매일 메시지가 뜨고 사라지던 속도와 같은 결로 둔다.
    const float EndHold = 6.0f;      // **아무 일도 일어나지 않는 시간.** 이것이 1번 박자 전부다
    const float EndFade = 1.1f;      // 한 기록이 뜨고 지는 데 드는 시간
    const float EndRead = 4.2f;      // 읽는 시간
    const float EndAskIn = 2.6f;     // 기록 안에서 질문이 사실보다 늦게 뜬다
    const float EndGap = 0.9f;       // 기록 사이의 빈 시간
    const float EndOtherStep = 2.6f; // 다른 사람의 말이 한 줄씩 올라오는 간격
    const float AskY = 668f;         // 섬 아래 어두운 물. 가운데는 집과 절벽이 있어 글씨가 죽는다

    EndPhase endPhase = EndPhase.None;
    float endTime;
    int endIndex;                    // 지금 보여 주는 기록
    List<Memory> endMemories;
    List<LedgerLine> endOthers;      // **내 이번 회차의 말은 여기 없다.** 열 때 읽은 것이다
    string endAnswer = "";
    bool endAnswered;
    GUIStyle endAskStyle, endFieldStyle, endOtherStyle, endCtxStyle, endHintStyle;

    /// <summary>엔딩이 글자를 받는 동안에는 R·Esc·Space 가 게임을 건드리면 안 된다.</summary>
    public bool EndingCapturesKeys => endPhase == EndPhase.Ask && !endAnswered;

    /// <summary>
    /// **1번 박자에서는 아무것도 숨기지 않는다.** HUD 가 그대로 도는 것이 §2-4 의 "쓸려가버렸다"다.
    /// 기록이 시작되면 버튼과 매일 메시지를 치운다 — 같은 자리에 그릴 것이고, 무엇보다
    /// "다시 시작" 이 화면에서 가장 밝은 것으로 남아 있으면 "다시 도전하세요"가 된다(§2-4).
    /// </summary>
    bool EndingHidesActions => endPhase == EndPhase.Records || EndingHidesAll;

    /// <summary>질문부터는 화면을 비운다. 어둡게 하는 것이 아니라 **덜어 내는** 것이다(§2-7 시안 3·4번 박자).</summary>
    bool EndingHidesAll => endPhase == EndPhase.Ask || endPhase == EndPhase.Others || endPhase == EndPhase.Done;

    static string LedgerPath => Path.Combine(Application.persistentDataPath, "ledger.json");

    void TickEnding()
    {
        if (!farm || sim == null) return;

        if (endPhase == EndPhase.None)
        {
            if (!sim.Ended) return;
            // 시작하는 순간 굽는다. 이 뒤로 시뮬레이션은 더 돌지 않는다.
            endMemories = Chronicle.Build(data, sim, trip);
            endOthers = ReadLedger();
            endPhase = EndPhase.Hold; endTime = 0f; endIndex = 0;
            endAnswer = ""; endAnswered = false;
            return;
        }
        if (endPhase == EndPhase.Done) return;

        endTime += Time.deltaTime;
        switch (endPhase)
        {
            case EndPhase.Hold:
                // 건너뛸 수 없다. **여기서 아무 일도 일어나지 않는 것**이 §2-4 의 "쓸려가버렸다"다.
                if (endTime >= EndHold) { endPhase = EndPhase.Records; endTime = 0f; }
                break;

            case EndPhase.Records:
                if (endTime >= RecordLength(endIndex))
                {
                    endTime = 0f; endIndex++;
                    if (endIndex >= endMemories.Count) endPhase = EndPhase.Ask;
                }
                break;

            case EndPhase.Ask:
                if (!endAnswered && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                    SubmitAnswer();
                else if (endAnswered && endTime >= EndFade) { endPhase = EndPhase.Others; endTime = 0f; }
                break;

            case EndPhase.Others:
                // 줄이 하나도 없으면 그대로 끝난다. **수를 채우려고 지어내지 않는다**(§2-7).
                if (endTime >= EndOtherStep * (endOthers.Count + 2)) endPhase = EndPhase.Done;
                break;
        }
    }

    float RecordLength(int i)
    {
        if (endMemories == null || i >= endMemories.Count) return 0f;
        bool asks = endMemories[i].QuestionKo != null;
        return EndFade + EndRead + (asks ? EndAskIn : 0f) + EndFade + EndGap;
    }

    void SubmitAnswer()
    {
        endAnswered = true; endTime = 0f;
        string clean = Ledger.Clean(endAnswer);
        if (clean == null) return;                       // 비워 두어도 된다. 빈 줄은 남기지 않는다
        try
        {
            string before = File.Exists(LedgerPath) ? File.ReadAllText(LedgerPath) : null;
            File.WriteAllText(LedgerPath, Ledger.Append(before, clean, Chronicle.SurvivedKo(data, sim)));
        }
        catch (IOException) { }                          // 못 써도 엔딩은 성립해야 한다
    }

    List<LedgerLine> ReadLedger()
    {
        try
        {
            string json = File.Exists(LedgerPath) ? File.ReadAllText(LedgerPath) : null;
            return Ledger.Recent(Ledger.Parse(json), Ledger.ShowCount);
        }
        catch (IOException) { return new List<LedgerLine>(); }
    }

    void EndingUI(float w)
    {
        if (endPhase == EndPhase.None || endPhase == EndPhase.Hold) return;
        if (endAskStyle == null)
        {
            endAskStyle = new GUIStyle { font = font, fontSize = 30, alignment = TextAnchor.MiddleCenter };
            // **상자가 아니라 밑줄 하나다.** GUI.skin.textField 를 복사하면 normal 만 비워도
            // hover·active·focused·onNormal 의 상자가 남아 화면 가운데에 검은 사각형이 뜬다.
            endFieldStyle = new GUIStyle(GUI.skin.textField) { font = font, fontSize = 22, alignment = TextAnchor.MiddleCenter };
            foreach (var st in new[] { endFieldStyle.normal, endFieldStyle.hover, endFieldStyle.active,
                                       endFieldStyle.focused, endFieldStyle.onNormal, endFieldStyle.onHover,
                                       endFieldStyle.onActive, endFieldStyle.onFocused })
            { st.background = null; st.textColor = cream; }
            endFieldStyle.border = new RectOffset(); endFieldStyle.padding = new RectOffset(6, 6, 4, 4);
            endOtherStyle = new GUIStyle { font = font, fontSize = 16, alignment = TextAnchor.MiddleLeft };
            endCtxStyle = new GUIStyle { font = font, fontSize = 13, alignment = TextAnchor.MiddleRight };
            endHintStyle = new GUIStyle { font = font, fontSize = 15, alignment = TextAnchor.MiddleCenter };
        }

        if (endPhase == EndPhase.Records) RecordUI(w);
        if (endPhase == EndPhase.Ask) AskUI(w);
        if (endPhase == EndPhase.Others || endPhase == EndPhase.Done) OthersUI(w);
    }

    /// <summary>
    /// 매일 메시지가 뜨던 **바로 그 자리, 그 글자 크기**다. 키우지 않는다 —
    /// 키우는 순간 게임 밖의 말이 되고, 그러면 §2-7 이 경고한 "싸구려"가 된다.
    /// </summary>
    void RecordUI(float w)
    {
        if (endMemories == null || endIndex >= endMemories.Count) return;
        var m = endMemories[endIndex];
        float len = RecordLength(endIndex);
        bool asks = m.QuestionKo != null;
        float bodyA = Ramp(endTime, 0f, EndFade, EndRead + (asks ? EndAskIn : 0f), EndFade);
        if (bodyA <= .003f) return;

        // **좌표를 여기서 정하지 않는다.** 매일 메시지가 뜨던 바로 그 자리여야 하므로
        // Hud 의 상수를 그대로 쓴다. 두 벌로 두면 한쪽만 옮겨져 어긋난다(DIRECTION §2-7).
        const float bx = ToastX, bw = ToastW;
        float bh = Mathf.Max(56, toastText.CalcHeight(new GUIContent(m.BodyKo), bw - 100) + 24);
        float qh = asks ? 44 : 0;
        float y = ToastBottom - bh - qh;

        GUI.color = new Color(1, 1, 1, bodyA);
        Card(bx, y, bw, bh);
        // 부호는 경고(!)가 아니라 점이다. 끝난 뒤에 경고할 것은 없다.
        Label(bx + 22, y + 7, 24, bh - 14, "·", toastText, new Color(.78f, .84f, .87f, bodyA));
        Label(bx + 54, y + 7, bw - 100, bh - 14, m.BodyKo, toastText, new Color(.874f, .906f, .918f, bodyA));
        GUI.color = Color.white;

        if (!asks) return;
        // **사실 다음에 질문이다.** 순서가 바뀌면 심문이 된다(§2-7).
        float askA = Ramp(endTime, EndFade + EndRead, EndAskIn * .5f, EndAskIn * .5f, EndFade);
        if (askA > .003f)
            Label(bx + 54, y + bh + 6, bw - 100, 34, m.QuestionKo, toastText, new Color(.812f, .851f, .871f, askA));
    }

    /// <summary>게임 전체에서 플레이어가 글을 쓰는 자리는 여기 하나뿐이다(§2-7).</summary>
    void AskUI(float w)
    {
        float a = endAnswered ? 1f - Mathf.Clamp01(endTime / EndFade) : Mathf.Clamp01(endTime / EndFade);
        if (a <= .003f) return;

        // **막을 덮지 않는다**(§2-7 · Intro 와 같은 규칙). 대신 두 가지로 읽히게 만든다.
        //
        // 하나는 그림자 — HUD 제목이 쓰는 방법과 같다. 다른 하나는 **자리**다.
        // 시안은 이 질문을 화면 한가운데 두는데, 시안의 배경은 칸이 전부 사라진 빈 바다였다.
        // 실제 빌드에서는 섬과 집이 그대로 남아 가운데가 가장 밝고 복잡하다 — 거기에 얹으니
        // 밑줄 아래 안내문이 통째로 안 읽혔다. 그래서 섬 아래 **어두운 물 위로 내린다.**
        Shadowed(0, AskY, w, 44, Chronicle.ClosingQuestionKo, endAskStyle, new Color(.812f, .851f, .871f, a));

        if (endAnswered) return;
        float fx = w / 2 - 280;
        foreach (var st in new[] { endFieldStyle.normal, endFieldStyle.hover, endFieldStyle.active, endFieldStyle.focused })
            st.textColor = new Color(.95f, .92f, .83f, a);
        GUI.SetNextControlName("endingAnswer");
        endAnswer = GUI.TextField(new Rect(fx, AskY + 54, 560, 40), endAnswer, Ledger.MaxChars, endFieldStyle);
        GUI.FocusControl("endingAnswer");
        Panel(fx, AskY + 96, 560, 1, new Color(.62f, .70f, .75f, .52f * a));
        // 권유하지 않는다. 비워 두어도 된다는 것만 말한다.
        Shadowed(fx, AskY + 110, 560, 26, Chronicle.ClosingHintKo, endHintStyle, new Color(.70f, .77f, .81f, a));
    }

    /// <summary>제자리에 짙은 그림자를 먼저 깔고 글자를 얹는다. 바다 위에서 얇은 글씨가 읽히는 유일한 방법이다.</summary>
    void Shadowed(float x, float y, float w, float h, string value, GUIStyle style, Color color)
    {
        style.normal.textColor = new Color(.02f, .035f, .05f, color.a * .85f);
        GUI.Label(new Rect(x + 1, y + 2, w, h), value, style);
        style.normal.textColor = color;
        GUI.Label(new Rect(x, y, w, h), value, style);
    }

    /// <summary>
    /// 다른 사람들이 남긴 말. **진짜만 올라온다** — 이 목록은 지난 회차에 실제로 적힌 것이다.
    /// 비어 있으면 아무것도 그리지 않고 그대로 끝난다. 끝에 정리하는 문장도 붙이지 않는다.
    /// </summary>
    void OthersUI(float w)
    {
        if (endOthers == null || endOthers.Count == 0) return;
        const float ow = 840;
        float ox = w / 2 - ow / 2, rows = endOthers.Count;
        float oh = 44 + rows * 34;
        float oy = 842 - oh;

        float panelA = Mathf.Clamp01(endTime / EndFade);
        GUI.color = new Color(1, 1, 1, panelA);
        Card(ox, oy, ow, oh);
        GUI.color = Color.white;

        for (int i = 0; i < endOthers.Count; i++)
        {
            float a = Mathf.Clamp01((endTime - EndOtherStep * i) / EndFade);
            if (a <= .003f) continue;
            // 앞에 올라온 줄부터 옅어진다. 쏟아지지 않고 하나씩 지나간다.
            float dim = 1f - .58f * (endOthers.Count - 1 - i) / Mathf.Max(1, endOthers.Count - 1);
            float ry = oy + 22 + i * 34;
            endOtherStyle.normal.textColor = new Color(.874f, .906f, .918f, a * dim);
            endCtxStyle.normal.textColor = new Color(.498f, .576f, .616f, a * dim);
            GUI.Label(new Rect(ox + 26, ry, ow - 240, 26), endOthers[i].TextKo, endOtherStyle);
            GUI.Label(new Rect(ox + ow - 206, ry, 180, 26), endOthers[i].ContextKo, endCtxStyle);
        }
    }
}
