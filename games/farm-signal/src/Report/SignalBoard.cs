using System;
using System.Collections.Generic;
using System.Text;
using FarmSignal.Data;
using FarmSignal.Sim;

namespace FarmSignal.Report
{
    public sealed class BoardSeason
    {
        public int SeasonNumber, Year;
        public string SeasonId, SeasonNameKo;
        public string[] PlotCrop;            // plots[] 배열 순서. null = 빈 칸
        public int[] PlotEffectiveExposure;  // plots[] 배열 순서
        public int SkippedBySoil;            // 계획이 지시했지만 토질이 모자라 못 심은 칸 수
        public int[] ObservedIndex;          // 작물별 하루 평균 관측치
        public int[] PriceBefore, PriceAfter;
        public int RenownBefore, RenownAfter, Visitors, PremiumPercent;
        public int TheftRiskPer1000, TheftRoll, TheftPlotsHit;
        public bool TheftStruck;
        public List<string> TheftTargets = new List<string>();
        public int FogDays;
        public int[] RevenueByCrop;
        public int RevenueTotal;
        public int ObservedTotal;
    }

    /// showcase.json 의 고정 계획 하나를 계절 단위로 돌려 **시세판**을 뽑는다.
    ///
    /// 전체 시뮬레이션(Simulation)과 다른 물건이다. 이쪽은 밭이 계절 내내 고정이고
    /// 물·성장·수확을 셈하지 않는다 — "남이 보고 무엇을 했나"만 남긴다.
    /// **presentation/index.html 이 이 계산을 JS로 이식해 같은 판을 그린다.**
    /// 그래서 여기에는 SignalMath 밖의 수가 들어오면 안 된다.
    public sealed class SignalBoard
    {
        public string PlanId, PlanNameKo, PlanNoteKo;
        public bool SignalOn;
        public int Years;
        public List<BoardSeason> Seasons = new List<BoardSeason>();
        public int GrandRevenue;

        public static SignalBoard Compute(GameData d, PlanDef plan, bool signalOn, int years)
        {
            var rules = signalOn ? SignalRules.Full(d) : SignalRules.Off(d);
            var state = new SignalState(d, rules);
            var weather = new WeatherCalendar(d, years);
            var prices = new PriceBook(d, years);

            var board = new SignalBoard
            {
                PlanId = plan.id, PlanNameKo = plan.nameKo, PlanNoteKo = plan.noteKo,
                SignalOn = signalOn, Years = years,
            };

            var order = PlotsByExposureDesc(d);
            int daysPerSeason = d.Seasons.daysPerSeason;
            int nPlots = d.Plots.plots.Length;
            var revenueRemainder = new int[d.CropCount];

            for (int s = 0; s < years * d.SeasonsPerYear; s++)
            {
                var season = d.Seasons.seasons[s % d.SeasonsPerYear];
                int seasonStartDay = s * daysPerSeason;
                var row = new BoardSeason
                {
                    SeasonNumber = s + 1,
                    Year = s / d.SeasonsPerYear + 1,
                    SeasonId = season.id,
                    SeasonNameKo = season.nameKo,
                    PlotCrop = new string[nPlots],
                    PlotEffectiveExposure = new int[nPlots],
                    ObservedIndex = new int[d.CropCount],
                    PriceBefore = new int[d.CropCount],
                    PriceAfter = new int[d.CropCount],
                    RevenueByCrop = new int[d.CropCount],
                    RenownBefore = state.Renown,
                    Visitors = state.Visitors,
                    PremiumPercent = state.PremiumPercent,
                };

                // 1. 계획을 칸에 펼친다. 길가부터 cropOrder 를 순서대로 놓는다.
                var cropOrder = OrderFor(plan, season.id);
                var assign = new Dictionary<string, string>();
                for (int i = 0; i < order.Count; i++)
                {
                    string cropId = (cropOrder != null && i < cropOrder.Length) ? cropOrder[i] : "";
                    if (string.IsNullOrEmpty(cropId)) continue;
                    var crop = d.Crop(cropId);
                    if (crop == null) continue;
                    if (order[i].soil < crop.minSoil) { row.SkippedBySoil++; continue; }
                    assign[order[i].id] = cropId;
                }
                for (int i = 0; i < nPlots; i++)
                    row.PlotCrop[i] = assign.TryGetValue(d.Plots.plots[i].id, out var c) ? c : null;

                // 2. 가림을 셈한 유효 노출
                for (int i = 0; i < nPlots; i++)
                    row.PlotEffectiveExposure[i] = EffExposure(d, d.Plots.plots[i], assign);

                // 3. 계절 내내 날마다 관측한다. 순회는 날짜 -> plots[] 순서
                for (int dd = 0; dd < daysPerSeason; dd++)
                {
                    var w = weather.At(seasonStartDay + dd);
                    if (w.id == "fog") { state.CountFogDay(); row.FogDays++; }
                    for (int i = 0; i < nPlots; i++)
                    {
                        if (row.PlotCrop[i] == null) continue;
                        state.Observe(row.PlotCrop[i], row.PlotEffectiveExposure[i],
                                      w.exposurePercent, season.roadTrafficPercent);
                    }
                }

                // 4. 이 계절에 걷었다고 볼 때의 매출 (계절당 한 번. 시세지수는 마감 전 값)
                for (int c = 0; c < d.CropCount; c++)
                {
                    row.PriceBefore[c] = state.PriceIndexPercent[c];
                    var crop = d.Crops.crops[c];
                    int plotsWith = 0;
                    for (int i = 0; i < nPlots; i++) if (row.PlotCrop[i] == crop.id) plotsWith++;
                    if (plotsWith == 0) continue;
                    int baseNoisy = prices.BaseUnit(crop.id, seasonStartDay);
                    int unit = SignalMath.SellUnit(baseNoisy, state.PriceIndexPercent[c],
                                                  state.PremiumPercent, ref revenueRemainder[c]);
                    row.RevenueByCrop[c] = plotsWith * crop.yieldUnits * unit;
                    row.RevenueTotal += row.RevenueByCrop[c];
                }
                board.GrandRevenue += row.RevenueTotal;

                // 5. 남이 반응한다
                var candidates = new List<TheftCandidate>();
                for (int i = 0; i < nPlots; i++)
                {
                    if (row.PlotCrop[i] == null) continue;
                    candidates.Add(new TheftCandidate
                    {
                        PlotId = d.Plots.plots[i].id, CropId = row.PlotCrop[i],
                        EffectiveExposure = row.PlotEffectiveExposure[i],
                        Appeal = d.Crop(row.PlotCrop[i]).theftAppeal,
                    });
                }
                var rec = state.CloseSeason(row.SeasonNumber, row.Year, season.id, candidates);
                Array.Copy(rec.ObservedIndex, row.ObservedIndex, d.CropCount);
                Array.Copy(rec.PriceAfter, row.PriceAfter, d.CropCount);
                row.ObservedTotal = rec.ObservedTotal;
                row.RenownAfter = rec.RenownAfter;
                row.TheftRiskPer1000 = rec.TheftRiskPer1000;
                row.TheftRoll = rec.TheftRoll;
                row.TheftStruck = rec.TheftStruck;
                row.TheftPlotsHit = rec.TheftPlotsHit;
                row.TheftTargets.AddRange(rec.TheftTargets);

                board.Seasons.Add(row);
            }
            return board;
        }

        /// 길가(노출 높은 칸)부터. 동점은 칸 이름으로 갈라 결정적으로 만든다.
        public static List<PlotDef> PlotsByExposureDesc(GameData d)
        {
            var list = new List<PlotDef>(d.Plots.plots);
            list.Sort((a, b) =>
            {
                int c = b.exposure.CompareTo(a.exposure);
                if (c != 0) return c;
                return string.CompareOrdinal(a.id, b.id);
            });
            return list;
        }

        static string[] OrderFor(PlanDef plan, string seasonId)
        {
            foreach (var bs in plan.bySeason) if (bs.seasonId == seasonId) return bs.cropOrder;
            return null;
        }

        static int EffExposure(GameData d, PlotDef p, Dictionary<string, string> assign)
        {
            int depth = Math.Max(1, d.Plots.screenDepth);
            var heights = new int[depth];
            for (int k = 0; k < depth; k++)
            {
                var front = PlotAt(d, p.x, p.y - (k + 1));
                heights[k] = 0;
                if (front != null && assign.TryGetValue(front.id, out var cropId))
                    heights[k] = d.Crop(cropId).screenHeight;
            }
            return SignalMath.EffectiveExposure(d, p.exposure, heights);
        }

        static PlotDef PlotAt(GameData d, int x, int y)
        {
            if (x < 0 || y < 0 || x >= d.Plots.gridWidth || y >= d.Plots.gridHeight) return null;
            foreach (var p in d.Plots.plots) if (p.x == x && p.y == y) return p;
            return null;
        }

        /// 목업이 그리는 것과 같은 표를 글로. 사람이 눈으로 맞춰 볼 수 있어야 이식이 검증된다.
        public string Report(GameData d)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"── SignalBoard · {PlanNameKo} · 신호 {(SignalOn ? "켬" : "끔")} ──────────────");
            sb.Append("계절   ");
            foreach (var c in d.Crops.crops) sb.Append(c.nameKo.PadLeft(6));
            sb.AppendLine("   평판 방문 웃돈  위험  주사위 도둑  매출");
            foreach (var s in Seasons)
            {
                sb.Append($"{s.Year}년{s.SeasonNameKo,-3}");
                for (int c = 0; c < d.CropCount; c++) sb.Append($"{s.PriceBefore[c],6}");
                sb.Append($"  {s.RenownAfter,5}{s.Visitors,4}{s.PremiumPercent,5}%");
                sb.Append($"{s.TheftRiskPer1000,6}{s.TheftRoll,8}");
                sb.Append(s.TheftStruck ? $"  {s.TheftPlotsHit}칸" : "   - ");
                sb.AppendLine($"{s.RevenueTotal,8}");
            }
            sb.AppendLine($"합계 매출 {GrandRevenue}");
            sb.AppendLine("─────────────────────────────────────────────────────────────");
            return sb.ToString();
        }
    }
}
