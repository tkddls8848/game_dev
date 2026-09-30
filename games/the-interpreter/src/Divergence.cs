using System.Collections.Generic;
using Interp.Data;

namespace Interp.Sim
{
    /// <summary>역어 하나만 바꿨을 때 무엇이 달라졌는가.</summary>
    public sealed class Flip
    {
        public int Seed;
        public string BasePolicy;
        public string StageId;
        public string RoundId;
        public string FromRendering;
        public string ToRendering;
        public string FromRegister;
        public string ToRegister;
        public bool TreatyChanged;
        public bool EndingChanged;
        public bool MisreadChanged;
        public int ClausesChanged;
        public string BaseTreaty;
        public string FlippedTreaty;
        public string BaseEnding;
        public string FlippedEnding;
    }

    public sealed class DivergenceReport
    {
        public int Runs;
        public int FlipsTried;
        public int FlipsChangingTreaty;
        public int FlipsChangingEnding;
        public int FlipsChangingMisreadings;

        /// <summary>"한 단어가 조약을 바꾸는 **지점**" — (씨드, 정책, 마디) 로 센다.</summary>
        public HashSet<string> PointsChangingTreaty = new HashSet<string>();
        public HashSet<string> StagesChangingTreaty = new HashSet<string>();
        public List<Flip> Changed = new List<Flip>();
    }

    /// <summary>
    /// ★ 핵 검사기 1 「한 단어가 조약을 바꾼다」의 계산기.
    ///
    /// 바탕 정책으로 한 판을 돌리고, **그 판에서 실제로 나온 마디마다 역어 하나씩만** 바꿔 다시 돌린다.
    /// 나머지는 전부 그대로다 — 난수도 경로와 무관하게 씨드·마디 번호로만 굴리므로(SessionSim.Mood)
    /// 달라진 것이 있다면 그것은 바꾼 낱말 하나 때문이다.
    /// </summary>
    public static class Divergence
    {
        public static DivergenceReport Analyse(GameData d, IList<int> seeds, IList<IPolicy> policies,
                                               bool renderingMatters = true)
        {
            DivergenceReport rep = new DivergenceReport();
            foreach (int seed in seeds)
            {
                foreach (IPolicy p in policies)
                {
                    SessionResult baseRun = SessionSim.Run(d, seed, p, null, renderingMatters);
                    rep.Runs++;
                    string baseTreaty = baseRun.TreatySignature(d.Clauses);
                    string baseMisread = string.Join(",", baseRun.Standing) + "/" + string.Join(",", baseRun.Exposed);

                    foreach (TraceStep step in baseRun.Trace)
                    {
                        RoundDef round = FindRound(d, step.StageId, step.RoundId);
                        if (round == null) continue;
                        foreach (RenderDef alt in round.renderings)
                        {
                            if (alt.id == step.RenderingId) continue;
                            rep.FlipsTried++;
                            Dictionary<string, string> ov = new Dictionary<string, string> { { step.StageId, alt.id } };
                            SessionResult flipped = SessionSim.Run(d, seed, p, ov, renderingMatters);

                            string ft = flipped.TreatySignature(d.Clauses);
                            string fm = string.Join(",", flipped.Standing) + "/" + string.Join(",", flipped.Exposed);
                            bool treatyChanged = ft != baseTreaty;
                            bool endingChanged = flipped.EndingId != baseRun.EndingId;
                            bool misreadChanged = fm != baseMisread;

                            if (treatyChanged) rep.FlipsChangingTreaty++;
                            if (endingChanged) rep.FlipsChangingEnding++;
                            if (misreadChanged) rep.FlipsChangingMisreadings++;
                            if (!treatyChanged && !endingChanged && !misreadChanged) continue;

                            if (treatyChanged)
                            {
                                rep.PointsChangingTreaty.Add(seed + "|" + p.Id + "|" + step.StageId);
                                rep.StagesChangingTreaty.Add(step.StageId);
                            }

                            rep.Changed.Add(new Flip
                            {
                                Seed = seed,
                                BasePolicy = p.Id,
                                StageId = step.StageId,
                                RoundId = step.RoundId,
                                FromRendering = step.RenderingId,
                                ToRendering = alt.id,
                                FromRegister = step.Register,
                                ToRegister = alt.register,
                                TreatyChanged = treatyChanged,
                                EndingChanged = endingChanged,
                                MisreadChanged = misreadChanged,
                                ClausesChanged = CountClauseDiffs(d, baseRun, flipped),
                                BaseTreaty = baseTreaty,
                                FlippedTreaty = ft,
                                BaseEnding = baseRun.EndingId,
                                FlippedEnding = flipped.EndingId
                            });
                        }
                    }
                }
            }
            return rep;
        }

        public static int CountClauseDiffs(GameData d, SessionResult a, SessionResult b)
        {
            int n = 0;
            foreach (ClauseDef c in d.Clauses)
            {
                string va, vb;
                a.Clauses.TryGetValue(c.id, out va);
                b.Clauses.TryGetValue(c.id, out vb);
                if ((va ?? "") != (vb ?? "")) n++;
            }
            return n;
        }

        public static RoundDef FindRound(GameData d, string stageId, string roundId)
        {
            foreach (StageDef s in d.Stages)
            {
                if (s.id != stageId) continue;
                foreach (RoundDef r in s.rounds) if (r.id == roundId) return r;
            }
            return null;
        }
    }
}
