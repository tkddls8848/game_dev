using System;
using System.Collections.Generic;
using System.Text;

namespace DeckRewind
{
    public enum BattleOutcome { InProgress, PlayerWon, PlayerLost }

    /// <summary>전투원. 수치는 전부 정수다 (설계 원칙 4). 나머지 누적 칸을 함께 들고 다닌다.</summary>
    public sealed class Combatant
    {
        public int Hp;
        public int MaxHp;
        public int Block;
        public int Strength;
        public int Weak;
        public int Burn;

        /// 백분율 정수 나눗셈에서 버려질 나머지. 누적해야 열 번 때린 총합이 맞는다.
        public int WeakRemainder;
        public int MemoryRemainder;

        public bool Dead => Hp <= 0;
    }

    /// <summary>
    /// 전투 하나. 되감기 규칙이 여기 들어 있다.
    ///
    /// 핵심: <b>적의 의도는 보이지 않는다.</b> 무엇이 오는지는 맞고 나서 안다.
    /// 되감기는 그 정보를 들고 한 라운드 앞으로 돌아가는 수단이고, 대가는 둘이다 —
    /// 적이 패턴을 한 칸 밀어 <b>같은 수를 다시 두지 않고</b>, 기억한 만큼 <b>더 세게 때린다</b>.
    /// 대가가 없으면 되감기는 규칙이 아니라 편의다.
    /// </summary>
    public sealed class Battle
    {
        /// 되감기가 라운드를 되돌리므로 라운드 수만으로는 끝을 보장하지 못한다. 절대 상한을 둔다.
        public const int StepGuard = 600;
        public const int MaxPlaysPerTurn = 30;

        readonly GameData _data;
        readonly List<Snapshot> _snapshots = new List<Snapshot>();
        readonly List<string> _log = new List<string>();

        public Rng Rng { get; }
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

        // --- 되감기 ---
        public int RewindCharges { get; private set; }
        public int RewindsUsed { get; private set; }
        public int MaxRewinds { get; }

        /// 적의 기억. 되감은 횟수. 스냅샷에 넣지 않는다 — 되돌려지지 않는 것이 규칙의 전부다.
        public int Memory { get; private set; }

        int _patternOffset;
        int _intentIndex;

        /// 플레이어가 본 적의 의도. 패턴 인덱스로 색인한다 (Dictionary 순회 순서에 기대지 않는다).
        readonly string[] _observedType;
        readonly int[] _observedAmount;

        public int WeakDamagePct { get; }
        public IReadOnlyList<string> Log => _log;

        public Battle(GameData data, EnemyData enemyDef, IEnumerable<string> deck,
                      int playerHp, int playerMaxHp, int seed,
                      int handSize, int energyPerTurn, int rewindCharges,
                      int maxRewinds, int maxRounds)
        {
            _data = data;
            EnemyDef = enemyDef;
            Rng = new Rng(seed);

            Player.MaxHp = playerMaxHp;
            Player.Hp = playerHp;
            Enemy.MaxHp = enemyDef.maxHp;
            Enemy.Hp = enemyDef.maxHp;

            HandSize = handSize;
            EnergyPerTurn = energyPerTurn;
            RewindCharges = rewindCharges;
            MaxRewinds = maxRewinds;
            MaxRounds = maxRounds;
            WeakDamagePct = data.Balance.weakDamagePct;

            _observedType = new string[enemyDef.pattern.Length];
            _observedAmount = new int[enemyDef.pattern.Length];
            for (int i = 0; i < _observedAmount.Length; i++) _observedAmount[i] = -1;

            Deck.AddRange(deck);
            Rng.Shuffle(Deck);
            Round = 0;
            _log.Add("battle " + enemyDef.id + " seed=" + seed + " hp=" + playerHp + " deck=" + Deck.Count);
        }

        public int EffectiveIntentIndex
        {
            get
            {
                int n = EnemyDef.pattern.Length;
                return ((_intentIndex + _patternOffset) % n + n) % n;
            }
        }

        public EffectData NextIntent => EnemyDef.pattern[EffectiveIntentIndex];

        /// <summary>
        /// 플레이어가 아는 다음 의도. 본 적이 없으면 false 다.
        /// 이것이 비어 있는 것이 이 PoC의 규칙이고, 되감기가 이것을 채운다.
        /// </summary>
        public bool TryPeekKnownIntent(out string type, out int amount)
        {
            int i = EffectiveIntentIndex;
            type = _observedType[i];
            amount = _observedAmount[i];
            return type != null;
        }

        public int ObservedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _observedType.Length; i++) if (_observedType[i] != null) n++;
                return n;
            }
        }

        public bool CanRewind => Outcome != BattleOutcome.PlayerWon
                                 && RewindCharges > 0
                                 && RewindsUsed < MaxRewinds
                                 && _snapshots.Count >= 2;

        // ---------- 턴 진행 ----------

        public void BeginPlayerTurn()
        {
            Round++;
            Player.Block = 0;
            Energy = EnergyPerTurn;
            DrawTo(HandSize);
            TakeSnapshot();
            _log.Add("T" + Round + " begin hp=" + Player.Hp + " ehp=" + Enemy.Hp
                     + " eblk=" + Enemy.Block + " hand=" + string.Join(",", Hand)
                     + " mem=" + Memory + " rw=" + RewindCharges);
        }

        public bool CanPlay(int handIndex) =>
            handIndex >= 0 && handIndex < Hand.Count && _data.Card(Hand[handIndex]).cost <= Energy;

        public void PlayCard(int handIndex)
        {
            var card = _data.Card(Hand[handIndex]);
            if (card.cost > Energy) throw new InvalidOperationException("기력 부족: " + card.id);
            Hand.RemoveAt(handIndex);
            Energy -= card.cost;
            Discard.Add(card.id);
            _log.Add("  play " + card.id);
            foreach (var e in card.effects) ApplyCardEffect(e);
            CheckOutcome();
        }

        public void EndPlayerTurn()
        {
            Discard.AddRange(Hand);
            Hand.Clear();
            if (Player.Weak > 0) Player.Weak--;
            _log.Add("  endturn");
        }

        public void EnemyTurn()
        {
            if (Outcome != BattleOutcome.InProgress) return;

            if (Enemy.Burn > 0)
            {
                Enemy.Hp -= Enemy.Burn;
                _log.Add("  burn " + Enemy.Burn + " ehp=" + Enemy.Hp);
                Enemy.Burn--;
                CheckOutcome();
                if (Outcome != BattleOutcome.InProgress) { AdvanceIntent(); return; }
            }

            int idx = EffectiveIntentIndex;
            var intent = EnemyDef.pattern[idx];
            _observedType[idx] = intent.type;
            _observedAmount[idx] = intent.amount;

            Enemy.Block = 0;
            switch (intent.type)
            {
                case "attack":
                    int dealt = DealEnemyDamage(intent.amount);
                    _log.Add("  enemy attack " + intent.amount + " dealt=" + dealt + " hp=" + Player.Hp);
                    break;
                case "block":
                    Enemy.Block += intent.amount;
                    _log.Add("  enemy block " + intent.amount);
                    break;
                case "weak":
                    Player.Weak += intent.amount;
                    _log.Add("  enemy weak " + intent.amount);
                    break;
                case "strengthen":
                    Enemy.Strength += intent.amount;
                    _log.Add("  enemy strengthen " + intent.amount);
                    break;
                case "heal":
                    Enemy.Hp = Math.Min(Enemy.MaxHp, Enemy.Hp + intent.amount);
                    _log.Add("  enemy heal " + intent.amount + " ehp=" + Enemy.Hp);
                    break;
                default:
                    throw new NotSupportedException("모르는 적 의도: " + intent.type);
            }

            if (Enemy.Weak > 0) Enemy.Weak--;
            AdvanceIntent();
            CheckOutcome();
            if (Outcome == BattleOutcome.InProgress && Round >= MaxRounds)
            {
                Outcome = BattleOutcome.PlayerLost;
                _log.Add("  timeout at T" + Round);
            }
        }

        void AdvanceIntent() => _intentIndex = (_intentIndex + 1) % EnemyDef.pattern.Length;

        // ---------- 되감기 ----------

        /// <summary>
        /// 한 라운드 되감는다. 이전 라운드 시작 시점으로 돌아가므로 적의 마지막 행동도 취소된다.
        /// 취소되지 않는 것이 셋 있다: 충전 소모 · 적의 기억 · 플레이어가 본 것.
        /// </summary>
        public void Rewind()
        {
            if (!CanRewind) throw new InvalidOperationException("되감을 수 없다");

            var target = _snapshots[Round - 2];
            _snapshots.RemoveRange(Round - 1, _snapshots.Count - (Round - 1));
            Restore(target);

            RewindCharges--;
            RewindsUsed++;
            Memory++;
            // 같은 수를 다시 두지 않는다. 패턴을 한 칸 밀어 둔다.
            _patternOffset = (_patternOffset + 1) % EnemyDef.pattern.Length;
            Outcome = BattleOutcome.InProgress;
            _log.Add("REWIND T" + Round + " mem=" + Memory + " rw=" + RewindCharges
                     + " offset=" + _patternOffset);
        }

        sealed class Snapshot
        {
            public int Round, RngCalls, Energy, IntentIndex;
            public int PHp, PBlock, PStr, PWeak, PWeakRem;
            public int EHp, EBlock, EStr, EWeak, EBurn, EWeakRem, EMemRem;
            public string[] Deck, Hand, Discard;
        }

        void TakeSnapshot()
        {
            if (_snapshots.Count >= Round)
                _snapshots.RemoveRange(Round - 1, _snapshots.Count - (Round - 1));
            _snapshots.Add(new Snapshot
            {
                Round = Round, RngCalls = Rng.Calls, Energy = Energy, IntentIndex = _intentIndex,
                PHp = Player.Hp, PBlock = Player.Block, PStr = Player.Strength,
                PWeak = Player.Weak, PWeakRem = Player.WeakRemainder,
                EHp = Enemy.Hp, EBlock = Enemy.Block, EStr = Enemy.Strength,
                EWeak = Enemy.Weak, EBurn = Enemy.Burn, EWeakRem = Enemy.WeakRemainder,
                EMemRem = Enemy.MemoryRemainder,
                Deck = Deck.ToArray(), Hand = Hand.ToArray(), Discard = Discard.ToArray()
            });
        }

        void Restore(Snapshot s)
        {
            Round = s.Round; Energy = s.Energy; _intentIndex = s.IntentIndex;
            Rng.RestoreTo(s.RngCalls);
            Player.Hp = s.PHp; Player.Block = s.PBlock; Player.Strength = s.PStr;
            Player.Weak = s.PWeak; Player.WeakRemainder = s.PWeakRem;
            Enemy.Hp = s.EHp; Enemy.Block = s.EBlock; Enemy.Strength = s.EStr;
            Enemy.Weak = s.EWeak; Enemy.Burn = s.EBurn; Enemy.WeakRemainder = s.EWeakRem;
            Enemy.MemoryRemainder = s.EMemRem;
            Deck.Clear(); Deck.AddRange(s.Deck);
            Hand.Clear(); Hand.AddRange(s.Hand);
            Discard.Clear(); Discard.AddRange(s.Discard);
        }

        // ---------- 효과 ----------

        void ApplyCardEffect(EffectData e)
        {
            switch (e.type)
            {
                case "damage":
                    if (e.target == "enemy") DealPlayerDamage(e.amount);
                    else Player.Hp -= e.amount;
                    break;
                case "block": Player.Block += e.amount; break;
                case "strength": Player.Strength += e.amount; break;
                case "draw": DrawCards(e.amount); break;
                case "energy": Energy += e.amount; break;
                case "heal": Player.Hp = Math.Min(Player.MaxHp, Player.Hp + e.amount); break;
                case "weak": Enemy.Weak += e.amount; break;
                case "burn": Enemy.Burn += e.amount; break;
                case "rewind_charge": RewindCharges += e.amount; break;
                default: throw new NotSupportedException("모르는 카드 효과: " + e.type);
            }
        }

        /// 백분율 정수 + 나머지 누적. 부동소수를 한 번도 지나가지 않는다.
        static int ScalePct(int raw, int pct, ref int remainder)
        {
            int num = raw * pct + remainder;
            remainder = ((num % 100) + 100) % 100;
            return num / 100;
        }

        int DealPlayerDamage(int amount)
        {
            int raw = Math.Max(0, amount + Player.Strength);
            if (Player.Weak > 0) raw = ScalePct(raw, WeakDamagePct, ref Player.WeakRemainder);
            return AbsorbInto(Enemy, raw);
        }

        int DealEnemyDamage(int amount)
        {
            int raw = Math.Max(0, amount + Enemy.Strength);
            // 적의 기억이 되감기의 값을 되받는 자리.
            if (Memory > 0)
                raw = ScalePct(raw, 100 + EnemyDef.memoryDamagePctPerStack * Memory, ref Enemy.MemoryRemainder);
            if (Enemy.Weak > 0) raw = ScalePct(raw, WeakDamagePct, ref Enemy.WeakRemainder);
            return AbsorbInto(Player, raw);
        }

        static int AbsorbInto(Combatant target, int raw)
        {
            int absorbed = Math.Min(target.Block, raw);
            target.Block -= absorbed;
            int through = raw - absorbed;
            target.Hp -= through;
            return through;
        }

        /// <summary>점수용 미리보기. 상태를 바꾸지 않으므로 나머지 누적을 쓰지 않는다 (근사값이다).</summary>
        public int PreviewPlayerDamage(int amount)
        {
            int raw = Math.Max(0, amount + Player.Strength);
            if (Player.Weak > 0) raw = raw * WeakDamagePct / 100;
            return raw;
        }

        public int PreviewEnemyDamage(int amount)
        {
            int raw = Math.Max(0, amount + Enemy.Strength);
            if (Memory > 0) raw = raw * (100 + EnemyDef.memoryDamagePctPerStack * Memory) / 100;
            if (Enemy.Weak > 0) raw = raw * WeakDamagePct / 100;
            return raw;
        }

        /// 패턴에 있는 공격의 평균 피해. 의도를 못 본 상태에서 방어량을 정하는 근거다.
        public int AverageAttack()
        {
            int sum = 0, n = 0;
            foreach (var p in EnemyDef.pattern)
                if (p.type == "attack") { sum += p.amount; n++; }
            return n == 0 ? 0 : PreviewEnemyDamage(sum / n);
        }

        void DrawCards(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (Deck.Count == 0)
                {
                    if (Discard.Count == 0) return;
                    Deck.AddRange(Discard);
                    Discard.Clear();
                    Rng.Shuffle(Deck);
                }
                Hand.Add(Deck[0]);
                Deck.RemoveAt(0);
            }
        }

        void DrawTo(int size)
        {
            if (Hand.Count < size) DrawCards(size - Hand.Count);
        }

        void CheckOutcome()
        {
            if (Enemy.Dead) Outcome = BattleOutcome.PlayerWon;
            else if (Player.Dead) Outcome = BattleOutcome.PlayerLost;
        }

        // ---------- 한 전투를 끝까지 ----------

        public void RunToEnd(IAgent agent)
        {
            bool turnAlreadyStarted = false;
            int steps = 0;
            while (Outcome == BattleOutcome.InProgress)
            {
                if (++steps > StepGuard)
                {
                    Outcome = BattleOutcome.PlayerLost;
                    _log.Add("STEPGUARD");
                    return;
                }

                if (!turnAlreadyStarted) BeginPlayerTurn();
                turnAlreadyStarted = false;

                agent.TakePlayerTurn(this);
                EndPlayerTurn();
                if (Outcome != BattleOutcome.InProgress) break;

                int hpBefore = Player.Hp;
                EnemyTurn();

                if (Outcome != BattleOutcome.PlayerWon
                    && CanRewind
                    && agent.ShouldRewind(this, hpBefore - Player.Hp))
                {
                    Rewind();
                    // 스냅샷은 턴 시작 상태다. 여기서 다시 뽑으면 되감기가 공짜 드로우가 된다.
                    turnAlreadyStarted = true;
                }
            }
            _log.Add("result " + Outcome + " hp=" + Player.Hp + " rewinds=" + RewindsUsed);
        }

        public string Transcript()
        {
            var sb = new StringBuilder();
            foreach (var l in _log) sb.Append(l).Append('\n');
            return sb.ToString();
        }
    }
}
