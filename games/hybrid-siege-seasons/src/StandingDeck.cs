// 영속 덱. 이 PoC 를 hybrid-harvest-deck 과 가르는 자리다.
//
// 덱은 한 판이 아니라 **해를 넘겨** 남는다. 카드마다 남은 횟수가 있고, 다 쓰면 영영 사라진다.
// 그래서 "강한 카드를 아낀다"가 전략이 되고, 동시에 "아끼기만 하면 밭이 썩는다"가 위험이 된다.
using System.Collections.Generic;

namespace HybridSiegeSeasons
{
    /// <summary>덱에 실제로 놓여 있는 카드 한 장. 남은 횟수를 스스로 들고 있다.</summary>
    public sealed class CardInstance
    {
        public string CardId;
        public int Uses;      // 남은 횟수. 0 이면 부서졌다
        public int MaxUses;
        /// <summary>몇 년차 수확에서 왔는가. 진 해의 수확을 도로 빼앗을 때 쓴다.</summary>
        public int FromYear;
        /// <summary>어느 계절의 수확인가. 한 계절만 털릴 때 쓴다.</summary>
        public string FromSeasonId;

        public bool Broken => Uses <= 0;
        public int WornPct => MaxUses <= 0 ? 0 : (MaxUses - Uses) * 100 / MaxUses;
    }

    public sealed class StandingDeck
    {
        public readonly List<CardInstance> Cards = new List<CardInstance>();

        public int Count => Cards.Count;

        public void Add(CardDef def, int count, int fromYear, string fromSeasonId = null)
        {
            for (int i = 0; i < count; i++)
                Cards.Add(new CardInstance
                {
                    CardId = def.id, Uses = def.durability, MaxUses = def.durability,
                    FromYear = fromYear, FromSeasonId = fromSeasonId
                });
        }

        /// <summary>다 쓴 카드를 덱에서 뺀다. 몇 장이 부서졌는지 돌려준다.</summary>
        public int SweepBroken()
        {
            int n = Cards.RemoveAll(c => c.Broken);
            return n;
        }

        /// <summary>진 해의 수확을 잃는다 — 그해에 들어온 카드만 도로 빠진다.</summary>
        public int RemoveHarvestOfYear(int year) => Cards.RemoveAll(c => c.FromYear == year);

        /// <summary>한 계절의 수확만 털린다. 계절 침입에 졌을 때.</summary>
        public int RemoveHarvestOfSeason(int year, string seasonId)
            => Cards.RemoveAll(c => c.FromYear == year && c.FromSeasonId == seasonId);

        public void Remove(CardInstance inst) => Cards.Remove(inst);

        public Dictionary<string, int> CountsByCard()
        {
            var d = new Dictionary<string, int>();
            foreach (var c in Cards) d[c.CardId] = d.TryGetValue(c.CardId, out var n) ? n + 1 : 1;
            return d;
        }

        /// <summary>카드 종류별로 덱에 남아 있는 총 사용 횟수. "앞으로 몇 번 더 벨 수 있는가".</summary>
        public Dictionary<string, int> UsesByCard()
        {
            var d = new Dictionary<string, int>();
            foreach (var c in Cards) d[c.CardId] = d.TryGetValue(c.CardId, out var n) ? n + c.Uses : c.Uses;
            return d;
        }

        public int TotalUses
        {
            get { int n = 0; foreach (var c in Cards) n += c.Uses; return n; }
        }
    }
}
