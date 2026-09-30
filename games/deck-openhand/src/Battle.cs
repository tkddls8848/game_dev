using System;
using System.Collections.Generic;
using System.Text;

namespace DeckOpenhand
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

        /// 백분율 정수 나눗셈에서 버려질 나머지. 누적해야 열 번 때린 총합이 맞는다.
        public int WeakRemainder;

        public bool Dead => Hp <= 0;
    }

    /// <summary>
    /// 전투 하나. 공개 덱 규칙이 여기 들어 있다.
    ///
    /// 핵심: <b>적의 다음 다섯 수가 늘 보인다.</b> 숨기는 것이 없다.
    /// 그래서 이 게임의 문제는 "무엇이 올까"가 아니라 <b>"어느 순서로 받을까"</b>다.
    /// 가림막으로 한 수를 지우고 역순으로 두 수를 바꾸는 것이 전부 그 문제를 푸는 수단이다.
    ///
    /// 적의 행동은 난수를 쓰지 않는다. 난수가 닿는 곳은 카드를 뽑는 순서 하나뿐이다.
    /// </summary>
    public sealed class Battle
    {
        public const int StepGuard = 400;
        public const int MaxPlaysPerTurn = 30;

        readonly GameData _data;
        readonly List<string> _log = new List<string>();

        /// 적의 예정 행동. 패턴 인덱스를 담는다. 카드가 여기서 지우거나 자리를 바꾼다.
        readonly List<int> _queue = new List<int>();
        int _fill;

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
        public int PeekWindow { get; }
        public int Round { get; private set; }
        public int MaxRounds { get; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;
        public int WeakDamagePct { get; }

        /// 카드가 적의 순서를 건드린 횟수. 규칙이 실제로 쓰이는지 재는 데 쓴다.
        public int IntentsSkipped { get; private set; }
        public int IntentsSwapped { get; private set; }

        public IReadOnlyList<string> Log => _log;

        public Battle(GameData data, EnemyData enemyDef, IEnumerable<string> deck,
                      int playerHp, int playerMaxHp, int seed,
                      int handSize, int energyPerTurn, int peekWindow, int maxRounds)
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
            PeekWindow = peekWindow;
            MaxRounds = maxRounds;
            WeakDamagePct = data.Balance.weakDamagePct;

            Refill();
            Deck.AddRange(deck);
            Rng.Shuffle(Deck);
            Round = 0;
            _log.Add("battle " + enemyDef.id + " seed=" + seed + " hp=" + playerHp + " deck=" + Deck.Count);
        }

        // ---------- 공개된 순서 ----------

        /// 대기열이 마르지 않게 패턴을 순환시켜 채운다. 미리 보는 창보다 넉넉히 둔다.
        void Refill()
        {
            int want = PeekWindowSafe + 4;
            while (_queue.Count < want)
            {
                _queue.Add(_fill);
                _fill = (_fill + 1) % EnemyDef.pattern.Length;
            }
        }

        int PeekWindowSafe => PeekWindow > 0 ? PeekWindow : 1;

        /// <summary>
        /// 앞으로 오는 수를 순서대로 돌려준다. <b>이 게임이 아무것도 숨기지 않는다는 것의 구현이다.</b>
        /// </summary>
        public EffectData[] PeekIntents(int count)
        {
            var result = new EffectData[count];
            for (int i = 0; i < count; i++) result[i] = EnemyDef.pattern[_queue[i]];
            return result;
        }

        public EffectData NextIntent => EnemyDef.pattern[_queue[0]];

        /// 보이는 창 안에 있는 공격의 개수. `echo` 효과와 탐욕 정책이 쓴다.
        public int AttacksInWindow
        {
            get
            {
                int n = 0;
                var window = PeekIntents(PeekWindowSafe);
                foreach (var i in window) if (i.type == "attack") n++;
                return n;
            }
        }

        // ---------- 턴 진행 ----------

        public void BeginPlayerTurn()
        {
            Round++;
            Player.Block = 0;
            Energy = EnergyPerTurn;
            DrawTo(HandSize);
            _log.Add("T" + Round + " begin hp=" + Player.Hp + " ehp=" + Enemy.Hp
                     + " eblk=" + Enemy.Block + " hand=" + string.Join(",", Hand)
                     + " peek=" + PeekString());
        }

        public string PeekString()
        {
            var sb = new StringBuilder();
            var w = PeekIntents(PeekWindowSafe);
            for (int i = 0; i < w.Length; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(w[i].type).Append(':').Append(w[i].amount);
            }
            return sb.ToString();
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

            var intent = NextIntent;
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
            _queue.RemoveAt(0);
            Refill();
            CheckOutcome();
            if (Outcome == BattleOutcome.InProgress && Round >= MaxRounds)
            {
                Outcome = BattleOutcome.PlayerLost;
                _log.Add("  timeout at T" + Round);
            }
        }

        // ---------- 순서를 건드리는 효과 ----------

        /// <summary>앞의 n 수를 지운다. 일어나지 않은 것으로 치고 그 뒤가 앞으로 온다.</summary>
        void SkipIntents(int n)
        {
            for (int i = 0; i < n && _queue.Count > 0; i++)
            {
                _log.Add("    skip " + EnemyDef.pattern[_queue[0]].type
                         + ":" + EnemyDef.pattern[_queue[0]].amount);
                _queue.RemoveAt(0);
                IntentsSkipped++;
            }
            Refill();
        }

        /// <summary>앞의 두 수를 맞바꾼다. n 은 몇 번 바꿀지가 아니라 몇 칸 뒤까지인지다.</summary>
        void SwapIntents(int n)
        {
            for (int i = 0; i < n && i + 1 < _queue.Count; i++)
            {
                (_queue[i], _queue[i + 1]) = (_queue[i + 1], _queue[i]);
                IntentsSwapped++;
            }
            _log.Add("    swap -> " + PeekString());
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

                // 다음 수가 공격이면 그 값만큼 막고, 아니면 amount 만 막는다.
                // 순서를 읽는 것이 곧바로 수치가 되는 자리다.
                case "brace":
                    Player.Block += e.amount + BraceValue();
                    break;

                // 보이는 창 안의 공격 하나마다 amount 씩 때린다. 운명을 읽어 값으로 바꾼다.
                case "echo":
                    DealPlayerDamage(e.amount * AttacksInWindow);
                    break;

                case "skip_intent": SkipIntents(e.amount); break;
                case "swap_intent": SwapIntents(e.amount); break;

                default: throw new NotSupportedException("모르는 카드 효과: " + e.type);
            }
        }

        public int BraceValue()
        {
            var next = NextIntent;
            return next.type == "attack" ? PreviewEnemyDamage(next.amount) : 0;
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
            if (Enemy.Weak > 0) raw = raw * WeakDamagePct / 100;
            return raw;
        }

        /// 패턴에 있는 공격의 평균 피해. 순서를 <b>안 보는</b> 대조군 정책이 쓴다.
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
            int steps = 0;
            while (Outcome == BattleOutcome.InProgress)
            {
                if (++steps > StepGuard)
                {
                    Outcome = BattleOutcome.PlayerLost;
                    _log.Add("STEPGUARD");
                    return;
                }
                BeginPlayerTurn();
                agent.TakePlayerTurn(this);
                EndPlayerTurn();
                if (Outcome != BattleOutcome.InProgress) break;
                EnemyTurn();
            }
            _log.Add("result " + Outcome + " hp=" + Player.Hp
                     + " skipped=" + IntentsSkipped + " swapped=" + IntentsSwapped);
        }

        public string Transcript()
        {
            var sb = new StringBuilder();
            foreach (var l in _log) sb.Append(l).Append('\n');
            return sb.ToString();
        }
    }
}
