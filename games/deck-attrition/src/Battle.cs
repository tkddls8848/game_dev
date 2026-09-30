using System;
using System.Collections.Generic;
using System.Text;

namespace DeckAttrition
{
    public enum BattleOutcome { InProgress, PlayerWon, PlayerLost }

    /// <summary>전투원. 수치는 전부 정수다 (설계 원칙 4).</summary>
    public sealed class Combatant
    {
        public int Hp;
        public int MaxHp;
        public int Block;
        public int Strength;

        /// 먹번짐. 적의 턴마다 이만큼 타고 한 칸 줄어든다.
        public int Smudge;

        public bool Dead => Hp <= 0;
        public int HpPct => MaxHp <= 0 ? 0 : Hp * 100 / MaxHp;
    }

    /// <summary>
    /// 전투 하나. <b>마모 상태는 여기 없다</b> — <see cref="TypeCase"/> 에 있고 전투는 빌려 쓴다.
    /// 그게 이 PoC의 규칙이다: 활자는 한 판이 아니라 회차 전체에서 닳는다.
    ///
    /// 적의 다음 수는 <b>보인다</b>(<see cref="NextIntent"/>). 숨기지 않는 이유는 규칙 때문이다 —
    /// "지금 쓸까 아껴 둘까"가 결정이 되려면 지금 무엇이 오는지 알아야 한다.
    /// 모르는 상태에서 아끼는 것은 결정이 아니라 도박이다.
    /// (`games/deck-rewind` 는 반대로 숨기고, `games/deck-openhand` 는 다섯 수를 보여 준다.)
    /// </summary>
    public sealed class Battle
    {
        public const int StepGuard = 400;
        public const int MaxPlaysPerTurn = 30;

        readonly GameData _data;
        readonly List<string> _log = new List<string>();

        public Rng Rng { get; }
        public TypeCase Case { get; }
        public Combatant Player { get; } = new Combatant();
        public Combatant Enemy { get; } = new Combatant();
        public EnemyData EnemyDef { get; }

        public List<string> Deck { get; } = new List<string>();
        public List<string> Hand { get; } = new List<string>();
        public List<string> Discard { get; } = new List<string>();

        public int Energy { get; private set; }
        public int EnergyPerTurn { get; }
        public int HandSize { get; }
        public int Round { get; private set; }
        public int MaxRounds { get; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;

        /// 이 전투에서 다 닳아 사라진 활자들. 목업이 여기에 표식을 찍는다.
        public List<string> SpentHere { get; } = new List<string>();

        /// 이 전투가 회차의 몇 번째인가 · 뒤에 몇 개가 남았는가. 정책이 "아직 아껴도 되는가"를 여기서 본다.
        public int BattleIndex { get; }
        public int BattlesLeftAfter { get; }
        public bool IsBoss { get; }

        public IReadOnlyList<string> Log => _log;

        public Battle(GameData data, TypeCase typeCase, EnemyData enemyDef, IEnumerable<string> deck,
                      int playerHp, int playerMaxHp, int seed,
                      int handSize, int energyPerTurn, int maxRounds,
                      int battleIndex = 0, int battlesLeftAfter = 0, bool isBoss = false)
        {
            _data = data;
            Case = typeCase;
            EnemyDef = enemyDef;
            Rng = new Rng(seed);

            Player.MaxHp = playerMaxHp;
            Player.Hp = playerHp;
            Enemy.MaxHp = enemyDef.maxHp;
            Enemy.Hp = enemyDef.maxHp;

            HandSize = handSize;
            EnergyPerTurn = energyPerTurn;
            MaxRounds = maxRounds;
            BattleIndex = battleIndex;
            BattlesLeftAfter = battlesLeftAfter;
            IsBoss = isBoss;

            foreach (var id in deck)
            {
                // 이미 다 닳은 활자는 덱에 오르지 않는다. 회차 초반에 녹은 활자가
                // 다음 전투에서 손에 오면 규칙이 한 판짜리로 되돌아간다.
                if (Case.IsSpent(id)) continue;
                Deck.Add(id);
            }
            Rng.Shuffle(Deck);

            Round = 0;
            _log.Add("battle " + enemyDef.id + " seed=" + seed + " hp=" + playerHp
                     + " deck=" + Deck.Count + " case=" + Case.Describe());
        }

        public EffectData NextIntent => EnemyDef.pattern[_intentIndex];
        int _intentIndex;

        /// 다음 수가 실제로 줄 피해. 방어를 뺀 값이 아니라 날것이다.
        public int IncomingDamage =>
            NextIntent.type == "attack" ? Math.Max(0, NextIntent.amount + Enemy.Strength) : 0;

        // ---------- 턴 진행 ----------

        public void BeginPlayerTurn()
        {
            Round++;
            Player.Block = 0;
            Energy = EnergyPerTurn;
            DrawTo(HandSize);
            _log.Add("T" + Round + " begin hp=" + Player.Hp + " ehp=" + Enemy.Hp
                     + " eblk=" + Enemy.Block + " hand=" + string.Join(",", Hand)
                     + " intent=" + NextIntent.type + ":" + IncomingDamage);
        }

        public bool CanPlay(int handIndex) =>
            handIndex >= 0 && handIndex < Hand.Count && _data.Card(Hand[handIndex]).cost <= Energy;

        public void PlayCard(int handIndex)
        {
            var card = _data.Card(Hand[handIndex]);
            if (card.cost > Energy) throw new InvalidOperationException("기력 부족: " + card.id);
            if (Case.IsSpent(card.id)) throw new InvalidOperationException("다 닳은 활자: " + card.id);

            Hand.RemoveAt(handIndex);
            Energy -= card.cost;
            int sharp = Case.SharpnessPct(card.id);
            _log.Add("  play " + card.id + " sharp=" + sharp + "%");

            // 효과는 <b>지금의 선명도</b>로 찍힌다. 닳는 것은 찍은 다음이다 — 실제 인쇄와 같은 순서다.
            foreach (var e in card.effects) ApplyCardEffect(card, e);

            if (card.IsConsumable)
            {
                bool gone = Case.Spend(card.id);
                _log.Add("    wear " + card.id + " left=" + Case.UsesLeft(card.id)
                         + " sharp=" + Case.SharpnessPct(card.id) + "%");
                if (gone) MeltDown(card.id);
                else Discard.Add(card.id);
            }
            else
            {
                Discard.Add(card.id);
            }

            CheckOutcome();
        }

        /// <summary>
        /// 다 닳은 활자를 회차에서 걷어낸다. 덱·손·버림 어디에 있든 전부다 —
        /// 횟수는 카드 장 수가 아니라 <b>활자 하나</b>에 붙어 있기 때문이다.
        /// </summary>
        void MeltDown(string cardId)
        {
            int removed = Deck.RemoveAll(x => x == cardId)
                        + Hand.RemoveAll(x => x == cardId)
                        + Discard.RemoveAll(x => x == cardId);
            SpentHere.Add(cardId);
            _log.Add("    melt " + cardId + " (덱·손·버림에서 " + removed + "장 걷어냄)");
        }

        public void EndPlayerTurn()
        {
            Discard.AddRange(Hand);
            Hand.Clear();
            _log.Add("  endturn");
        }

        public void EnemyTurn()
        {
            if (Outcome != BattleOutcome.InProgress) return;

            if (Enemy.Smudge > 0)
            {
                Enemy.Hp -= Enemy.Smudge;
                _log.Add("  smudge " + Enemy.Smudge + " ehp=" + Enemy.Hp);
                Enemy.Smudge--;
                CheckOutcome();
                if (Outcome != BattleOutcome.InProgress) { AdvanceIntent(); return; }
            }

            var intent = EnemyDef.pattern[_intentIndex];
            Enemy.Block = 0;
            ApplyEnemyEffect(intent);
            AdvanceIntent();
            CheckOutcome();

            if (Outcome == BattleOutcome.InProgress && Round >= MaxRounds)
            {
                // 시간이 다 되면 진 것으로 친다. 끝나지 않는 전투는 시뮬레이션을 못 돌게 만든다.
                Outcome = BattleOutcome.PlayerLost;
                _log.Add("  timeout round=" + Round);
            }
        }

        void AdvanceIntent() => _intentIndex = (_intentIndex + 1) % EnemyDef.pattern.Length;

        // ---------- 효과 ----------

        void ApplyCardEffect(CardData card, EffectData e)
        {
            int amount = Case.Print(card.id, e.type, e.amount);
            switch (e.type)
            {
                case "damage":
                    DealToEnemy(amount + Player.Strength);
                    break;
                case "block":
                    Player.Block += amount;
                    break;
                case "heal":
                    Player.Hp = Math.Min(Player.MaxHp, Player.Hp + amount);
                    break;
                case "strength":
                    Player.Strength += amount;
                    break;
                case "smudge":
                    Enemy.Smudge += amount;
                    break;
                case "draw":
                    DrawTo(Hand.Count + amount);
                    break;
                case "energy":
                    Energy += amount;
                    break;
                case "recast":
                    // 가장 뭉개진 활자를 다시 붓는다. 자기 자신은 고르지 않는다 —
                    // 그러면 재주조가 스스로를 무한히 먹여 규칙이 사라진다.
                    string target = Case.MostWorn(card.id);
                    if (target != null)
                    {
                        Case.Recast(target, amount);
                        _log.Add("    recast " + target + " left=" + Case.UsesLeft(target)
                                 + " sharp=" + Case.SharpnessPct(target) + "%");
                    }
                    else _log.Add("    recast (다시 부을 활자가 없다)");
                    break;
                default:
                    throw new InvalidOperationException("모르는 효과: " + e.type + " (" + card.id + ")");
            }
        }

        void ApplyEnemyEffect(EffectData e)
        {
            switch (e.type)
            {
                case "attack":
                {
                    int raw = Math.Max(0, e.amount + Enemy.Strength);
                    int absorbed = Math.Min(Player.Block, raw);
                    Player.Block -= absorbed;
                    Player.Hp -= raw - absorbed;
                    _log.Add("  enemy attack " + raw + " (막음 " + absorbed + ") hp=" + Player.Hp);
                    break;
                }
                case "block":
                    Enemy.Block += e.amount;
                    _log.Add("  enemy block " + e.amount);
                    break;
                case "strengthen":
                    Enemy.Strength += e.amount;
                    _log.Add("  enemy strengthen " + e.amount);
                    break;
                case "heal":
                    Enemy.Hp = Math.Min(Enemy.MaxHp, Enemy.Hp + e.amount);
                    _log.Add("  enemy heal " + e.amount + " ehp=" + Enemy.Hp);
                    break;
                default:
                    throw new InvalidOperationException("모르는 적 효과: " + e.type);
            }
        }

        void DealToEnemy(int raw)
        {
            if (raw <= 0) return;
            int absorbed = Math.Min(Enemy.Block, raw);
            Enemy.Block -= absorbed;
            Enemy.Hp -= raw - absorbed;
            _log.Add("    hit " + raw + " (막힘 " + absorbed + ") ehp=" + Enemy.Hp);
        }

        // ---------- 덱 ----------

        public void DrawTo(int target)
        {
            int guard = 0;
            while (Hand.Count < target && guard++ < StepGuard)
            {
                if (Deck.Count == 0)
                {
                    if (Discard.Count == 0) return;
                    Deck.AddRange(Discard);
                    Discard.Clear();
                    Rng.Shuffle(Deck);
                    _log.Add("  reshuffle " + Deck.Count);
                }
                int last = Deck.Count - 1;
                Hand.Add(Deck[last]);
                Deck.RemoveAt(last);
            }
        }

        void CheckOutcome()
        {
            if (Outcome != BattleOutcome.InProgress) return;
            if (Enemy.Hp <= 0) { Outcome = BattleOutcome.PlayerWon; _log.Add("  win round=" + Round); }
            else if (Player.Hp <= 0) { Outcome = BattleOutcome.PlayerLost; _log.Add("  lose round=" + Round); }
        }

        /// <summary>화면 없이 한 전투를 끝까지 돌린다. 이게 하루에 PoC 스무 개를 보는 방법이다.</summary>
        public void RunToEnd(IAgent agent)
        {
            int guard = 0;
            while (Outcome == BattleOutcome.InProgress && guard++ < StepGuard)
            {
                BeginPlayerTurn();
                agent.TakePlayerTurn(this);
                if (Outcome != BattleOutcome.InProgress) break;
                EndPlayerTurn();
                EnemyTurn();
            }
            if (Outcome == BattleOutcome.InProgress)
            {
                Outcome = BattleOutcome.PlayerLost;
                _log.Add("  guard-stop");
            }
        }

        public string Transcript()
        {
            var sb = new StringBuilder();
            foreach (var line in _log) sb.Append(line).Append('\n');
            return sb.ToString();
        }
    }
}
