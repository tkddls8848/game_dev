// 표본 하나와 그 표본의 내력(來歷).
//
// 이 PoC 를 먼저 만든 둘과 가르는 것이 이 파일이다.
//   hybrid-harvest-deck : 덱은 그 해 수확물이고 밤이 끝나면 사라진다
//   hybrid-siege-seasons: 덱은 해를 넘겨 남고 쓰면 닳는다
//   여기(hybrid-sown-deck): 덱은 밤이 끝나면 사라지지만, **한 번도 놓지 않은 표본은
//                           버려지지 않고 밭으로 돌아가 심긴다.** 다음 해에 변해서 온다.
//
// 그래서 표본마다 '몇 해째 이어진 것인가'와 '어떤 모습을 거쳐 왔는가'가 있다.
// 연출(식물 표본집)이 한 표본의 여러 해 모습을 옆으로 늘어놓는 근거가 이 Lineage 다.
using System.Collections.Generic;

namespace HybridSownDeck
{
    /// <summary>표본이 한 해에 어떤 모습이었는가. 표본집의 한 칸이다.</summary>
    public sealed class LineageStep
    {
        public int Year;
        public string CardId;
        /// <summary>harvest(밭에서 났다) · grow(자랐다) · split(갈라져 둘이 됐다)</summary>
        public string How;
    }

    public sealed class Specimen
    {
        public string CardId;
        /// <summary>이 모습이 된 해.</summary>
        public int FromYear;
        /// <summary>어느 계절에 손에 들어왔는가. 심어서 돋아난 것은 봄이다.</summary>
        public string FromSeasonId;
        /// <summary>씨앗으로 길러 거둔 것이면 작물 id, 심어서 온 것이면 null.</summary>
        public string SourceCropId;
        /// <summary>이 해 밤에 한 번이라도 놓았는가. 놓은 표본은 밭으로 돌아가지 못한다.</summary>
        public bool Played;
        /// <summary>표본 번호. 갈라지면 뒤에 갈래가 붙는다(예: S07 → S07a · S07b).</summary>
        public string LineageId;
        /// <summary>몇 해째 이어진 표본인가. 1 = 올해 밭에서 났다.</summary>
        public int Generation;
        public List<LineageStep> Lineage = new List<LineageStep>();

        public Specimen Fresh(string cardId, int year, string seasonId, string how, string lineageId, int generation)
        {
            var s = new Specimen
            {
                CardId = cardId, FromYear = year, FromSeasonId = seasonId,
                SourceCropId = null, Played = false,
                LineageId = lineageId, Generation = generation
            };
            s.Lineage.AddRange(Lineage);
            s.Lineage.Add(new LineageStep { Year = year, CardId = cardId, How = how });
            return s;
        }
    }

    /// <summary>겨울에 밭으로 돌아간 표본. 다음 봄에 돋아난다.</summary>
    public sealed class SownSpecimen
    {
        public Specimen Source;
        /// <summary>심은 해. 돋아나는 해는 이보다 하나 크다.</summary>
        public int SownInYear;
        /// <summary>봄의 밭칸을 몇 턴 묶는가.</summary>
        public int OccupiesTurns;
    }

    /// <summary>그 해의 덱. 밤이 끝나면 놓지 않은 것만 남는다.</summary>
    public sealed class YearDeck
    {
        public readonly List<Specimen> Cards = new List<Specimen>();

        public int Count => Cards.Count;

        /// <summary>밭에서 거둔 표본. 이 길과 '심는 길' 둘뿐이고 둘 다 밭이다 — 카드 상점은 없다.</summary>
        public void AddHarvest(CardDef def, CropDef from, int count, int year, string seasonId, ref int lineageCounter)
        {
            for (int i = 0; i < count; i++)
            {
                lineageCounter++;
                var s = new Specimen
                {
                    CardId = def.id, FromYear = year, FromSeasonId = seasonId,
                    SourceCropId = from.id, Played = false,
                    LineageId = "S" + lineageCounter.ToString("D3"), Generation = 1
                };
                s.Lineage.Add(new LineageStep { Year = year, CardId = def.id, How = "harvest" });
                Cards.Add(s);
            }
        }

        public void Add(Specimen s) => Cards.Add(s);

        public Dictionary<string, int> CountsByCard()
        {
            var d = new Dictionary<string, int>();
            foreach (var c in Cards) d[c.CardId] = d.TryGetValue(c.CardId, out var n) ? n + 1 : 1;
            return d;
        }

        /// <summary>밤에 한 번도 놓지 않은 표본. 밭으로 돌아갈 수 있는 것이 이들뿐이다.</summary>
        public List<Specimen> Unplayed()
        {
            var list = new List<Specimen>();
            foreach (var c in Cards) if (!c.Played) list.Add(c);
            return list;
        }

        public int PlayedCount
        {
            get { int n = 0; foreach (var c in Cards) if (c.Played) n++; return n; }
        }
    }

    /// <summary>심은 표본이 다음 봄에 무엇이 되는가. 규칙은 cards.json 의 sown 한 곳에만 있다.</summary>
    public static class Sowing
    {
        public sealed class Sprouted
        {
            public List<Specimen> Cards = new List<Specimen>();
            public int Seeds;
        }

        /// <summary>
        /// grow  — 자란다. 한 장이 더 좋은 한 장으로 바뀐다
        /// split — 갈라져 둘이 된다. 좋은 한 장이 덜 좋은 두 장이 된다
        /// seedfall — 씨앗만 남는다. 카드는 남지 않는다
        /// </summary>
        public static Sprouted Sprout(GameData d, SownSpecimen sown, int year, string seasonId)
        {
            var def = d.Card(sown.Source.CardId);
            var outp = new Sprouted();
            var rule = def.sown;
            if (rule == null) return outp;

            if (rule.YieldsSeeds)
            {
                outp.Seeds += rule.seeds > 0 ? rule.seeds : 0;
                return outp;
            }
            if (!rule.YieldsCard || string.IsNullOrEmpty(rule.becomes)) return outp;

            int n = rule.amount < 1 ? 1 : rule.amount;
            for (int i = 0; i < n; i++)
            {
                // 갈라지면 표본 번호에 갈래가 붙는다. 한 장이면 번호를 그대로 물려받는다.
                string lineageId = n == 1
                    ? sown.Source.LineageId
                    : sown.Source.LineageId + (char)('a' + i);
                outp.Cards.Add(sown.Source.Fresh(rule.becomes, year, seasonId, rule.type,
                                                 lineageId, sown.Source.Generation + 1));
            }
            return outp;
        }
    }
}
