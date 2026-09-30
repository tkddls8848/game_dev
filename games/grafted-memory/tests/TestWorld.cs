using System.Collections.Generic;
using Graft.Data;
using Graft.Sim;

namespace Graft.Tests
{
    /// <summary>한 정책을 한 의뢰에서 씨드 전부로 돌린 결과.</summary>
    public sealed class PolicyRun
    {
        public string PolicyName = "";
        public string CommissionId = "";
        public bool RulesEnabled = true;
        public readonly List<Attempt> Attempts = new List<Attempt>();

        public int Total { get { return Attempts.Count; } }

        public int Accepted
        {
            get { int n = 0; foreach (Attempt a in Attempts) if (a.Accepted) n++; return n; }
        }

        /// <summary>성공률(백분율 정수). 부동소수를 쓰지 않는다 — 설계 원칙 4.</summary>
        public int RatePercent { get { return Total == 0 ? 0 : Accepted * 100 / Total; } }

        public int Surprises
        {
            get { int n = 0; foreach (Attempt a in Attempts) if (a.Surprised) n++; return n; }
        }

        public int LuciditySpent
        {
            get { int n = 0; foreach (Attempt a in Attempts) n += a.Knowledge.LuciditySpent; return n; }
        }
    }

    /// <summary>
    /// 테스트가 공유하는 세계. data/ 를 한 번만 읽고 정책 실행 결과를 재사용한다.
    /// 정책 실행이 이 PoC에서 가장 비싼 일이다 — 의뢰 넷 x 정책 일곱 x 씨드 예순.
    /// </summary>
    public static class TestWorld
    {
        private static GameData _data;
        private static readonly Dictionary<string, PolicyRun> _runs = new Dictionary<string, PolicyRun>();

        public static GameData Data
        {
            get { if (_data == null) _data = GameData.Load(); return _data; }
        }

        public static List<string> CommissionIds()
        {
            List<string> ids = new List<string>();
            foreach (CommissionDef c in Data.AllCommissions) ids.Add(c.id);
            return ids;
        }

        public static string[] PolicyNames()
        {
            return new[]
            {
                "Careful", "Reckless", "ProbeEverything", "LiarTrusting", "TrustDistorted",
                "FixedFirstMood", "FixedMaxIntensity", "FixedEarliestDay"
            };
        }

        public static PolicyRun Run(string commissionId, string policy, bool rulesEnabled = true)
        {
            string key = commissionId + "|" + policy + "|" + (rulesEnabled ? "on" : "off");
            PolicyRun r;
            if (_runs.TryGetValue(key, out r)) return r;

            r = new PolicyRun { PolicyName = policy, CommissionId = commissionId, RulesEnabled = rulesEnabled };
            for (int trial = 0; trial < Data.Balance.trialSeeds; trial++)
            {
                DreamSession s = DreamSession.Open(Data, commissionId, trial, rulesEnabled);
                r.Attempts.Add(Invoke(policy, s, trial));
            }
            _runs[key] = r;
            return r;
        }

        private static Attempt Invoke(string policy, DreamSession s, int trial)
        {
            CommissionDef c = s.Commission;
            switch (policy)
            {
                case "Careful": return Policies.Careful(s, trial);
                case "Reckless": return Policies.Reckless(s, trial);
                case "ProbeEverything": return Policies.ProbeEverything(s, trial);
                case "LiarTrusting": return Policies.LiarTrusting(s, trial);
                case "TrustDistorted": return Policies.TrustDistorted(s, trial);
                case "FixedFirstMood":
                    return Policies.Fixed(s, trial, policy, c.allowedMoodIds[0], -1, null);
                case "FixedMaxIntensity":
                    return Policies.Fixed(s, trial, policy, null, c.intensityMax, null);
                case "FixedEarliestDay":
                    return Policies.Fixed(s, trial, policy, null, -1, "earliest");
                default: throw new KeyNotFoundException("없는 정책: " + policy);
            }
        }

        /// <summary>정책 하나의 의뢰 전체 성공률.</summary>
        public static int OverallRate(string policy, bool rulesEnabled = true)
        {
            int acc = 0, tot = 0;
            foreach (string cid in CommissionIds())
            {
                PolicyRun r = Run(cid, policy, rulesEnabled);
                acc += r.Accepted; tot += r.Total;
            }
            return tot == 0 ? 0 : acc * 100 / tot;
        }

        /// <summary>net.json 을 건드리지 않고 복제한다. 음성 대조군이 데이터를 망가뜨릴 때 쓴다.</summary>
        public static GameData CloneData()
        {
            return GameData.Load(Data.DataRoot);
        }
    }
}
