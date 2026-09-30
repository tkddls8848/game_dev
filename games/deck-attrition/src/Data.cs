using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DeckAttrition
{
    /// <summary>
    /// 효과 하나. {type, amount, target} 세 필드의 평평한 배열이다 (PLAN_DECKBUILDER §6).
    /// 값 없는 int 는 -1 을 명시한다 — JSON 이 빠뜨린 int 가 0 으로 채워지면 "피해 0" 이 조용히 생긴다.
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

        /// <summary>
        /// 이 회차에서 이 활자가 <b>몇 번 더 찍을 수 있는가</b>. -1 이면 닳지 않는다(상용 활자).
        /// 이 한 필드가 이 PoC의 규칙 전부다: 한 판이 아니라 <b>회차 전체</b>에서 줄어든다.
        /// </summary>
        public int uses = -1;

        /// 한 번 찍을 때마다 선명도가 떨어지는 폭(백분율 정수). 닳지 않는 활자는 0.
        public int wearPerUsePct = -1;

        public EffectData[] effects;

        /// 닳는 활자인가. `uses > 0` 하나로 판정한다 — 따로 플래그를 두면 둘이 어긋난다.
        public bool IsConsumable => uses > 0;

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

        /// 행동 패턴. 순환한다.
        public EffectData[] pattern;
    }

    public sealed class RunData
    {
        public string id;
        public int seed = -1;
        public int playerMaxHp = -1;
        public int handSize = -1;
        public int energyPerTurn = -1;
        public int maxRoundsPerBattle = -1;
        public int healAfterBattle = -1;
        public int rewardsPerBattle = -1;

        /// 보상으로 받은 활자가 이미 상자에 있을 때 더해 주는 횟수. §RunEngine 참고.
        public int rewardRecastUses = -1;

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
        public int measuredWinRatePctMin = -1;
        public int measuredWinRatePctMax = -1;
        public int deadCardMinAdoptionPct = -1;
        public int seedBase = -1;

        /// 선명도의 바닥. 아무리 닳아도 이 아래로는 안 내려간다 — 0 이 되면 "남았는데 쓸모없는" 활자가 된다.
        public int minSharpnessPct = -1;

        // --- HoardingIsNotOptimal (이 PoC의 핵) ---
        /// 적절히 쓰는 정책이 아끼는 정책을 이 폭(%p) 이상으로 이겨야 한다.
        public int hoardingMarginPct = -1;
        /// 적절히 쓰는 정책이 즉시 쓰는 정책을 이 폭(%p) 이상으로 이겨야 한다.
        public int spendNowMarginPct = -1;

        // --- 적절히 쓰는 정책의 손잡이 ---
        /// 체력이 이 백분율 아래로 내려가면 닳는 활자를 꺼낸다.
        public int measuredHpTriggerPct = -1;
        /// 남은 전투가 이 수 이하면(=막바지면) 아끼지 않는다.
        public int measuredEndgameBattlesLeft = -1;
        /// 다음 한 대가 지금 체력의 이 백분율 이상이면 아끼지 않는다.
        public int measuredIncomingTriggerPct = -1;

        // --- AttritionReachable ---
        /// 정상적으로 끝낸 회차 중 활자가 최소 하나 <b>다 닳아 없어진</b> 비율의 하한.
        public int attritionExhaustedRunPctMin = -1;
        /// 한 회차에서 쓰인 활자 횟수의 하한(평균).
        public int attritionUsesPerRunMin = -1;
    }

    // 이 셋은 Newtonsoft 가 채운다. 코드에서 대입하지 않으므로 CS0649 가 뜨고,
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

        /// <summary>
        /// 카드의 데이터 배열 순서. <b>Dictionary 순회를 쓰지 않기 위해 존재한다</b> —
        /// 순회 순서가 결과를 정하는 자리(재주조 대상 고르기 등)가 있고, 거기서 순서가
        /// 흔들리면 씨드 재현이 깨진다.
        /// </summary>
        public int CardIndex(string id)
        {
            for (int i = 0; i < Cards.Length; i++) if (Cards[i].id == id) return i;
            return -1;
        }

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
