using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DeckRewind
{
    /// <summary>
    /// 효과 하나. {type, amount, target} 세 필드의 평평한 형태다 (설계 원칙 3).
    /// 다형성이 필요해 보이지만 필요하지 않다 — 이 형태가 제약이자 해답이다.
    /// 값 없는 int 는 -1 을 명시한다. JSON 이 빠뜨린 int 를 0 으로 채우면
    /// "피해 0" 같은 조용한 버그가 되기 때문이다.
    /// </summary>
    public sealed class EffectData
    {
        public string type;
        public int amount = -1;
        public string target;

        public override string ToString() => type + ":" + amount + "->" + target;
    }

    public sealed class CardData
    {
        public string id;
        public string nameKo;
        public string textKo;
        public int cost = -1;
        public string rarity;
        public EffectData[] effects;

        /// <summary>
        /// 화면에 쓰는 이름. 한국어가 원문이고 영어가 덮어쓰기다 (설계 원칙 6).
        /// <b>속성이지 필드가 아니다</b> — 정적 필드에 담으면 언어를 바꿔도 첫 값이 굳는다.
        /// </summary>
        public string Name => Localization.Text("card." + id + ".name", nameKo);
        public string Text => Localization.Text("card." + id + ".text", textKo);
    }

    public sealed class EnemyData
    {
        public string id;
        public string nameKo;
        public int maxHp = -1;

        /// 화면에 쓰는 이름. 속성으로 둔다 — 정적 필드에 담으면 언어를 바꿔도 첫 값이 굳는다.
        public string Name => Localization.Text("enemy." + id + ".name", nameKo);

        /// 되감을 때마다 적 피해에 더해지는 백분율. 이 값이 되감기의 대가다.
        public int memoryDamagePctPerStack = -1;

        /// 행동 패턴. 순환한다. 되감기는 여기의 오프셋을 밀어 같은 수를 다시 두지 않게 만든다.
        public EffectData[] pattern;
    }

    public sealed class RunData
    {
        public string id;
        public int seed = -1;
        public int playerMaxHp = -1;
        public int handSize = -1;
        public int energyPerTurn = -1;
        public int rewindCharges = -1;
        public int maxRewindsPerBattle = -1;
        public int maxRoundsPerBattle = -1;
        public int healAfterBattle = -1;
        public int rewardsPerBattle = -1;
        public string[] startingDeck;
        public string[] rewardPool;
        public string[] battles;
        public string boss;
    }

    public sealed class BalanceData
    {
        public int monteCarloRuns = -1;
        public int dominanceRuns = -1;
        public int dominanceMaxWinRatePct = -1;
        public int randomWinRatePctMin = -1;
        public int randomWinRatePctMax = -1;
        public int randomBattleWinRatePctMin = -1;
        public int randomBattleWinRatePctMax = -1;
        public int greedyWinRatePctMin = -1;
        public int greedyWinRatePctMax = -1;
        public int deadCardMinAdoptionPct = -1;
        public int rewardPenaltyMaxPct = -1;
        public int seedBase = -1;

        /// 약화(weak) 가 걸린 쪽의 피해 배율. 백분율 정수 + 나머지 누적으로 쓴다.
        public int weakDamagePct = -1;
    }

    // 이 둘은 Newtonsoft 가 채운다. 코드에서 대입하지 않으므로 CS0649 가 뜨고,
    // 경고를 0으로 유지하는 것이 이 저장소의 규율이라 그 자리만 꺼 둔다.
#pragma warning disable 0649
    sealed class CardFile { public CardData[] cards; }
    sealed class EnemyFile { public EnemyData[] enemies; }
#pragma warning restore 0649

    /// <summary>data/ 의 JSON 을 읽는다. 테스트는 빌드 산출물에서 위로 올라가며 data/ 를 찾는다.</summary>
    public sealed class GameData
    {
        public CardData[] Cards { get; private set; }
        public EnemyData[] Enemies { get; private set; }
        public RunData Run { get; private set; }
        public BalanceData Balance { get; private set; }
        public string DataDir { get; private set; }

        readonly Dictionary<string, CardData> _cardById = new Dictionary<string, CardData>();
        readonly Dictionary<string, EnemyData> _enemyById = new Dictionary<string, EnemyData>();

        public CardData Card(string id) =>
            _cardById.TryGetValue(id, out var c) ? c : throw new KeyNotFoundException("카드 없음: " + id);

        public EnemyData Enemy(string id) =>
            _enemyById.TryGetValue(id, out var e) ? e : throw new KeyNotFoundException("적 없음: " + id);

        public bool HasCard(string id) => _cardById.ContainsKey(id);
        public bool HasEnemy(string id) => _enemyById.ContainsKey(id);

        public static string FindDataDir(string startDir)
        {
            var d = new DirectoryInfo(startDir);
            while (d != null)
            {
                string candidate = Path.Combine(d.FullName, "data");
                if (File.Exists(Path.Combine(candidate, "cards.json"))) return candidate;
                d = d.Parent;
            }
            throw new DirectoryNotFoundException("data/cards.json 을 찾을 수 없다. 시작 위치: " + startDir);
        }

        public static GameData Load(string startDir = null, string runId = "run_01")
        {
            string dir = FindDataDir(startDir ?? AppContext.BaseDirectory);
            var g = new GameData { DataDir = dir };
            g.Cards = JsonConvert.DeserializeObject<CardFile>(File.ReadAllText(Path.Combine(dir, "cards.json"))).cards;
            g.Enemies = JsonConvert.DeserializeObject<EnemyFile>(File.ReadAllText(Path.Combine(dir, "enemies.json"))).enemies;
            g.Run = JsonConvert.DeserializeObject<RunData>(File.ReadAllText(Path.Combine(dir, "runs", runId + ".json")));
            g.Balance = JsonConvert.DeserializeObject<BalanceData>(File.ReadAllText(Path.Combine(dir, "balance.json")));
            foreach (var c in g.Cards) g._cardById[c.id] = c;
            foreach (var e in g.Enemies) g._enemyById[e.id] = e;
            return g;
        }
    }
}
