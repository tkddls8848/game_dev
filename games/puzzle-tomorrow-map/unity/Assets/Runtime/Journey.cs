using System;
using System.Linq;
using UnityEngine;
using FarmErosion.Data;
using FarmErosion.Sim;

// 구역 G — 떠나기. **다른 사람을 만나는 화면**이다.
//
// DIRECTION.md §2-2 가 정한 것을 화면으로 옮긴다: 자급만으로는 못 버티고,
// 이동만으로도 못 버틴다. 그래서 떠나는 것이 보상 화면이 되면 안 된다 —
// **무엇을 얻는지와 함께 무엇을 잃는지가 같은 크기로 보여야** 판단이 된다.
//
// 그래서 이 패널이 늘 함께 보여 주는 것 셋:
//   · 며칠 걸리는가 — 그 며칠 동안 내 땅은 손을 못 쓴 채 깎인다
//   · 그의 밭이 몇 칸 남았는가 — 줄 수 있는 것이 여기 비례한다(§2-1)
//   · 내가 무엇을 내놓는가 — 의뢰는 보상이 아니라 **이전**이다(§4-1)
//
// 규칙은 games/farm-erosion/src/Sim/Travel.cs 가 전부 가진다. 여기는 그리기만 한다.
public sealed partial class LowpolyGame
{
    Travel trip;                     // farm 모드에서만 만든다
    bool journey;                    // 떠나기 패널이 열려 있는가
    string journeyRegion;            // 고른 곳
    string journeyErrand;            // 고른 의뢰. null 이면 그냥 들른다
    string journeyReport = "";       // 다녀온 뒤 그 사람이 한 말과 받은 것

    void OpenJourney()
    {
        journey = !journey;
        seedPicker = false;
        if (journey && journeyRegion == null && data.Travel != null && data.Travel.regions.Length > 0)
            journeyRegion = data.Travel.regions[0].id;
    }

    /// <summary>받을 것 미리 보기. 실제로 주는 것과 같은 식으로 센다(Travel.Scaled).</summary>
    string GiftPreview(RegionDef def)
    {
        var st = trip.Region(def.id);
        var parts = new System.Collections.Generic.List<string>();
        foreach (var g in def.gifts)
        {
            if (g.onlyWhenTheirPlotsAtMost != -1 && st.PlotsLeft > g.onlyWhenTheirPlotsAtMost) continue;
            if (g.once == 1 && st.GiftsTaken.Contains(g.kind)) continue;
            int amount = g.scaled == 1 ? Travel.Scaled(g.baseAmount, g.minAmount, st.PlotsLeft, st.StartPlots) : g.baseAmount;
            parts.Add(ResNameKo(g.kind) + " " + amount);
        }
        return parts.Count == 0 ? "줄 수 있는 것이 없습니다" : string.Join(" · ", parts);
    }

    static string ResNameKo(string kind) => kind == Res.Coin ? "셈"
                                          : kind == Res.Soil ? "흙"
                                          : kind == Res.Wall ? "방벽"
                                          : kind == Res.Seed ? "씨앗"
                                          : kind == Res.Hand ? "일손" : kind;

    /// <summary>내가 내놓는 것. **받는 것보다 먼저 읽혀야 한다** — 의뢰는 이전이지 보상이 아니다.</summary>
    string ErrandCost(ErrandDef e)
        => e.giveDays > 0 ? "손을 " + e.giveDays + "일 보탭니다"
                          : ResNameKo(e.giveKind) + " " + e.giveAmount + "을 내놓습니다";

    void JourneyUI(float w)
    {
        if (!journey || trip == null || data.Travel == null) return;
        const float pw = 1104, ph = 452;
        float px = w / 2 - pw / 2, py = 268;
        Card(px, py, pw, ph);
        Label(px + 28, py + 16, 620, 34, "어디로 가시겠습니까", heading);
        Label(px + 28, py + 52, 700, 26,
            "떠나 있는 동안 내 땅에는 손을 쓸 수 없습니다. 날은 그대로 갑니다.", caption);
        if (Button(px + pw - 132, py + 16, 104, 38, "닫기")) { journey = false; return; }
        Rule(px + 28, py + 86, pw - 56);

        // ── 왼쪽: 사람들 ──────────────────────────────────────────────────
        float lx = px + 28, ly = py + 102, lw = 452;
        for (int i = 0; i < data.Travel.regions.Length; i++)
        {
            var def = data.Travel.regions[i];
            var st = trip.Region(def.id);
            float ry = ly + i * 78;
            bool picked = journeyRegion == def.id;
            var rect = new Rect(lx, ry, lw, 68);
            bool hot = GUI.enabled && rect.Contains(Event.current.mousePosition);
            GUI.Box(rect, GUIContent.none, picked ? btnPick : hot ? btnHot : btnFrame);
            Label(lx + 16, ry + 8, lw - 32, 28, def.nameKo + " · " + def.personKo, picked ? btnTitle : text);
            // **거리와 남은 밭을 같은 줄에 둔다.** 멀수록 많이 주지만 그만큼 내 땅이 깎인다.
            Label(lx + 16, ry + 38, lw - 32, 24,
                "왕복 " + trip.DaysFor(def.id) + "일 · 그의 밭 " + st.PlotsLeft + "칸 · " + GiftPreview(def), caption);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) { journeyRegion = def.id; journeyErrand = null; Sound(); }
        }

        // ── 오른쪽: 고른 사람에게 할 수 있는 것 ─────────────────────────────
        var chosen = data.Region(journeyRegion);
        if (chosen == null) return;
        float rx = px + 512, rw = pw - 540;
        var state = trip.Region(chosen.id);

        // 그가 지금 할 말. 다시 갈수록 짧아진다(§4-1) — 그것이 쇠퇴의 표현이다.
        string line = chosen.linesKo[Mathf.Clamp(state.VisitCount, 0, chosen.linesKo.Length - 1)];
        Label(rx, py + 102, rw, 92, "“" + line + "”", caption);

        Label(rx, py + 202, rw, 26, "부탁받은 일", small);
        Rule(rx, py + 232, rw);

        var errands = trip.ErrandsAt(chosen.id);
        float ey = py + 244;
        // 아무 의뢰도 받지 않는 것도 선택이다. 그냥 들르면 주는 것만 받고 온다.
        if (ErrandRow(rx, ey, rw, 46, journeyErrand == null, "그냥 들른다", "내놓는 것 없이 다녀옵니다"))
            journeyErrand = null;
        for (int i = 0; i < errands.Count && i < 3; i++)
        {
            var e = errands[i];
            bool payable = trip.CanPay(e);
            string give = ErrandCost(e) + (payable ? "" : " · 낼 것이 없습니다");
            if (ErrandRow(rx, ey + 54 + i * 54, rw, 46, journeyErrand == e.id, e.titleKo, give, payable))
                journeyErrand = e.id;
        }

        // ── 떠난다 ────────────────────────────────────────────────────────
        int days = trip.DaysFor(chosen.id, journeyErrand);
        bool canGo = !sim.Ended;
        if (Button(rx, py + ph - 68, rw, 50, "다녀온다   ·   " + days + "일", canGo))
            Depart(chosen.id, journeyErrand);
    }

    bool ErrandRow(float x, float y, float w, float h, bool picked, string title, string sub, bool enabled = true)
    {
        var rect = new Rect(x, y, w, h);
        bool hot = enabled && GUI.enabled && rect.Contains(Event.current.mousePosition);
        GUI.Box(rect, GUIContent.none, !enabled ? btnOff : picked ? btnPick : hot ? btnHot : btnFrame);
        Color tint = !enabled ? new Color(.58f, .64f, .68f, .50f) : picked ? C("f0cd7f") : cream;
        Label(x + 14, y + 3, w * .40f, 24, title, small, tint);
        Label(x + w * .40f + 8, y + 3, w * .60f - 22, 24, sub, caption);
        if (!enabled) return false;
        if (!GUI.Button(rect, GUIContent.none, GUIStyle.none)) return false;
        Sound(); return true;
    }

    /// <summary>
    /// 다녀온다. **날이 실제로 가고 내 땅이 그동안 깎인다** — Travel.Visit 안에서 AdvanceDay 가 돈다.
    /// 그래서 돌아오면 칸이 사라져 있을 수 있고, 그때는 붕괴 연출도 같이 나와야 한다.
    /// </summary>
    void Depart(string regionId, string errandId)
    {
        var held = sim.Plots.Where(p => p.Active).ToArray();
        int before = sim.ActiveCount();
        var def = data.Region(regionId);

        var v = trip.Visit(regionId, errandId);
        if (!v.Ok) { message = v.FailKo; Deny(); return; }

        // 받은 것을 한 줄로. **먼저 무엇을 잃었는지 말하고 그 다음이 받은 것이다.**
        string got = v.ReceivedKinds.Length == 0 ? "빈손으로 돌아왔습니다"
                   : string.Join(" · ", Enumerable.Range(0, v.ReceivedKinds.Length)
                        .Select(i => ResNameKo(v.ReceivedKinds[i]) + " " + v.ReceivedAmounts[i]));
        journeyReport = "“" + v.LineKo + "”";
        message = def.nameKo + "에 다녀왔습니다 · " + v.DaysSpent + "일 · " + got;
        if (v.ReprieveGiven > 0) message += "\n" + (def.personKo.EndsWith("부부") || def.personKo.EndsWith("아이들") ? "그들은 " : "그는 ")
                                          + v.ReprieveGiven + "일을 더 버틸 수 있게 됐습니다.";
        else if (errandId != null) message += "\n더 미뤄 줄 수 있는 날이 남아 있지 않았습니다.";

        journey = false;
        if (sound != null) sound.Advance();
        int lost = before - sim.ActiveCount();
        if (lost > 0)
        {
            message += "\n돌아와 보니 밭 " + lost + "칸이 없었습니다.";
            if (sound != null) sound.Collapse(lost);
        }
        Rebuild();
        CollapseEffects(held.Where(p => !p.Active).Select(PlotPosition));
    }
}
