using System;
using System.Collections.Generic;
using FarmSignal.Data;

namespace FarmSignal.Sim
{
    /// 한 계절이 남긴 기록. 목업의 시세판 한 줄이 이것 하나다.
    public sealed class SeasonSignal
    {
        public int SeasonNumber;      // 1부터. 3년이면 1~12
        public int Year;
        public string SeasonId;
        public int[] ObservedIndex;   // 작물별 하루 평균 관측치
        public int[] PriceBefore;     // 이 계절에 팔린 시세지수(%)
        public int[] PriceAfter;      // 다음 계절의 시세지수(%)
        public int RenownBefore, RenownAfter, Visitors, PremiumPercent;
        public int TheftRiskPer1000, TheftRoll;
        public bool TheftStruck;
        public int TheftPlotsHit;
        public List<string> TheftTargets = new List<string>();   // "A1:saffron"
        public int FogDays;
        public int ObservedTotal;
    }

    /// 신호의 상태. 시세지수·평판·이번 계절의 관측 누적.
    /// **관측 누적의 순회 순서가 결과를 정한다** — 반드시 날짜 오름차순 · plots[] 배열 순서로 돈다.
    /// 딕셔너리 순회로 바꾸면 같은 씨드에서 다른 결과가 나오고 SeedDeterminism이 터진다.
    public sealed class SignalState
    {
        readonly GameData _d;
        readonly SignalRules _r;
        readonly Random _rng;

        public readonly int[] PriceIndexPercent;   // 작물별. 100이 기준
        public int Renown;
        public int Visitors;
        public int PremiumPercent;

        readonly int[] _observedPoints;
        readonly int[] _observeRemainder;
        readonly int[] _sellRemainder;
        int _fogDays;

        public readonly List<SeasonSignal> History = new List<SeasonSignal>();

        public int FirstWarnSeason = -1;      // 어느 작물이든 시세지수가 warn 아래로 처음 내려간 계절
        public int FirstCollapseSeason = -1;
        public int FirstTheftSeason = -1;
        public int FirstVisitorSeason = -1;
        public int TheftCount;
        public int TheftPlotsLost;
        public int MaxRenown;
        public int MinPriceIndexSeen = 1000;
        public int MaxPriceIndexSeen;

        public SignalState(GameData d, SignalRules r)
        {
            _d = d;
            _r = r;
            _rng = new Random(d.Config.seed + d.Config.signalSeedOffset);
            PriceIndexPercent = new int[d.CropCount];
            for (int i = 0; i < PriceIndexPercent.Length; i++) PriceIndexPercent[i] = 100;
            _observedPoints = new int[d.CropCount];
            _observeRemainder = new int[d.CropCount];
            _sellRemainder = new int[d.CropCount];
        }

        public int Observed(string cropId)
        {
            int c = _d.CropIndex(cropId);
            return c < 0 ? 0 : _observedPoints[c];
        }

        public int ObservedTotalThisSeason()
        {
            int t = 0;
            for (int c = 0; c < _observedPoints.Length; c++) t += _observedPoints[c];
            return t;
        }

        /// 하루 한 칸의 관측치를 누적한다. 부르는 쪽이 순회 순서를 지킨다.
        public void Observe(string cropId, int effectiveExposure, int weatherExposurePercent, int roadTrafficPercent)
        {
            int c = _d.CropIndex(cropId);
            if (c < 0) return;
            var crop = _d.Crops.crops[c];
            _observedPoints[c] += SignalMath.DailyObservedPoints(
                _r, crop.visibility, effectiveExposure, weatherExposurePercent, roadTrafficPercent,
                ref _observeRemainder[c]);
        }

        public void CountFogDay() => _fogDays++;

        /// 지금 시세로 한 단위를 팔면 얼마인가. 잡음가(PriceBook) 위에 시세지수와 웃돈을 얹는다.
        /// **실제로 팔 때만 부른다** — 나머지를 누적하므로 점수 계산에 부르면 값이 흐른다.
        public int SellUnit(string cropId, int baseNoisyPrice)
        {
            int c = _d.CropIndex(cropId);
            if (c < 0) return 0;
            return SignalMath.SellUnit(baseNoisyPrice, PriceIndexPercent[c], PremiumPercent, ref _sellRemainder[c]);
        }

        /// 점수 계산용. 나머지를 건드리지 않는다.
        public int PeekSellUnit(string cropId, int baseNoisyPrice)
        {
            int c = _d.CropIndex(cropId);
            if (c < 0) return 0;
            int scratch = 0;
            return SignalMath.SellUnit(baseNoisyPrice, PriceIndexPercent[c], PremiumPercent, ref scratch);
        }

        public int PriceIndexOf(string cropId)
        {
            int c = _d.CropIndex(cropId);
            return c < 0 ? 100 : PriceIndexPercent[c];
        }

        /// 계절 마감. 여기서 남이 반응한다.
        /// theftCandidates 는 (칸 이름, 작물 id, 유효노출) 을 plots[] 순서로 받는다.
        /// 돌려주는 목록이 실제로 털린 칸이고, 부르는 쪽이 그 칸의 작물을 없앤다.
        public SeasonSignal CloseSeason(int seasonNumber, int year, string seasonId,
                                        List<TheftCandidate> theftCandidates)
        {
            var rec = new SeasonSignal
            {
                SeasonNumber = seasonNumber,
                Year = year,
                SeasonId = seasonId,
                ObservedIndex = new int[_d.CropCount],
                PriceBefore = new int[_d.CropCount],
                PriceAfter = new int[_d.CropCount],
                RenownBefore = Renown,
                Visitors = Visitors,
                PremiumPercent = PremiumPercent,
                FogDays = _fogDays,
            };

            int daysPerSeason = _d.Seasons.daysPerSeason;
            for (int c = 0; c < _d.CropCount; c++)
            {
                rec.ObservedIndex[c] = _observedPoints[c] / daysPerSeason;
                rec.PriceBefore[c] = PriceIndexPercent[c];
                rec.ObservedTotal += rec.ObservedIndex[c];
            }

            // 1. 도둑 — 평판이 깎아 준 뒤의 위험도로 한 번 굴린다.
            //
            // **주사위는 위험도와 무관하게 계절마다 꼭 한 번 굴린다.** 위험도가 낮을 때 안 굴리면
            // 난수 줄기가 정책마다 어긋나 두 정책이 서로 다른 주사위를 보게 되고,
            // 그러면 SignalMatters 가 비교하는 것이 노출이 아니라 운이 된다. 처음에 그렇게 짰다.
            rec.TheftRiskPer1000 = SignalMath.TheftRiskPer1000(_d, _r, rec.ObservedIndex, Renown);
            rec.TheftRoll = _rng.Next(1000);
            if (rec.TheftRiskPer1000 > 0)
            {
                if (rec.TheftRoll < rec.TheftRiskPer1000)
                {
                    int hits = SignalMath.TheftPlotsHit(_r, rec.TheftRiskPer1000);
                    var picks = PickTheftTargets(theftCandidates, hits);
                    // 걷어 갈 것이 하나도 없으면 든 것으로 세지 않는다 — 셈에 빈 도둑이 섞이면
                    // "도둑이 몇 번 왔나"가 실제 손실과 어긋난다.
                    if (picks.Count > 0)
                    {
                        rec.TheftStruck = true;
                        rec.TheftPlotsHit = picks.Count;
                        foreach (var t in picks) rec.TheftTargets.Add(t.PlotId + ":" + t.CropId);
                        TheftCount++;
                        TheftPlotsLost += picks.Count;
                        if (FirstTheftSeason < 0) FirstTheftSeason = seasonNumber;
                    }
                }
            }

            // 2. 평판 → 방문자 → 웃돈
            Renown = SignalMath.NextRenown(_d, _r, Renown, rec.ObservedIndex);
            Visitors = SignalMath.VisitorCount(_r, Renown);
            PremiumPercent = SignalMath.VisitorPremiumPercent(_r, Visitors);
            rec.RenownAfter = Renown;
            if (Renown > MaxRenown) MaxRenown = Renown;
            if (FirstVisitorSeason < 0 && Visitors > 0) FirstVisitorSeason = seasonNumber;

            // 3. 모방 → 다음 계절 시세
            for (int c = 0; c < _d.CropCount; c++)
            {
                PriceIndexPercent[c] = SignalMath.NextPriceIndex(
                    _r, PriceIndexPercent[c], rec.ObservedIndex[c], _d.Crops.crops[c].copyAppeal);
                rec.PriceAfter[c] = PriceIndexPercent[c];
                if (PriceIndexPercent[c] < MinPriceIndexSeen) MinPriceIndexSeen = PriceIndexPercent[c];
                if (PriceIndexPercent[c] > MaxPriceIndexSeen) MaxPriceIndexSeen = PriceIndexPercent[c];
                if (FirstWarnSeason < 0 && PriceIndexPercent[c] <= _r.PriceWarnPercent) FirstWarnSeason = seasonNumber;
                if (FirstCollapseSeason < 0 && PriceIndexPercent[c] <= _r.PriceCollapsePercent) FirstCollapseSeason = seasonNumber;
            }

            History.Add(rec);
            Array.Clear(_observedPoints, 0, _observedPoints.Length);
            _fogDays = 0;
            return rec;
        }

        /// 길 쪽부터, 노리는 값이 큰 것부터. 동점은 칸 이름으로 갈라 결정적으로 만든다.
        List<TheftCandidate> PickTheftTargets(List<TheftCandidate> candidates, int hits)
        {
            var picked = new List<TheftCandidate>();
            if (candidates == null || candidates.Count == 0) return picked;
            var sorted = new List<TheftCandidate>(candidates);
            sorted.Sort((a, b) =>
            {
                int va = a.Appeal * a.EffectiveExposure, vb = b.Appeal * b.EffectiveExposure;
                int c = vb.CompareTo(va);
                if (c != 0) return c;
                return string.CompareOrdinal(a.PlotId, b.PlotId);
            });
            for (int i = 0; i < sorted.Count && picked.Count < hits; i++)
                if (sorted[i].Appeal > 0) picked.Add(sorted[i]);
            return picked;
        }
    }

    public struct TheftCandidate
    {
        public string PlotId;
        public string CropId;
        public int EffectiveExposure;
        public int Appeal;
    }
}
