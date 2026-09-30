using System;
using System.Collections.Generic;
using CurseLedger.Data;

namespace CurseLedger.Sim
{
    /// <summary>회차 하나의 결말. 검사기가 보는 것은 전부 여기 있다.</summary>
    public sealed class LedgerRun
    {
        public Ending Ending;
        public int GenerationsSurvived;
        public bool VillageAlive;
        public int Prosperity, Binding, Wrath, HouseVitality, Resentment, ReleaseProgress, Coffers;
        public List<VictimRecord> Victims;
        public List<LedgerLine> Lines;
        public List<Bill> UnpaidBillsLeft;
        public string[] Policy;
        public int Seed;
        public int Score;

        public int VictimCount { get { return Victims.Count; } }

        /// <summary>★ 이것이 있으면 딜레마가 아니다 — 마을도 살고 아무도 희생되지 않은 결말.</summary>
        public bool CleanExit { get { return VillageAlive && Victims.Count == 0; } }

        public int DistinctRites
        {
            get
            {
                HashSet<string> s = new HashSet<string>();
                foreach (string r in Policy) if (r != null) s.Add(r);
                return s.Count;
            }
        }

        public string EndingKorean { get { return Endings.Korean(Ending); } }

        public string PolicyText { get { return string.Join(" > ", Policy); } }
    }

    /// <summary>
    /// 저주를 한 대씩 운영한다.
    ///
    /// **한 대의 순서가 이 PoC의 전부다:**
    ///   (1) 앞 세대의 청구서가 도착한다 (내가 고른 것이 아니다)
    ///   (2) 구덩이가 이번 대의 공물을 요구한다
    ///   (3) 내가 제례를 고른다
    ///   (4) 미납의 값이 즉시 붙는다
    ///   (5) 속박이 주는 풍요와 노여움이 주는 재앙이 정산된다
    ///   (6) 내가 고른 것의 대부분은 **청구서로 미래에 놓인다** — 지금 보이지 않는다
    ///
    /// 씨드가 정하는 것(흉년/풍년 · 그 대에 마을에 있는 사람 순서)은 **정책과 무관하게**
    /// 생성자에서 미리 뽑는다. 그러지 않으면 정책마다 난수 호출 횟수가 달라져
    /// 정책 비교가 무의미해진다.
    /// </summary>
    public sealed class CurseSim
    {
        private readonly GameData _d;
        private readonly int _seed;
        private readonly int _generations;
        private readonly int[] _leanYearPercent;      // 대마다 요구를 흔드는 % (풍년은 음수)
        private readonly List<VillagerDef> _elderOrder;
        private readonly List<VillagerDef> _youngOrder;

        /// <summary>지연을 끄면(false) 모든 효과가 결정한 대에 즉시 붙는다 — DeferredCostMatters 의 대조군.</summary>
        public bool DeferralOn { get; private set; }

        public CurseSim(GameData d, int seed, int generations, bool deferralOn = true)
        {
            _d = d;
            _seed = seed;
            _generations = generations;
            DeferralOn = deferralOn;

            Random rng = new Random(seed);
            int span = d.Curse.demand.leanYearSpanPercent;
            _leanYearPercent = new int[generations + 2];
            for (int g = 1; g <= generations + 1; g++) _leanYearPercent[g] = rng.Next(-span, span + 1);

            _elderOrder = Shuffle(d.TierRoster(Tiers.Elder), rng);
            _youngOrder = Shuffle(d.TierRoster(Tiers.Young), rng);
        }

        public GameData Data { get { return _d; } }
        public int Seed { get { return _seed; } }
        public int Generations { get { return _generations; } }
        public int LeanYearPercent(int g) { return _leanYearPercent[g]; }
        public IList<VillagerDef> ElderOrder { get { return _elderOrder; } }
        public IList<VillagerDef> YoungOrder { get { return _youngOrder; } }

        private static List<VillagerDef> Shuffle(List<VillagerDef> src, Random rng)
        {
            List<VillagerDef> list = new List<VillagerDef>(src);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                VillagerDef t = list[i]; list[i] = list[j]; list[j] = t;
            }
            return list;
        }

        /// <summary>이 대에 이 제례를 고를 수 있는가. 고를 수 없는 수는 정책 공간에서 빠진다.</summary>
        public bool Available(LedgerState s, RiteDef r)
        {
            if (r.requiresCoffers > 0 && s.Coffers < r.requiresCoffers) return false;
            if (r.TakesVictim && NextVictim(s, r.requiresVictimTier) == null) return false;
            return true;
        }

        public VillagerDef NextVictim(LedgerState s, string tier)
        {
            IList<VillagerDef> order = tier == Tiers.Elder ? _elderOrder : _youngOrder;
            foreach (VillagerDef v in order) if (!s.TakenVillagers.Contains(v.id)) return v;
            return null;
        }

        public LedgerState NewState()
        {
            StartState st = _d.Curse.start;
            LedgerState s = new LedgerState
            {
                Generation = 0,
                Prosperity = st.prosperity, Binding = st.binding, Wrath = st.wrath,
                HouseVitality = st.houseVitality, Resentment = st.resentment,
                ReleaseProgress = st.releaseProgress, Coffers = st.coffers
            };
            // 앞 세대가 남긴 청구서. **1대가 시작되기도 전에 이미 놓여 있다.**
            foreach (PendingDef p in _d.Curse.inheritedPending)
                s.Bills.Add(Bill.From(p, DeferralOn ? p.atGeneration : 1, -1, p.from));
            return s;
        }

        /// <summary>
        /// 이번 대의 공물 요구. 노여움과 앞 세대가 붙인 이자, 미룬 공물, 그해의 흉년이 얹힌다.
        /// **상태를 건드린다** — 나머지를 누적하고 미룬 공물(DemandOnce)을 비운다. 한 대에 한 번만 부른다.
        /// </summary>
        public int DemandOf(LedgerState s, int generation)
        {
            DemandCurve c = _d.Curse.demand;
            int raw = c.baseAmount + (generation - 1) * c.growthPerGeneration + s.DemandBonus + s.DemandOnce;
            s.DemandOnce = 0;
            int carry = s.DemandCarry;
            raw += Ratio.Scale(s.Wrath, c.wrathPercent, ref carry);
            raw += Ratio.Scale(raw, _leanYearPercent[generation], ref carry);
            s.DemandCarry = carry;
            return raw < 0 ? 0 : raw;
        }

        /// <summary>한 대를 돌린다. 고를 수 없는 제례를 주면 false 를 돌려준다.</summary>
        public bool Step(LedgerState s, string riteId)
        {
            if (s.Ended) return false;
            RiteDef r = _d.Rite(riteId);
            if (!Available(s, r)) return false;

            int g = ++s.Generation;
            LedgerLine line = new LedgerLine { Generation = g, RiteId = r.id, RiteName = r.name };
            HeirDef heir = _d.HeirOfGeneration(g);
            line.HeirId = heir.id; line.HeirName = heir.name;

            // (1) 앞 세대의 청구서가 도착한다
            ArriveBills(s, g, line);
            if (CheckEnding(s))
            {
                // 청구서가 도착한 순간 끝났다. 이 대는 제례를 올리지 못한다 —
                // **앞 세대의 결정이 이번 대의 선택을 아예 지운 것이다.**
                line.Aborted = true;
                Close(s, line);
                return true;
            }

            // (2) 구덩이가 요구한다
            line.LeanYear = _leanYearPercent[g] > 0;
            int demand = DemandOf(s, g);
            line.DemandDue = demand;

            // (3) 제례
            int carry = s.DemandCarry;
            int paid = Ratio.Scale(demand, r.payPercent, ref carry);
            s.DemandCarry = carry;
            line.Paid = paid;

            Apply(s, Bill.From(r.immediate, g, g, r.id));
            s.ReleaseProgress += r.releaseProgress;

            if (r.TakesVictim) TakeVictim(s, r, g, heir, line);

            // (6) 내가 고른 것의 나머지는 청구서로 미래에 놓인다
            foreach (PendingDef p in r.deferred)
            {
                int at = DeferralOn ? g + p.delayGenerations : g;
                Bill b = Bill.From(p, at, g, r.id);
                if (DeferralOn) { s.Bills.Add(b); line.BillsScheduled.Add(b); }
                else { Apply(s, b); line.BillsArrived.Add(b); }
            }

            // (4) 미납의 값
            int unpaid = demand - paid;
            if (unpaid > 0)
            {
                line.Unpaid = unpaid;
                if (r.deferUnpaidGenerations >= 0)
                {
                    // 미룬 공물은 사라지지 않는다. 다음 대의 요구에 그대로 얹힌다 —
                    // 구덩이는 노하지 않고 **기다린다.** 그게 이 수가 죽은 선택지가 아닌 이유다.
                    int at = DeferralOn ? g + r.deferUnpaidGenerations : g + 1;
                    int c2 = s.DemandCarry;
                    int carriedAmount = Ratio.Scale(unpaid, _d.Balance.flow.postponeInterestPercent, ref c2);
                    s.DemandCarry = c2;
                    Bill carried = new Bill
                    {
                        AtGeneration = at, FromGeneration = g, Source = r.id,
                        Note = Localization.Text("bill.carried", "미룬 공물이 불어서 이 대의 요구에 얹힌다"),
                        DemandOnce = carriedAmount
                    };
                    line.Postponed = carriedAmount;
                    s.Bills.Add(carried);
                    line.BillsScheduled.Add(carried);
                }
                else
                {
                    int c1 = s.BindingCarry;
                    s.Wrath += Ratio.Scale(unpaid, _d.Balance.flow.wrathPerUnpaidPercent, ref c1);
                    s.Binding -= Ratio.Scale(unpaid, _d.Balance.flow.bindingLossPerUnpaidPercent, ref c1);
                    s.BindingCarry = c1;
                }
            }
            else if (unpaid < 0)
            {
                int c1 = s.BindingCarry;
                s.Binding += Ratio.Scale(-unpaid, _d.Balance.flow.surplusBindingPercent, ref c1);
                s.BindingCarry = c1;
            }

            // (5) 속박이 주는 풍요, 노여움이 주는 재앙
            FlowBalance f = _d.Balance.flow;
            int pc = s.ProsperityCarry;
            int gain = Ratio.Scale(s.Binding, f.blessingPerBindingPercent, ref pc);
            int blight = Ratio.Scale(s.Wrath, f.blightPerWrathPercent, ref pc);
            s.ProsperityCarry = pc;
            s.Prosperity += gain - blight - f.prosperityDecayPerGeneration;
            s.Resentment -= f.resentmentEasePerGeneration;

            Clamp(s);
            CheckEnding(s);
            if (!s.Ended && g >= _generations) HandDown(s, line);
            Close(s, line);
            return true;
        }

        private void ArriveBills(LedgerState s, int g, LedgerLine line)
        {
            for (int i = 0; i < s.Bills.Count; i++)
            {
                if (s.Bills[i].AtGeneration > g) continue;
                Bill b = s.Bills[i];
                s.Bills.RemoveAt(i);
                i--;
                Apply(s, b);
                line.BillsArrived.Add(b);
            }
            Clamp(s);
        }

        private void Apply(LedgerState s, Bill b)
        {
            s.Wrath += b.Wrath;
            s.Binding += b.Binding;
            s.Prosperity += b.Prosperity;
            s.HouseVitality += b.HouseVitality;
            s.Resentment += b.Resentment;
            s.DemandBonus += b.DemandBonus;
            s.DemandOnce += b.DemandOnce;
            s.Coffers += b.Coffers;
        }

        private void TakeVictim(LedgerState s, RiteDef r, int g, HeirDef heir, LedgerLine line)
        {
            VillagerDef v = NextVictim(s, r.requiresVictimTier);
            s.TakenVillagers.Add(v.id);
            VictimRecord rec = new VictimRecord
            {
                VillagerId = v.id, Name = v.name, Age = v.age, Tier = v.tier,
                Household = v.household, Note = v.note,
                Generation = g, HeirId = heir.id, RiteId = r.id,
                LedgerLine = s.Victims.Count + 1
            };
            s.Victims.Add(rec);
            line.VictimIds.Add(v.id);
        }

        /// <summary>
        /// 마지막 대를 넘겼다. **남은 청구서는 후손이 치른다** — 이 PoC의 마지막 한 수다.
        /// 마지막 결정의 값을 보지 않고 끝나면 지연이 거짓이 된다.
        /// </summary>
        private void HandDown(LedgerState s, LedgerLine line)
        {
            foreach (Bill b in s.Bills) { Apply(s, b); line.BillsArrived.Add(b); }
            s.Bills.Clear();

            // 미뤄 둔 공물은 **사라지지 않는다.** 장부를 물려받은 후손이 그것을 떠안고,
            // 구덩이는 그 대에 노한다. 이 여섯 줄이 없으면 마지막 몇 대를 덮어 두는 것이
            // 공짜가 된다 — 목업을 브라우저로 열어 보다가 8대에 공물 721이 미납으로 쌓인 채
            // 아무 값도 치르지 않는 것을 보고 찾았다.
            if (s.DemandOnce > 0)
            {
                line.Unpaid += s.DemandOnce;
                int c = s.BindingCarry;
                s.Wrath += Ratio.Scale(s.DemandOnce, _d.Balance.flow.wrathPerUnpaidPercent, ref c);
                s.Binding -= Ratio.Scale(s.DemandOnce, _d.Balance.flow.bindingLossPerUnpaidPercent, ref c);
                s.BindingCarry = c;
                s.DemandOnce = 0;
            }

            Clamp(s);
            if (!CheckEnding(s)) s.Ending = Ending.PassedOn;
        }

        private void Clamp(LedgerState s)
        {
            LimitBalance l = _d.Balance.limits;
            if (s.Prosperity > l.prosperityMax) s.Prosperity = l.prosperityMax;
            if (s.Binding > l.bindingMax) s.Binding = l.bindingMax;
            if (s.Wrath > l.wrathMax) s.Wrath = l.wrathMax;
            if (s.Wrath < 0) s.Wrath = 0;
            if (s.HouseVitality > l.houseVitalityMax) s.HouseVitality = l.houseVitalityMax;
            if (s.Resentment > l.resentmentMax) s.Resentment = l.resentmentMax;
            if (s.Resentment < 0) s.Resentment = 0;
            if (s.Coffers > l.coffersMax) s.Coffers = l.coffersMax;
            if (s.Coffers < 0) s.Coffers = 0;
            if (s.ReleaseProgress < 0) s.ReleaseProgress = 0;
        }

        private bool CheckEnding(LedgerState s)
        {
            if (s.Ended) return true;
            EndingBalance e = _d.Balance.ending;
            // 해제를 가장 먼저 본다. 의식이 끝났으면 그것이 결말의 이름이고,
            // 번영이 이미 기울어 있었어도 **기근이 마지막 낙차**로 얹힌다.
            if (s.ReleaseProgress >= e.releaseComplete)
            {
                s.Prosperity -= e.releaseFamineDrop;
                s.Ending = Ending.CurseLifted;
                return true;
            }
            if (s.Prosperity <= 0) { s.Ending = Ending.VillageRuin; return true; }
            if (s.HouseVitality <= 0) { s.Ending = Ending.HouseExtinct; return true; }
            if (s.Resentment >= e.uprisingResentment) { s.Ending = Ending.Uprising; return true; }
            if (s.Binding <= 0) { s.Ending = Ending.CurseUnbound; return true; }
            return false;
        }

        private void Close(LedgerState s, LedgerLine line)
        {
            line.Prosperity = s.Prosperity; line.Binding = s.Binding; line.Wrath = s.Wrath;
            line.HouseVitality = s.HouseVitality; line.Resentment = s.Resentment;
            line.ReleaseProgress = s.ReleaseProgress; line.Coffers = s.Coffers;
            s.Lines.Add(line);
        }

        /// <summary>정책 하나를 끝까지 돌린다. 고를 수 없는 제례가 나오면 null 을 돌려준다.</summary>
        public LedgerRun Run(IList<string> policy)
        {
            LedgerState s = NewState();
            List<string> taken = new List<string>();
            for (int i = 0; i < policy.Count && !s.Ended; i++)
            {
                if (!Step(s, policy[i])) return null;
                taken.Add(policy[i]);
            }
            return Settle(s, taken.ToArray());
        }

        public LedgerRun Settle(LedgerState s, string[] policy)
        {
            if (!s.Ended) s.Ending = s.Generation >= _generations ? Ending.PassedOn : Ending.Stalled;
            EndingBalance e = _d.Balance.ending;
            // 해제 완수를 **배제하지 않는다.** 기근의 낙차를 번영이 견디면 살아남은 것으로 센다.
            // 그렇게 두어야 "저주를 풀면 마을이 몰락한다"가 규칙이 아니라 **수치의 결과**가 된다.
            bool alive = s.Prosperity >= e.villageAliveFloor
                         && s.Ending != Ending.VillageRuin
                         && s.Ending != Ending.Uprising
                         && s.Ending != Ending.CurseUnbound
                         && s.Ending != Ending.HouseExtinct
                         && s.Ending != Ending.Stalled;
            LedgerRun run = new LedgerRun
            {
                Ending = s.Ending,
                GenerationsSurvived = s.Generation,
                VillageAlive = alive,
                Prosperity = s.Prosperity, Binding = s.Binding, Wrath = s.Wrath,
                HouseVitality = s.HouseVitality, Resentment = s.Resentment,
                ReleaseProgress = s.ReleaseProgress, Coffers = s.Coffers,
                Victims = s.Victims, Lines = s.Lines, UnpaidBillsLeft = s.Bills,
                Policy = policy, Seed = _seed
            };
            run.Score = ScoreOf(run);
            return run;
        }

        /// <summary>
        /// ★ 검사기의 잣대일 뿐이고 게임의 도덕이 아니다 —
        /// 이 PoC는 "몇 점인가"를 묻지 않는다. NoDominantStrategy 가 정책을 견주는 데만 쓴다.
        /// </summary>
        public int ScoreOf(LedgerRun r)
        {
            ScoreBalance w = _d.Balance.score;
            int score = r.GenerationsSurvived * w.perSurvivedGeneration
                        + r.Prosperity * w.perProsperity
                        + r.HouseVitality * w.perHouseVitality
                        + r.VictimCount * w.perVictim
                        + r.Resentment * w.perResentment;
            if (r.VillageAlive) score += w.villageAliveBonus;
            return score;
        }
    }
}
