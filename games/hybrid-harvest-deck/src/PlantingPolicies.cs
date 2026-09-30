// 심기 정책. 검사기가 밭 쪽에서 쓰는 축이다.
// "이 카드를 뽑고 싶으면 그 작물을 심어야 한다"가 성립하는지 보려면
// 작물 선택을 바꿔 가며 밤의 결과를 재는 수밖에 없다.
using System.Collections.Generic;

namespace HybridHarvestDeck
{
    /// <summary>목표 덱 구성에 맞춰 가장 모자란 카드를 내는 작물을 심는다. 결정적이다.</summary>
    public sealed class BalancedPlantingPolicy : IPlantingPolicy
    {
        // 카드별 목표 비율(백분율 정수). 합이 100 이다.
        readonly Dictionary<string, int> _target;

        public BalancedPlantingPolicy(Dictionary<string, int> target = null)
        {
            _target = target ?? new Dictionary<string, int>
            {
                { "card_slash",   30 },
                { "card_bulwark", 22 },
                { "card_ember",   18 },
                { "card_sort",    15 },
                { "card_harvest", 15 },
            };
        }

        public PlantDecision Choose(PlantView v)
        {
            int total = 0;
            foreach (var kv in v.DeckProjected) total += kv.Value;

            CropDef best = null;
            int bestGap = int.MinValue;
            foreach (var crop in v.Data.Crops)
            {
                if (!v.CanPlant(crop)) continue;
                int have = v.DeckProjected.TryGetValue(crop.cardId, out var n) ? n : 0;
                int want = _target.TryGetValue(crop.cardId, out var t) ? t : 0;
                // 목표 장수(백분율 정수) - 가진 장수. 100배로 비교해 정수만 쓴다
                int gap = want * (total + 4) - have * 100;
                if (gap > bestGap || (gap == bestGap && best != null && crop.sortOrder < best.sortOrder))
                {
                    bestGap = gap; best = crop;
                }
            }
            if (best == null) return PlantDecision.None;
            return PlantDecision.Plant(best.id, v.FertilizerLeft > 0 && best.seedCost >= 2);
        }
    }

    /// <summary>아무 작물이나. WinRateBand 의 아래쪽 기준선이 쓰는 밭이다.</summary>
    public sealed class RandomPlantingPolicy : IPlantingPolicy
    {
        readonly Rng _rng;
        public RandomPlantingPolicy(Rng rng) { _rng = rng; }

        public PlantDecision Choose(PlantView v)
        {
            var ok = new List<CropDef>();
            foreach (var crop in v.Data.Crops) if (v.CanPlant(crop)) ok.Add(crop);
            if (ok.Count == 0) return PlantDecision.None;
            return PlantDecision.Plant(ok[_rng.Next(ok.Count)].id);
        }
    }

    /// <summary>한 작물만 심는다. DominanceChecker 가 쓴다.</summary>
    public sealed class MonoCropPlantingPolicy : IPlantingPolicy
    {
        readonly string _cropId;
        public MonoCropPlantingPolicy(string cropId) { _cropId = cropId; }

        public PlantDecision Choose(PlantView v)
        {
            var crop = v.Data.Crop(_cropId);
            return v.CanPlant(crop) ? PlantDecision.Plant(_cropId) : PlantDecision.None;
        }
    }

    /// <summary>작물 후보를 좁혀서 안쪽 정책에 넘긴다. SeasonFeasibility · DeadCrop 이 쓴다.</summary>
    public sealed class RestrictedPlantingPolicy : IPlantingPolicy
    {
        readonly IPlantingPolicy _inner;
        readonly HashSet<string> _allowed;
        GameData _cachedSource;
        GameData _cachedView;

        public RestrictedPlantingPolicy(IPlantingPolicy inner, IEnumerable<string> allowedCropIds)
        {
            _inner = inner;
            _allowed = new HashSet<string>(allowedCropIds);
        }

        public PlantDecision Choose(PlantView v)
        {
            if (!ReferenceEquals(_cachedSource, v.Data))
            {
                _cachedSource = v.Data;
                _cachedView = v.Data.WithCropSubset(_allowed);
            }

            var filtered = new PlantView
            {
                Data = _cachedView,
                Season = v.Season,
                Turn = v.Turn,
                TurnsLeftInSeason = v.TurnsLeftInSeason,
                Plot = v.Plot,
                SeedsLeft = v.SeedsLeft,
                SeedsUsableThisSeason = v.SeedsUsableThisSeason,
                WaterLeft = v.WaterLeft,
                FertilizerLeft = v.FertilizerLeft,
                DeckProjected = v.DeckProjected
            };
            var d = _inner.Choose(filtered);
            if (d == null || !_allowed.Contains(d.CropId)) return PlantDecision.None;
            return d;
        }
    }
}
