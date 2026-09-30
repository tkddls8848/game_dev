using System;
using System.Collections.Generic;
using CurseLedger.Data;

namespace CurseLedger.Sim
{
    /// <summary>정책 공간을 훑은 결과. NoCleanExit 의 증거가 전부 여기 있다.</summary>
    public sealed class SweepResult
    {
        public int Seed;
        public int Generations;
        public int PoliciesExplored;
        public int RunsSettled;
        /// <summary>내려간 마디 수. 잎(결말)보다 많다 — 훑은 넓이를 정직하게 적기 위해 센다.</summary>
        public int NodesVisited;
        public int DeepestGeneration;
        public List<string> AllowedRites = new List<string>();

        /// <summary>나온 결말의 종류. BothExtremesFail 이 두 극단의 집합을 견준다.</summary>
        public HashSet<Ending> EndingKinds = new HashSet<Ending>();
        public Dictionary<Ending, int> EndingCounts = new Dictionary<Ending, int>();
        public List<LedgerRun> Survivors = new List<LedgerRun>();

        /// <summary>저주가 실제로 풀린 회차들. 기근을 견딘 것이 하나라도 있으면 전제가 무너진다.</summary>
        public List<LedgerRun> LiftedRuns = new List<LedgerRun>();

        /// <summary>훑은 정책 전부. DeferredCostMatters 가 이것을 두 세계에 그대로 넣는다.</summary>
        public List<string[]> Policies = new List<string[]>();

        /// <summary>끝까지 여덟 대를 결정한 정책만. 두 세계 비교는 이것으로 한다 — 길이가 같아야 견준다.</summary>
        public List<string[]> FullLengthPolicies = new List<string[]>();

        /// <summary>마을이 살아 있고 **아무도 바치지 않은** 결말. 이게 하나라도 있으면 딜레마가 아니다.</summary>
        public List<LedgerRun> CleanExits = new List<LedgerRun>();

        /// <summary>마을이 살아 있지만 이름이 지워진 결말. 이게 없으면 그냥 못 이기는 게임이다.</summary>
        public List<LedgerRun> AliveWithVictims = new List<LedgerRun>();

        /// <summary>아무도 바치지 않은 정책 전부의 결말 분포. "왜 못 하는가"를 수치로 보여 준다.</summary>
        public Dictionary<Ending, int> BloodlessEndings = new Dictionary<Ending, int>();
        public int BloodlessRuns;

        /// <summary>희생자 수별로 가장 좋은(잣대 기준) 결말.</summary>
        public Dictionary<int, LedgerRun> BestByVictimCount = new Dictionary<int, LedgerRun>();

        public LedgerRun Best;

        /// <summary>아무도 바치지 않은 정책 중 가장 오래 버틴 것 — 깨끗한 출구에 가장 가까운 시도.</summary>
        public LedgerRun BloodlessBest;

        public void Note(LedgerRun r)
        {
            RunsSettled++;
            EndingKinds.Add(r.Ending);
            int en;
            EndingCounts.TryGetValue(r.Ending, out en);
            EndingCounts[r.Ending] = en + 1;
            if (r.GenerationsSurvived > DeepestGeneration) DeepestGeneration = r.GenerationsSurvived;
            if (r.VillageAlive) Survivors.Add(r);
            if (r.Ending == Ending.CurseLifted) LiftedRuns.Add(r);
            Policies.Add(r.Policy);
            if (r.Policy.Length == Generations) FullLengthPolicies.Add(r.Policy);
            if (Best == null || r.Score > Best.Score) Best = r;

            LedgerRun cur;
            if (!BestByVictimCount.TryGetValue(r.VictimCount, out cur) || r.Score > cur.Score)
                BestByVictimCount[r.VictimCount] = r;

            if (r.VictimCount == 0)
            {
                BloodlessRuns++;
                int n;
                BloodlessEndings.TryGetValue(r.Ending, out n);
                BloodlessEndings[r.Ending] = n + 1;
                if (BloodlessBest == null || r.Score > BloodlessBest.Score) BloodlessBest = r;
                if (r.VillageAlive) CleanExits.Add(r);
            }
            else if (r.VillageAlive)
            {
                AliveWithVictims.Add(r);
            }
        }
    }

    /// <summary>
    /// ★ NoCleanExit 의 엔진.
    ///
    /// 정책 공간을 **전수로** 훑는다 — 대마다 고를 수 있는 제례 전부로 갈라지며 내려간다.
    /// 중간에 끝난 회차는 더 내려가지 않고 그 자리에서 결말로 센다(그 아래는 존재하지 않는 미래다).
    /// 고를 수 없는 제례(곳간 부족 · 바칠 사람 없음)도 가지에서 빠진다.
    ///
    /// 표본이 아니라 전수인 이유: "깨끗한 출구가 없다"는 **없음의 주장**이라 표본으로는 못 보인다.
    /// </summary>
    public static class PolicySweep
    {
        public static SweepResult Exhaustive(GameData d, int seed, int generations)
        {
            CurseSim sim = new CurseSim(d, seed, generations);
            SweepResult res = new SweepResult { Seed = seed, Generations = generations };
            List<RiteDef> rites = new List<RiteDef>(d.Rites.rites);
            foreach (RiteDef r in rites) res.AllowedRites.Add(r.id);
            string[] path = new string[generations];
            Descend(sim, sim.NewState(), rites, path, 0, generations, res);
            return res;
        }

        /// <summary>제례 일부만 허용해 훑는다 (예: 사람을 바치는 수를 빼고 전수 확인).</summary>
        public static SweepResult Restricted(GameData d, int seed, int generations, ICollection<string> allowed)
        {
            CurseSim sim = new CurseSim(d, seed, generations);
            SweepResult res = new SweepResult { Seed = seed, Generations = generations };
            List<RiteDef> rites = new List<RiteDef>();
            foreach (RiteDef r in d.Rites.rites) if (allowed.Contains(r.id)) rites.Add(r);
            foreach (RiteDef r in rites) res.AllowedRites.Add(r.id);
            string[] path = new string[generations];
            Descend(sim, sim.NewState(), rites, path, 0, generations, res);
            return res;
        }

        private static void Descend(CurseSim sim, LedgerState s, List<RiteDef> rites,
                                    string[] path, int depth, int generations, SweepResult res)
        {
            res.NodesVisited++;
            if (s.Ended || depth >= generations)
            {
                string[] taken = new string[depth];
                Array.Copy(path, taken, depth);
                res.PoliciesExplored++;
                res.Note(sim.Settle(s, taken));
                return;
            }

            bool any = false;
            foreach (RiteDef r in rites)
            {
                if (!sim.Available(s, r)) continue;
                any = true;
                LedgerState next = Clone(s);
                path[depth] = r.id;
                sim.Step(next, r.id);
                Descend(sim, next, rites, path, depth + 1, generations, res);
            }
            // 고를 수 있는 제례가 하나도 없다 — 장부가 멈춘 자리다. 그것도 결말로 센다.
            if (!any)
            {
                string[] taken = new string[depth];
                Array.Copy(path, taken, depth);
                res.PoliciesExplored++;
                res.Note(sim.Settle(Clone(s), taken));
            }
        }

        /// <summary>
        /// 상태를 깊이 복사한다. 전수 훑기에서 형제 가지가 서로를 오염시키면
        /// 검사기가 거짓말을 한다 — 여기가 이 파일에서 가장 조심할 자리다.
        /// </summary>
        public static LedgerState Clone(LedgerState s)
        {
            LedgerState c = new LedgerState
            {
                Generation = s.Generation,
                Prosperity = s.Prosperity, Binding = s.Binding, Wrath = s.Wrath,
                HouseVitality = s.HouseVitality, Resentment = s.Resentment,
                ReleaseProgress = s.ReleaseProgress, Coffers = s.Coffers,
                DemandBonus = s.DemandBonus, DemandOnce = s.DemandOnce, Ending = s.Ending,
                ProsperityCarry = s.ProsperityCarry, BindingCarry = s.BindingCarry,
                DemandCarry = s.DemandCarry
            };
            c.Victims.AddRange(s.Victims);
            c.Bills.AddRange(s.Bills);
            c.Lines.AddRange(s.Lines);
            foreach (string id in s.TakenVillagers) c.TakenVillagers.Add(id);
            return c;
        }

        /// <summary>한 제례를 여덟 대 되풀이하는 정책. NoDominantStrategy 가 이것들을 견준다.</summary>
        public static List<string> Mono(string riteId, int generations)
        {
            List<string> p = new List<string>();
            for (int i = 0; i < generations; i++) p.Add(riteId);
            return p;
        }
    }

    /// <summary>
    /// 결정의 값이 **언제** 오는가를 잰다. 이 PoC의 기제가 장식이 아니라는 첫 증거다.
    /// </summary>
    public static class ConsequenceWeight
    {
        /// <summary>제례 하나의 총량 중 지연으로 오는 몫(%).</summary>
        public static int DeferredPercent(RiteDef r)
        {
            int now = Bill.From(r.immediate, 0, 0, r.id).Magnitude;
            int later = 0;
            foreach (PendingDef p in r.deferred) later += Bill.From(p, 0, 0, r.id).Magnitude;
            int total = now + later;
            if (total == 0) return 0;
            return later * 100 / total;
        }

        public static int LongestDelay(RiteDef r)
        {
            int max = 0;
            foreach (PendingDef p in r.deferred) if (p.delayGenerations > max) max = p.delayGenerations;
            return max;
        }
    }
}
