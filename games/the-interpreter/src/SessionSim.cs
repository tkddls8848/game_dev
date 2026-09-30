using System;
using System.Collections.Generic;
using System.Text;
using Interp.Data;

namespace Interp.Sim
{
    /// <summary>한 마디에서 실제로 일어난 일. 화면과 검사기가 같은 것을 본다.</summary>
    public sealed class TraceStep
    {
        public int StageIndex;
        public string StageId;
        public string RoundId;
        public string Speaker;
        public string Listener;
        public string SourceText;
        public string RenderingId;
        public string Register;
        public string RenderedText;
        public string HeardAs;
        public string TopicClause;
        public string SetClause = "";
        public string SetVariant = "";
        public string SeededMisread = "";
        public string ExposedMisread = "";
        public int Mood;
        public int TensionAfter;
        public int TrustAAfter;
        public int TrustBAfter;
        public int SuspicionAfter;
    }

    /// <summary>회담 한 판의 결과. 결말은 "누가 이겼나"가 아니라 이 덩어리 전체다.</summary>
    public sealed class SessionResult
    {
        public int Seed;
        public string PolicyName;
        public bool Signed;
        public bool Collapsed;
        public string CollapsedAtStage = "";
        public int Tension;
        public int TrustA;
        public int TrustB;
        public int Suspicion;

        /// <summary>조항 id → 굳은 변형 id. "" 면 비워 둔 채 끝났다.</summary>
        public Dictionary<string, string> Clauses = new Dictionary<string, string>();
        public List<string> Standing = new List<string>();   // 끝까지 남은 오해
        public List<string> Exposed = new List<string>();    // 도중에 드러난 오역
        public List<TraceStep> Trace = new List<TraceStep>();

        public int PaperFavorA;   // 조문에 적힌 대로의 이익
        public int PaperFavorB;
        public int FavorA;        // 남은 오해를 깎고 난 실익
        public int FavorB;
        public int UnsetClauses;
        public string EndingId = "";

        /// <summary>조약문을 한 줄로. 두 결과가 "같은 조약인가"를 이것으로 견준다.</summary>
        public string TreatySignature(IList<ClauseDef> order)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ClauseDef c in order)
            {
                string v;
                sb.Append(c.id).Append('=').Append(Clauses.TryGetValue(c.id, out v) ? v : "-").Append(';');
            }
            sb.Append(Signed ? "SIGNED" : "UNSIGNED");
            return sb.ToString();
        }

        /// <summary>결과 전체를 한 줄로. 씨드 재현성은 이것으로 본다.</summary>
        public string FullSignature(IList<ClauseDef> order)
        {
            StringBuilder sb = new StringBuilder(TreatySignature(order));
            sb.Append("|T").Append(Tension).Append("|A").Append(TrustA).Append("|B").Append(TrustB)
              .Append("|S").Append(Suspicion).Append("|fa").Append(FavorA).Append("|fb").Append(FavorB)
              .Append("|e").Append(EndingId).Append('|');
            foreach (string m in Standing) sb.Append('m').Append(m);
            sb.Append('|');
            foreach (string m in Exposed) sb.Append('x').Append(m);
            sb.Append('|');
            foreach (TraceStep s in Trace) sb.Append(s.RoundId).Append('>').Append(s.RenderingId).Append(',');
            return sb.ToString();
        }
    }

    /// <summary>통역사의 방침. 한 마디마다 후보 역어 하나를 고른다.</summary>
    public interface IPolicy
    {
        string Id { get; }
        string Name { get; }
        RenderDef Pick(GameData d, RoundDef round, SessionState st);
    }

    /// <summary>회담이 굴러가는 동안의 상태. 정책이 읽을 수 있는 것은 여기까지다.</summary>
    public sealed class SessionState
    {
        public int Tension;
        public int TrustA;
        public int TrustB;
        public int Suspicion;
        public int StageIndex;
        public Dictionary<string, string> Clauses = new Dictionary<string, string>();
        public List<string> Standing = new List<string>();
    }

    public static class SessionSim
    {
        /// <summary>
        /// 분위기. **씨드와 마디 번호만으로** 정해진다 — 통역이 무엇을 골랐든 같은 값이 나온다.
        /// 이게 아니면 역어 하나를 바꿨을 때 난수열이 어긋나서
        /// "한 단어가 조약을 바꿨다"와 "난수가 달라졌다"를 가를 수 없다.
        /// </summary>
        public static int Mood(BalanceFile b, int seed, int stageIndex)
        {
            Random r = new Random(unchecked(seed * 1000003 + stageIndex * 7717 + 17));
            return r.Next(b.moodJitterMin, b.moodJitterMax + 1);
        }

        /// <summary>오역이 드러나는지. 이것도 경로와 무관하게 씨드·마디·오해 id 로만 굴린다.</summary>
        public static bool ExposureRoll(int seed, int stageIndex, string misreadId, int riskPercent)
        {
            if (riskPercent <= 0) return false;
            Random r = new Random(unchecked(seed * 7919 + stageIndex * 131 + StableHash(misreadId)));
            return r.Next(0, 100) < riskPercent;
        }

        /// <summary>string.GetHashCode 는 실행마다 달라진다. 재현성을 위해 직접 센다.</summary>
        public static int StableHash(string s)
        {
            int h = 5381;
            for (int i = 0; i < s.Length; i++) h = unchecked(h * 33 + s[i]);
            return h & 0x7FFFFFF;
        }

        private static bool Matches(CondDef c, SessionState st)
        {
            if (c == null) return true;
            if (c.tensionMin >= 0 && st.Tension < c.tensionMin) return false;
            if (c.tensionMax >= 0 && st.Tension > c.tensionMax) return false;
            if (c.suspicionMin >= 0 && st.Suspicion < c.suspicionMin) return false;
            if (!string.IsNullOrEmpty(c.clauseId))
            {
                string cur;
                st.Clauses.TryGetValue(c.clauseId, out cur);
                if (cur == null) cur = "";
                if (!string.IsNullOrEmpty(c.variantId) && cur != c.variantId) return false;
                if (!string.IsNullOrEmpty(c.notVariantId) && cur == c.notVariantId) return false;
            }
            return true;
        }

        /// <summary>이 마디에서 실제로 나오는 발화. 조건에 처음 맞는 것이다.</summary>
        public static RoundDef PickRound(StageDef stage, SessionState st)
        {
            foreach (RoundDef r in stage.rounds) if (Matches(r.condition, st)) return r;
            return stage.rounds[stage.rounds.Length - 1];
        }

        public static RenderDef Exact(RoundDef r)
        {
            foreach (RenderDef x in r.renderings) if (x.register == "exact") return x;
            return r.renderings[0];
        }

        private static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }

        /// <summary>
        /// 회담 한 판.
        /// </summary>
        /// <param name="overrides">마디 id → 역어 id. 정책보다 앞선다. 한 단어만 바꿔 보는 데 쓴다.</param>
        /// <param name="renderingMatters">
        /// false 면 **역어가 장식인 세계**다 — 무엇을 고르든 정확한 역어가 나간 것으로 친다.
        /// 대조군이다. 이 세계에서 정책 사이에 차이가 나면 검사기가 잘못된 것을 재고 있는 것이다.
        /// </param>
        public static SessionResult Run(
            GameData d, int seed, IPolicy policy,
            Dictionary<string, string> overrides = null,
            bool renderingMatters = true)
        {
            BalanceFile b = d.Balance;
            SessionState st = new SessionState
            {
                Tension = b.startTension,
                TrustA = b.startTrustA,
                TrustB = b.startTrustB,
                Suspicion = b.startSuspicion
            };
            SessionResult res = new SessionResult { Seed = seed, PolicyName = policy.Name };
            foreach (ClauseDef c in d.Clauses) res.Clauses[c.id] = "";

            Dictionary<string, int> standingRisk = new Dictionary<string, int>();

            for (int i = 0; i < d.Stages.Count; i++)
            {
                StageDef stage = d.Stages[i];
                st.StageIndex = i;
                st.Clauses = res.Clauses;
                st.Standing = res.Standing;

                RoundDef round = PickRound(stage, st);

                RenderDef pick = null;
                string forced;
                if (overrides != null && overrides.TryGetValue(stage.id, out forced))
                    foreach (RenderDef x in round.renderings) if (x.id == forced) { pick = x; break; }
                if (pick == null) pick = policy.Pick(d, round, st);
                if (pick == null) pick = Exact(round);

                RenderDef applied = renderingMatters ? pick : Exact(round);

                TraceStep step = new TraceStep
                {
                    StageIndex = i,
                    StageId = stage.id,
                    RoundId = round.id,
                    Speaker = round.speaker,
                    Listener = round.listener,
                    SourceText = round.sourceText,
                    TopicClause = round.topicClause,
                    RenderingId = pick.id,
                    Register = pick.register,
                    RenderedText = pick.text,
                    HeardAs = pick.heardAs
                };

                // ── 역어가 남기는 것 ────────────────────────────────────────
                st.Tension = Clamp(st.Tension + applied.tensionDelta, 0, 100);
                st.TrustA = Clamp(st.TrustA + applied.trustADelta, 0, 100);
                st.TrustB = Clamp(st.TrustB + applied.trustBDelta, 0, 100);
                st.Suspicion = Clamp(st.Suspicion + applied.suspicionDelta, 0, 100);

                if (!string.IsNullOrEmpty(applied.setsClause) && !string.IsNullOrEmpty(applied.setsVariant))
                {
                    res.Clauses[applied.setsClause] = applied.setsVariant;
                    step.SetClause = applied.setsClause;
                    step.SetVariant = applied.setsVariant;
                }

                if (!string.IsNullOrEmpty(applied.seedsMisunderstanding)
                    && !res.Standing.Contains(applied.seedsMisunderstanding)
                    && !res.Exposed.Contains(applied.seedsMisunderstanding))
                {
                    res.Standing.Add(applied.seedsMisunderstanding);
                    standingRisk[applied.seedsMisunderstanding] =
                        applied.exposureRiskPercent < 0 ? 0 : applied.exposureRiskPercent;
                    step.SeededMisread = applied.seedsMisunderstanding;
                }

                // ── 앞서 심은 오역이 같은 조항으로 되돌아오면 드러날 수 있다 ──
                // 조인 직전 낭독(revisits)은 조항 여럿을 한꺼번에 다시 읽는다.
                List<string> revisited = new List<string>();
                if (!string.IsNullOrEmpty(round.topicClause)) revisited.Add(round.topicClause);
                if (round.revisits != null) foreach (string rc in round.revisits) if (!revisited.Contains(rc)) revisited.Add(rc);
                if (revisited.Count > 0)
                {
                    for (int k = res.Standing.Count - 1; k >= 0; k--)
                    {
                        string mid = res.Standing[k];
                        if (mid == step.SeededMisread) continue;   // 방금 심은 것은 이 자리에서 드러나지 않는다
                        MisunderstandingDef m = d.Misread(mid);
                        if (m == null || !revisited.Contains(m.clauseId)) continue;
                        int risk;
                        standingRisk.TryGetValue(mid, out risk);
                        risk += applied.revealBonusPercent;   // 그대로 읽으면 드러나고, 넘기면 덮인다
                        if (!ExposureRoll(seed, i, mid, risk)) continue;
                        res.Standing.RemoveAt(k);
                        res.Exposed.Add(mid);
                        step.ExposedMisread = step.ExposedMisread.Length == 0 ? mid : step.ExposedMisread + "+" + mid;
                        st.Suspicion = Clamp(st.Suspicion + b.exposureSuspicion, 0, 100);
                        st.Tension = Clamp(st.Tension + b.exposureTension, 0, 100);
                        st.TrustA = Clamp(st.TrustA - b.exposureTrust, 0, 100);
                        st.TrustB = Clamp(st.TrustB - b.exposureTrust, 0, 100);
                    }
                }

                // ── 방의 분위기. 통역과 무관하게 매 마디 붙는다 ────────────
                int mood = Mood(b, seed, i);
                st.Tension = Clamp(st.Tension + mood, 0, 100);
                step.Mood = mood;

                step.TensionAfter = st.Tension;
                step.TrustAAfter = st.TrustA;
                step.TrustBAfter = st.TrustB;
                step.SuspicionAfter = st.Suspicion;
                res.Trace.Add(step);

                if (st.Tension >= b.collapseTension)
                {
                    res.Collapsed = true;
                    res.CollapsedAtStage = stage.id;
                    break;
                }
            }

            // ── 조인 직전. 조문을 소리 내어 읽으면 남은 오해가 압력이 된다 ──
            if (!res.Collapsed)
            {
                st.Tension = Clamp(st.Tension + res.Standing.Count * b.misunderstandingTension, 0, 100);
                if (st.Tension >= b.collapseTension)
                {
                    res.Collapsed = true;
                    res.CollapsedAtStage = "s_sign";
                }
            }

            res.Tension = st.Tension;
            res.TrustA = st.TrustA;
            res.TrustB = st.TrustB;
            res.Suspicion = st.Suspicion;

            res.Signed = !res.Collapsed && st.TrustA >= b.signTrustMin && st.TrustB >= b.signTrustMin;

            foreach (ClauseDef c in d.Clauses)
            {
                string vid = res.Clauses[c.id];
                if (string.IsNullOrEmpty(vid)) { res.UnsetClauses++; continue; }
                VariantDef v = d.Variant(vid);
                if (v == null) continue;
                res.PaperFavorA += v.favorA;
                res.PaperFavorB += v.favorB;
            }
            int loss = res.Standing.Count * b.misunderstandingFavorLoss;
            res.FavorA = res.Signed ? res.PaperFavorA - loss : 0;
            res.FavorB = res.Signed ? res.PaperFavorB - loss : 0;

            res.EndingId = Endings.Resolve(d, res);
            return res;
        }
    }
}
