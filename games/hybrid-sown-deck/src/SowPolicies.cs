// 겨울이 끝날 때, 한 번도 놓지 않은 표본 가운데 무엇을 밭에 심을 것인가.
//
// **이 파일이 덱 → 밭 경로의 결정이다.** 밭칸이 한정돼 있으니 전부는 못 심는다.
// 심은 칸은 다음 봄에 묶이므로, 많이 심으면 새로 기를 자리가 없어진다.
using System.Collections.Generic;

namespace HybridSownDeck
{
    public interface ISowPolicy
    {
        /// <summary>몇 칸을 심기에 쓸 것인가. 밭칸 수와 데이터 상한 중 작은 쪽으로 깎인다.</summary>
        int PlotsWanted { get; }
        List<Specimen> Choose(GameData d, List<Specimen> unplayed, int plotsAvailable);
    }

    /// <summary>이득이 큰 표본부터 칸이 차는 데까지 심는다. 결정적이다.</summary>
    public sealed class GreedySowPolicy : ISowPolicy
    {
        readonly int _plots;
        public GreedySowPolicy(int plots) { _plots = plots; }
        public int PlotsWanted => _plots;

        public List<Specimen> Choose(GameData d, List<Specimen> unplayed, int plotsAvailable)
        {
            var picked = new List<Specimen>();
            if (plotsAvailable <= 0) return picked;
            foreach (var s in CardValue.RankForSowing(d, unplayed))
            {
                if (picked.Count >= plotsAvailable) break;
                // 이득이 음수면 심지 않는다. 0 은 심는다 —
                // 갈라지는 표본은 값이 본전이지만 **씨앗을 한 알도 안 쓰고** 장수를 늘린다.
                // 씨앗이 마른 해에는 그것이 유일한 농사다.
                if (CardValue.SowGain(d, d.Card(s.CardId)) < 0) break;
                picked.Add(s);
            }
            return picked;
        }
    }

    /// <summary>이득을 따지지 않고 무엇이든 칸이 차는 데까지 심는다. 극단을 재는 기준선이다.</summary>
    public sealed class SowEverythingPolicy : ISowPolicy
    {
        readonly int _plots;
        public SowEverythingPolicy(int plots) { _plots = plots; }
        public int PlotsWanted => _plots;

        public List<Specimen> Choose(GameData d, List<Specimen> unplayed, int plotsAvailable)
        {
            var picked = new List<Specimen>();
            foreach (var s in CardValue.RankForSowing(d, unplayed))
            {
                if (picked.Count >= plotsAvailable) break;
                picked.Add(s);
            }
            return picked;
        }
    }

    /// <summary>아무것도 심지 않는다. 덱 → 밭 경로를 **끊은 세계**가 쓰는 정책이다.</summary>
    public sealed class NoSowPolicy : ISowPolicy
    {
        public int PlotsWanted => 0;
        public List<Specimen> Choose(GameData d, List<Specimen> unplayed, int plotsAvailable)
            => new List<Specimen>();
    }
}
