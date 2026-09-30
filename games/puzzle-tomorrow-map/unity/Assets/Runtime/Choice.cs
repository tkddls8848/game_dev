using UnityEngine;
using FarmErosion.Data;

// 구역 I — 난이도 선택. **이 게임을 처음 설명하는 자리다.**
//
// DIRECTION.md §2-6: `쉬움 / 보통 / 어려움` 은 보통 *이길 확률*을 뜻하는데
// 이 게임에는 이기는 길이 없다. 이름과 실제가 어긋나므로 **여기서 정직하게 말한다** —
// *"어느 쪽을 골라도 끝은 같습니다. 다만 손 쓸 여지가 다릅니다."*
//
// 그래서 §0-1 의 대비(힐링의 외형 · 종말의 주제)가 **이 화면에서 처음 드러난다.**
// 문구는 데이터(difficulty.json)에 있고 검사기가 읽는다 — 화면에서 지어내지 않는다.
//
// 연출은 시작 연출(Intro)과 같은 규칙이다: 막을 덮지 않고 얇은 장막만 씌운다.
// 고르는 동안에도 밭이 보여야 한다.
public sealed partial class LowpolyGame
{
    bool choosing;
    GUIStyle chooseTitle, chooseLine;

    /// <summary>난이도를 고르는 동안에는 밭을 누르거나 날을 넘길 수 없다.</summary>
    public bool Choosing => choosing;

    void BeginChoice() { choosing = true; }

    void ChoiceUI(float w)
    {
        if (!choosing) return;
        if (chooseTitle == null)
        {
            chooseTitle = new GUIStyle { font = font, fontSize = 34, alignment = TextAnchor.MiddleCenter };
            chooseLine = new GUIStyle { font = font, fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }
        var table = data.Difficulty;
        if (table == null || table.levels == null || table.levels.Length == 0) { choosing = false; return; }

        // 장막은 얇다. 막으면 화면 전환이 되고, 그러면 이 말이 게임 밖의 말이 된다.
        GUI.color = new Color(.055f, .075f, .095f, .52f);
        GUI.DrawTexture(new Rect(0, 0, w, 900), Texture2D.whiteTexture);
        GUI.color = Color.white;

        chooseTitle.normal.textColor = new Color(.94f, .93f, .88f, 1f);
        GUI.Label(new Rect(0, 236, w, 46), "어떻게 시작하시겠습니까", chooseTitle);

        // **이 한 줄이 이 화면의 이유다.** 이길 수 있다고 말하지 않는다.
        chooseLine.normal.textColor = new Color(.74f, .79f, .82f, 1f);
        GUI.Label(new Rect(w / 2 - 420, 292, 840, 30), table.honestLineKo, chooseLine);

        int n = table.levels.Length;
        const float cw = 308, ch = 176, gap = 22;
        float x0 = w / 2 - (cw * n + gap * (n - 1)) / 2f;
        for (int i = 0; i < n; i++)
        {
            var lv = table.levels[i];
            float cx = x0 + i * (cw + gap);
            var rect = new Rect(cx, 366, cw, ch);
            bool hot = rect.Contains(Event.current.mousePosition);
            GUI.Box(rect, GUIContent.none, hot ? btnHot : btnFrame);
            Label(cx, 392, cw, 40, lv.nameKo, heading, hot ? C("f0cd7f") : cream);
            Rule(cx + 40, 442, cw - 80);
            // 여지가 어떻게 다른지 한 줄. '더 오래 버틴다'라고 쓰지 않는다(§2-6).
            Label(cx + 22, 462, cw - 44, 74, lv.captionKo, caption);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) Choose(lv.id);
        }
    }

    void Choose(string id)
    {
        Sound();
        difficultyId = id;
        choosing = false;
        StartRun();
        // 고르고 나서야 첫 질문이 뜬다. 순서가 바뀌면 질문이 메뉴에 묻힌다.
        if (farm && !Headless) BeginIntro();
    }
}
