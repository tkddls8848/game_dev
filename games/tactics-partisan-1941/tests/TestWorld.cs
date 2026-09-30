using System;
using System.Collections.Generic;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 데이터와 계획 탐색 결과를 **한 번만** 만들어 모든 검사기가 나눠 쓴다.
    /// 탐색은 회차를 수만 번 돌린다 — 검사기마다 다시 돌리면 0.3초가 30초가 된다.
    /// </summary>
    public static class TestWorld
    {
        private static readonly object Gate = new object();
        private static GameData _data;
        private static readonly Dictionary<string, MissionSim> Sims = new Dictionary<string, MissionSim>();
        private static readonly Dictionary<string, SearchResult> Searches = new Dictionary<string, SearchResult>();

        public static GameData Data
        {
            get
            {
                lock (Gate) { return _data ?? (_data = GameData.Load()); }
            }
        }

        public static MissionSim Sim(string missionId)
        {
            lock (Gate)
            {
                MissionSim sim;
                if (Sims.TryGetValue(missionId, out sim)) return sim;
                sim = new MissionSim(Data, Data.Mission(missionId));
                Sims[missionId] = sim;
                return sim;
            }
        }

        /// <summary>전원으로 푼 결과.</summary>
        public static SearchResult Search(string missionId) { return Search(missionId, null); }

        /// <summary>일부 분대원만으로 푼 결과. available 이 null 이면 전원.</summary>
        public static SearchResult Search(string missionId, HashSet<string> available)
        {
            string key = missionId + "|" + (available == null ? "*" : string.Join(",", Sorted(available)));
            lock (Gate)
            {
                SearchResult sr;
                if (Searches.TryGetValue(key, out sr)) return sr;
                SearchOptions options = new SearchOptions { AvailableMembers = available };
                sr = PlanSearch.Solve(Data, Sim(missionId), options);
                Searches[key] = sr;
                return sr;
            }
        }

        public static ReconReport Recon(string missionId)
        {
            MissionSim sim = Sim(missionId);
            return ReconReport.Observe(Data, sim.Mission, sim.Graph, sim.Patrols);
        }

        public static List<string> MissionIds()
        {
            List<string> ids = new List<string>();
            foreach (MissionDef m in Data.AllMissions) ids.Add(m.id);
            return ids;
        }

        public static List<string> MemberIds()
        {
            List<string> ids = new List<string>();
            foreach (MemberDef m in Data.AllMembers) ids.Add(m.id);
            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        public static HashSet<string> AllMembersExcept(string memberId)
        {
            HashSet<string> set = new HashSet<string>();
            foreach (string id in MemberIds()) if (id != memberId) set.Add(id);
            return set;
        }

        private static List<string> Sorted(IEnumerable<string> items)
        {
            List<string> list = new List<string>(items);
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }
}
