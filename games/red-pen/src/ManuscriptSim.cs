using System;
using System.Collections.Generic;
using System.Text;
using RedPen.Data;

namespace RedPen.Sim
{
    /// <summary>원고 위의 문장 하나. 오간 회차 동안 달라진다.</summary>
    public sealed class LiveSentence
    {
        public string Id;
        public int Order;
        public string Kind;
        public string Text;
        public int Quality;
        public int Voice;
        public int PrideGuard;
        public List<string> Flaws = new List<string>();
        public bool Deleted;
        public bool IsMasterpiece;

        /// <summary>붉어진 정도 — 이 문장에 얹힌 표시들. 관계의 역사이자 화면의 주인공이다.</summary>
        public List<string> MarkHistory = new List<string>();
        public List<int> MarkRound = new List<int>();
        public List<bool> MarkTook = new List<bool>();   // 작가가 받아들였는가

        public LiveSentence Clone()
        {
            LiveSentence c = new LiveSentence
            {
                Id = Id, Order = Order, Kind = Kind, Text = Text, Quality = Quality, Voice = Voice,
                PrideGuard = PrideGuard, Deleted = Deleted, IsMasterpiece = IsMasterpiece
            };
            c.Flaws.AddRange(Flaws);
            c.MarkHistory.AddRange(MarkHistory);
            c.MarkRound.AddRange(MarkRound);
            c.MarkTook.AddRange(MarkTook);
            return c;
        }

        /// <summary>흠을 뺀 실제 값.</summary>
        public int Effective(GameData d)
        {
            int q = Quality;
            foreach (string f in Flaws) { FlawDef fd = d.Flaw(f); if (fd != null) q -= fd.weight; }
            return q < 0 ? 0 : q;
        }
    }

    /// <summary>회차 하나에서 일어난 일.</summary>
    public sealed class RoundLog
    {
        public int Round;
        public int Harshness;
        public int ConfidenceAfter;
        public int TrustAfter;
        public int StubbornAfter;
        public int QualityAfter;
        public int VoiceAfter;
        public string ReviseBand = "";       // good · normal · timid
        public int Day;                      // 작가의 그날
        public bool Masterpiece;
        public List<string> Refused = new List<string>();   // 작가가 버틴 표시
        public List<string> Applied = new List<string>();
        public List<string> Wasted = new List<string>();   // 고칠 것도 없는데 그어 놓은 줄
        public List<string> Introduced = new List<string>();  // 고치다가 새로 생긴 흠
        public List<string> Hollow = new List<string>();      // 흠 있는 문장에 얹은 빈 칭찬
        public List<string> Deleted = new List<string>();
    }

    public sealed class RunResult
    {
        public int Seed;
        public string PolicyName = "";
        public List<LiveSentence> Sentences = new List<LiveSentence>();
        public List<RoundLog> Rounds = new List<RoundLog>();
        public int Confidence;
        public int Trust;
        public int Stubborn;
        public int Quality;          // 살아남은 문장의 평균 실효값
        public int Voice;            // 평균
        public int Masterpieces;
        public int LostSentences;    // 지워 버린 문장
        public int Redness;          // 얹힌 표시의 총합 (harshness 가중)
        public bool Withdrawn;       // 작가가 원고를 거둬 갔다
        public bool Silenced;        // 더 쓰지 못한다
        public bool Publishable;
        public string EndingId = "";

        public int SurvivingCount()
        {
            int n = 0;
            foreach (LiveSentence s in Sentences) if (!s.Deleted) n++;
            return n;
        }

        public string Signature()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("q").Append(Quality).Append("v").Append(Voice)
              .Append("c").Append(Confidence).Append("t").Append(Trust).Append("s").Append(Stubborn)
              .Append("m").Append(Masterpieces).Append("l").Append(LostSentences)
              .Append("r").Append(Redness).Append("|").Append(EndingId).Append("|");
            foreach (LiveSentence s in Sentences)
                sb.Append(s.Id).Append(s.Deleted ? "-" : "+").Append(s.Quality).Append('/').Append(s.Voice)
                  .Append('[').Append(string.Join(".", s.Flaws)).Append(']')
                  .Append('{').Append(string.Join(".", s.MarkHistory)).Append('}').Append(';');
            return sb.ToString();
        }
    }

    /// <summary>편집자의 방침. 회차마다 문장 하나하나에 표시를 고른다.</summary>
    public interface IPolicy
    {
        string Id { get; }
        string Name { get; }
        MarkDef Pick(GameData d, LiveSentence s, AuthorState a, int round);
    }

    public sealed class AuthorState
    {
        public int Confidence;
        public int Trust;
        public int Stubborn;
    }

    public static class ManuscriptSim
    {
        /// <summary>작가의 그날. **씨드와 회차로만** 정해진다 — 편집자가 무엇을 했든 같은 값이다.</summary>
        public static int Day(AuthorFile a, int seed, int round)
        {
            Random r = new Random(unchecked(seed * 1000003 + round * 7717 + 17));
            return r.Next(a.dayJitterMin, a.dayJitterMax + 1);
        }

        /// <summary>고치다가 새 흠이 생기는가. 씨드·회차·문장·부호로만 굴린다.</summary>
        public static bool IntroduceRoll(int seed, int round, string sentenceId, string markId, int percent)
        {
            if (percent <= 0) return false;
            Random r = new Random(unchecked(seed * 6997 + round * 211
                        + StableHash(sentenceId) * 3 + StableHash(markId)));
            return r.Next(0, 100) < percent;
        }

        public static bool MasterpieceRoll(int seed, int round, int chancePercent)
        {
            if (chancePercent <= 0) return false;
            Random r = new Random(unchecked(seed * 7919 + round * 131 + StableHash("masterpiece")));
            return r.Next(0, 100) < chancePercent;
        }

        /// <summary>string.GetHashCode 는 실행마다 달라진다. 재현성을 위해 직접 센다.</summary>
        public static int StableHash(string s)
        {
            int h = 5381;
            for (int i = 0; i < s.Length; i++) h = unchecked(h * 33 + s[i]);
            return h & 0x7FFFFFF;
        }

        private static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }

        /// <summary>이 부호가 이 문장의 흠을 하나라도 건드리는가.</summary>
        public static bool Helps(GameData d, MarkDef m, LiveSentence s)
        {
            if (m.fixes == null) return false;
            if (m.removesSentence == 1) return true;
            foreach (string f in m.fixes)
            {
                if (f == "*") return s.Flaws.Count > 0;
                if (s.Flaws.Contains(f)) return true;
            }
            return false;
        }

        private static bool Fixes(MarkDef m, string flaw)
        {
            if (m.fixes == null) return false;
            foreach (string f in m.fixes) if (f == "*" || f == flaw) return true;
            return false;
        }

        /// <summary>
        /// 원고 한 벌을 몇 차례 주고받는다.
        /// </summary>
        /// <param name="editsMatter">
        /// false 면 **붉은 펜이 장식인 세계**다 — 표시는 남지만 원고도 작가도 달라지지 않는다.
        /// 대조군이다. 이 세계에서 정책 사이에 차이가 나면 검사기가 잘못된 것을 재고 있는 것이다.
        /// </param>
        public static RunResult Run(GameData d, int seed, IPolicy policy, bool editsMatter = true)
        {
            AuthorFile af = d.Author;
            BalanceFile b = d.Balance;

            RunResult res = new RunResult { Seed = seed, PolicyName = policy.Name };
            foreach (SentenceDef sd in d.AllSentences)
            {
                LiveSentence s = new LiveSentence
                {
                    Id = sd.id, Order = sd.order, Kind = sd.kind, Text = sd.text,
                    Quality = sd.quality, Voice = sd.voice, PrideGuard = sd.prideGuard
                };
                if (sd.flaws != null) s.Flaws.AddRange(sd.flaws);
                res.Sentences.Add(s);
            }
            res.Sentences.Sort((x, y) => x.Order.CompareTo(y.Order));

            AuthorState a = new AuthorState
            {
                Confidence = af.startConfidence, Trust = af.startTrust, Stubborn = af.startStubborn
            };

            for (int round = 0; round < b.rounds; round++)
            {
                RoundLog log = new RoundLog { Round = round };

                // ── 편집자가 붉은 펜을 든다 ──────────────────────────────
                List<KeyValuePair<LiveSentence, MarkDef>> picks = new List<KeyValuePair<LiveSentence, MarkDef>>();
                foreach (LiveSentence s in res.Sentences)
                {
                    if (s.Deleted) continue;
                    MarkDef m = policy.Pick(d, s, a, round);
                    if (m == null) continue;
                    picks.Add(new KeyValuePair<LiveSentence, MarkDef>(s, m));
                    int weight = m.harshness * (100 + s.PrideGuard * af.prideAmplifyPercent / 100) / 100;
                    log.Harshness += weight;
                    res.Redness += m.harshness;
                    s.MarkHistory.Add(m.id);
                    s.MarkRound.Add(round);
                    s.MarkTook.Add(false);   // 아래에서 받아들여지면 true 로 바꾼다
                }

                if (!editsMatter)
                {
                    // 붉은 펜이 장식인 세계. 표시만 남고 원고도 작가도 그대로다.
                    log.ConfidenceAfter = a.Confidence; log.TrustAfter = a.Trust; log.StubbornAfter = a.Stubborn;
                    log.QualityAfter = AverageQuality(d, res); log.VoiceAfter = AverageVoice(res);
                    log.ReviseBand = "none";
                    res.Rounds.Add(log);
                    continue;
                }

                // ── 작가가 봉투를 연다 ───────────────────────────────────
                // **한 통의 편지로 읽는다.** 문장마다 더하면 문장 아홉에 동그라미를 친 것이
                // 한 번 칭찬한 것의 아홉 배가 되는데, 그건 편지가 아니라 산수다.
                // 그래서 마음에 남는 것은 **평균**이고, 요구한 일의 양(혹독함)만 합이다.
                int confSum = 0, trustSum = 0, stubSum = 0;
                foreach (KeyValuePair<LiveSentence, MarkDef> kv in picks)
                {
                    MarkDef m = kv.Value;
                    int pride = 100 + kv.Key.PrideGuard * af.prideAmplifyPercent / 100;
                    confSum += m.confidenceDelta < 0 ? m.confidenceDelta * pride / 100 : m.confidenceDelta;
                    trustSum += m.trustDelta < 0 ? m.trustDelta * pride / 100 : m.trustDelta;
                    stubSum += m.stubbornDelta;
                }
                int n = picks.Count < 1 ? 1 : picks.Count;
                a.Confidence = Clamp(a.Confidence + confSum / n, 0, 100);
                a.Trust = Clamp(a.Trust + trustSum / n, 0, 100);
                a.Stubborn = Clamp(a.Stubborn + stubSum / n, 0, 100);
                a.Stubborn = Clamp(a.Stubborn
                    + log.Harshness * af.stubbornPerHarsh / 100
                    - a.Trust * af.trustCalmsStubborn / 100, 0, 100);

                // ── 작가가 고친다 (또는 버틴다) ─────────────────────────
                int wasted = 0, hollow = 0;
                for (int i = 0; i < picks.Count; i++)
                {
                    LiveSentence s = picks[i].Key;
                    MarkDef m = picks[i].Value;
                    int slot = s.MarkHistory.Count - 1;
                    for (int k = s.MarkHistory.Count - 1; k >= 0; k--)
                        if (s.MarkRound[k] == round) { slot = k; break; }

                    // 흠이 있는 문장에 얹은 **빈 칭찬**(또는 침묵). 작가도 자기 글의 흠을 안다 —
                    // 아무것도 묻지 않는 사람은 읽지 않은 사람이다. 칭찬만 다섯 번 하는 것이
                    // 최선이 되지 않게 막는 값이다.
                    if (m.harshness == 0 && !Helps(d, m, s) && s.Flaws.Count > 0)
                    {
                        hollow++;
                        log.Hollow.Add(s.Id + ":" + m.id);
                    }

                    bool refuses = false;
                    if (a.Stubborn >= af.ignoreStubborn && m.harshness >= af.hardMarkFrom) refuses = true;
                    if (m.needsTrustMin >= 0 && a.Trust < m.needsTrustMin) refuses = true;
                    if (m.needsStubbornMax >= 0 && a.Stubborn > m.needsStubbornMax) refuses = true;

                    if (refuses) { log.Refused.Add(s.Id + ":" + m.id); continue; }

                    s.MarkTook[slot] = true;
                    log.Applied.Add(s.Id + ":" + m.id);

                    if (m.removesSentence == 1)
                    {
                        s.Deleted = true;
                        res.LostSentences++;
                        log.Deleted.Add(s.Id);
                        continue;
                    }

                    // 한 번에 다 낫지 않는다. 그래서 원고가 다섯 번 오간다.
                    int cap = m.maxFixes < 1 ? 1 : m.maxFixes;
                    int removed = 0;
                    for (int k = s.Flaws.Count - 1; k >= 0 && removed < cap; k--)
                        if (Fixes(m, s.Flaws[k])) { s.Flaws.RemoveAt(k); removed++; }

                    // 고칠 것이 없는 문장에 그어 놓은 붉은 줄. 원고는 그대로고 작가만 깎인다.
                    // 이게 없으면 한 부호만 다섯 번 되풀이하는 것이 언제나 최선이 된다.
                    if (removed == 0 && m.harshness > 0)
                    {
                        wasted++;
                        log.Wasted.Add(s.Id + ":" + m.id);
                        continue;
                    }
                    s.Quality = Clamp(s.Quality + m.qualityGain, 0, 100);
                    s.Voice = Clamp(s.Voice + m.voiceDelta, 0, 100);

                    // ★ 고치면 새것이 생긴다. 편집자가 써 넣은 줄은 이 글의 소리가 아니고,
                    //   도려낸 자리는 무엇을 가리키는지 흐려진다. 되풀이가 공짜가 아닌 이유다.
                    if (!string.IsNullOrEmpty(m.introducesFlaw) && m.introducesPercent > 0
                        && !s.Flaws.Contains(m.introducesFlaw)
                        && IntroduceRoll(seed, round, s.Id, m.id, m.introducesPercent))
                    {
                        s.Flaws.Add(m.introducesFlaw);
                        log.Introduced.Add(s.Id + ":" + m.introducesFlaw);
                    }
                }
                a.Confidence = Clamp(a.Confidence + af.wasteConfidence * wasted / n, 0, 100);
                a.Trust = Clamp(a.Trust + af.wasteTrust * wasted / n, 0, 100);
                a.Stubborn = Clamp(a.Stubborn + af.wasteStubborn * wasted / n, 0, 100);
                a.Trust = Clamp(a.Trust + af.hollowTrust * hollow / n, 0, 100);

                // ── 돌아온 원고 ─────────────────────────────────────────
                int day = Day(af, seed, round);
                log.Day = day;
                int dq, dv;
                if (a.Confidence >= af.goodConfidence && a.Trust >= af.goodTrust)
                { dq = af.reviseGoodQuality; dv = af.reviseGoodVoice; log.ReviseBand = "good"; }
                else if (a.Confidence <= af.timidConfidence)
                { dq = af.reviseTimidQuality; dv = af.reviseTimidVoice; log.ReviseBand = "timid"; }
                else
                { dq = af.reviseNormalQuality; dv = af.reviseNormalVoice; log.ReviseBand = "normal"; }

                foreach (LiveSentence s in res.Sentences)
                {
                    if (s.Deleted) continue;
                    s.Quality = Clamp(s.Quality + dq + day, 0, 100);
                    s.Voice = Clamp(s.Voice + dv, 0, 100);
                }

                // ── 가끔 진짜 걸작이 온다 ───────────────────────────────
                int avgQ = AverageQuality(d, res), avgV = AverageVoice(res);
                if (a.Confidence >= b.masterpieceConfidence && a.Trust >= b.masterpieceTrust
                    && avgV >= b.masterpieceVoice && avgQ >= b.masterpieceQuality
                    && MasterpieceRoll(seed, round, b.masterpieceChancePercent))
                {
                    LiveSentence gift = new LiveSentence
                    {
                        Id = "s_gift_" + round,
                        Order = 1000 + round,
                        Kind = "k_gift",
                        Text = Localization.Text("masterpiece", "아무도 청하지 않은 문장이 한 줄 딸려 왔다"),
                        Quality = b.masterpieceSentenceQuality,
                        Voice = b.masterpieceSentenceVoice,
                        PrideGuard = b.masterpieceSentencePride,
                        IsMasterpiece = true
                    };
                    res.Sentences.Add(gift);
                    res.Masterpieces++;
                    log.Masterpiece = true;
                }

                log.ConfidenceAfter = a.Confidence;
                log.TrustAfter = a.Trust;
                log.StubbornAfter = a.Stubborn;
                log.QualityAfter = AverageQuality(d, res);
                log.VoiceAfter = AverageVoice(res);
                res.Rounds.Add(log);

                if (a.Trust <= af.quitTrust) { res.Withdrawn = true; break; }
                if (a.Confidence <= af.silenceConfidence) { res.Silenced = true; break; }
            }

            res.Confidence = a.Confidence;
            res.Trust = a.Trust;
            res.Stubborn = a.Stubborn;
            res.Quality = AverageQuality(d, res);
            res.Voice = AverageVoice(res);
            res.Publishable = !res.Withdrawn && !res.Silenced
                              && res.Quality >= b.publishQuality
                              && res.Voice >= b.publishVoice
                              && res.SurvivingCount() >= b.publishMinSentences;
            res.EndingId = Endings.Resolve(d, res);
            return res;
        }

        public static int AverageQuality(GameData d, RunResult r)
        {
            int sum = 0, n = 0;
            foreach (LiveSentence s in r.Sentences) { if (s.Deleted) continue; sum += s.Effective(d); n++; }
            return n == 0 ? 0 : sum / n;
        }

        public static int AverageVoice(RunResult r)
        {
            int sum = 0, n = 0;
            foreach (LiveSentence s in r.Sentences) { if (s.Deleted) continue; sum += s.Voice; n++; }
            return n == 0 ? 0 : sum / n;
        }
    }
}
