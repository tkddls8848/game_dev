using UnityEngine;

// 시작 연출. **질문을 던지고 답하지 않는다.**
//
// games/farm-erosion/docs/DIRECTION.md §2-3 의 결정을 화면으로 옮긴 것이다 —
// 세상이 왜 무너지는지 게임은 끝까지 답하지 않는다. 답하는 순간 플레이어가
// "원인을 없애면 되잖아?"를 묻고, **버티는 게임이 해결하는 게임**이 된다.
//
// 그래서 이 연출은 세 가지를 **하지 않는다**:
//   · 설명하지 않는다 — 배경 설정도, 무슨 일이 있었는지도 말하지 않는다
//   · 답하지 않는다 — 질문만 띄우고 그대로 사라진다
//   · 극적으로 만들지 않는다 — 효과음도, 확대도, 어두워짐도 없다
//
// 화면 뒤에서는 이미 게임이 돌고 있다. 막을 덮지 않고 **얇은 장막만 씌운다** —
// 질문하는 동안에도 땅은 그대로 있고, 그 사실이 보여야 한다.
public sealed partial class LowpolyGame
{
    // 초 단위 구간. 합이 전체 길이다.
    const float IntroHold = 0.9f;    // 아무것도 없다. 화면이 먼저 눈에 들어와야 한다
    const float IntroTitle = 1.6f;   // 제목이 뜬다
    const float IntroGap = 1.1f;     // 제목만 있는 시간
    const float IntroAsk = 2.2f;     // 질문이 뜬다
    const float IntroLinger = 2.6f;  // **답이 오지 않는 시간.** 이 침묵이 연출의 전부다
    const float IntroFade = 1.4f;    // 둘 다 사라진다

    float introTime = -1f;           // -1 이면 끝났거나 시작 안 한 것
    GUIStyle introTitleStyle, introAskStyle;

    void BeginIntro() { introTime = 0f; }

    bool IntroRunning => introTime >= 0f;

    void TickIntro()
    {
        if (introTime < 0f) return;
        introTime += Time.deltaTime;
        // 아무 키·클릭으로 건너뛴다. 두 번째 플레이부터는 방해가 된다.
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0)) introTime = -1f;
        else if (introTime > IntroHold + IntroTitle + IntroGap + IntroAsk + IntroLinger + IntroFade) introTime = -1f;
    }

    /// <summary>0..1 로 오르내리는 알파. 구간 밖이면 0 이다.</summary>
    static float Ramp(float t, float start, float rise, float hold, float fall)
    {
        if (t < start) return 0f;
        if (t < start + rise) return (t - start) / rise;
        if (t < start + rise + hold) return 1f;
        if (t < start + rise + hold + fall) return 1f - (t - start - rise - hold) / fall;
        return 0f;
    }

    void IntroUI(float w)
    {
        if (introTime < 0f) return;
        if (introTitleStyle == null)
        {
            introTitleStyle = new GUIStyle { font = font, fontSize = 64, alignment = TextAnchor.MiddleCenter };
            introAskStyle = new GUIStyle { font = font, fontSize = 30, alignment = TextAnchor.MiddleCenter };
        }

        float t = introTime;
        float titleA = Ramp(t, IntroHold, IntroTitle, IntroGap + IntroAsk + IntroLinger, IntroFade);
        float askA = Ramp(t, IntroHold + IntroTitle + IntroGap, IntroAsk, IntroLinger, IntroFade);
        float veilA = Mathf.Max(titleA, askA) * .46f;

        // 장막은 얇다. 막을 덮으면 "화면 전환"이 되고, 그러면 질문이 게임 밖의 말이 된다.
        // 질문하는 동안에도 땅이 보여야 한다.
        if (veilA > 0.001f)
        {
            GUI.color = new Color(.055f, .075f, .095f, veilA);
            GUI.DrawTexture(new Rect(0, 0, w, 900), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (titleA > 0.001f)
        {
            introTitleStyle.normal.textColor = new Color(.94f, .93f, .88f, titleA);
            GUI.Label(new Rect(0, 318, w, 80), "밭이 줄어드는 세계", introTitleStyle);
        }

        if (askA > 0.001f)
        {
            // **답하지 않는다.** 이 줄 뒤에 아무것도 오지 않는다.
            introAskStyle.normal.textColor = new Color(.80f, .84f, .86f, askA * .92f);
            GUI.Label(new Rect(0, 424, w, 44), "왜 이 세상은 무너지고 있을까?", introAskStyle);
        }
    }
}
