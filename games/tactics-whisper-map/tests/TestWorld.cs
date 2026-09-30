using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 테스트가 공유하는 세계. data/ 를 한 번만 읽고, 무거운 탐색 결과를 재사용한다.
    /// 탐색이 이 PoC에서 가장 비싼 일이다 — 임무마다 회차를 수천 번 돌린다.
    /// </summary>
    public static class TestWorld
    {
        private static GameData _data;
        private static readonly Dictionary<string, MissionSim> _sims = new Dictionary<string, MissionSim>();
        private static readonly Dictionary<string, SearchResult> _searches = new Dictionary<string, SearchResult>();
        private static readonly Dictionary<string, List<SquadPlan>> _blind = new Dictionary<string, List<SquadPlan>>();

        public static GameData Data
        {
            get
            {
                if (_data == null) _data = GameData.Load();
                return _data;
            }
        }

        public static List<string> MissionIds()
        {
            List<string> ids = new List<string>();
            foreach (MissionDef m in Data.AllMissions) ids.Add(m.id);
            return ids;
        }

        /// <summary>오늘 밤(씨드가 고른 위상)의 회차.</summary>
        public static MissionSim Sim(string missionId)
        {
            MissionSim sim;
            if (_sims.TryGetValue(missionId, out sim)) return sim;
            sim = MissionSim.Tonight(Data, missionId);
            _sims[missionId] = sim;
            return sim;
        }

        /// <summary>이 임무의 정보 시장 (누가 무엇에 대해 거짓을 말하는가).</summary>
        public static Whispers Market(string missionId)
        {
            MissionSim sim = Sim(missionId);
            return new Whispers(Data, sim.Mission, sim.Phases);
        }

        /// <summary>전부 물어서 다 쥔 지식 상태. 값은 무한하다고 보고 — 경제는 IntelEconomy 가 본다.</summary>
        public static IntelKnowledge FullyAsked(string missionId, Whispers market = null)
        {
            MissionSim sim = Sim(missionId);
            if (market == null) market = Market(missionId);
            IntelKnowledge k = new IntelKnowledge(Data, sim.Mission);
            foreach (IntelItemDef item in Data.AllItems)
            {
                IList<VillagerDef> sources = market.AvailableSources(item.id);
                if (sources.Count == 0) continue;
                k.Accept(market.Answer(sources[0].id, item.id));
            }
            return k;
        }

        /// <summary>겹쳐 물어 확인까지 한 지식 상태. 거짓이 섞이지 않는다(대신 값이 두 배다).</summary>
        public static IntelKnowledge Corroborated(string missionId, Whispers market = null)
        {
            MissionSim sim = Sim(missionId);
            if (market == null) market = Market(missionId);
            IntelKnowledge k = new IntelKnowledge(Data, sim.Mission);
            foreach (IntelItemDef item in Data.AllItems)
            {
                IList<VillagerDef> sources = market.AvailableSources(item.id);
                for (int i = 0; i < sources.Count && i < 3; i++)
                    k.Accept(market.Answer(sources[i].id, item.id));
            }
            return k;
        }

        /// <summary>
        /// **거짓말하는 사람에게 물은** 지식 상태. 항목마다 거짓말쟁이가 있으면 그 사람에게 묻는다.
        /// DetectionFairness 의 어려운 부분(틀린 정보로 실패하는 것)을 일부러 만드는 자리다.
        /// </summary>
        public static IntelKnowledge AskedLiarsFirst(string missionId, Whispers market = null)
        {
            MissionSim sim = Sim(missionId);
            if (market == null) market = Market(missionId);
            IntelKnowledge k = new IntelKnowledge(Data, sim.Mission);
            foreach (IntelItemDef item in Data.AllItems)
            {
                IList<VillagerDef> sources = market.AvailableSources(item.id);
                if (sources.Count == 0) continue;
                string liar = market.LiarOf(item.id);
                string pick = liar ?? sources[0].id;
                k.Accept(market.Answer(pick, item.id));
            }
            return k;
        }

        public static IntelKnowledge Blind(string missionId)
        {
            return IntelKnowledge.Blind(Data, Sim(missionId).Mission);
        }

        /// <summary>겹쳐 물어 전부 확인한 상태에서의 탐색 결과. 무겁다 — 한 번만 돈다.</summary>
        public static SearchResult Search(string missionId)
        {
            SearchResult sr;
            if (_searches.TryGetValue(missionId, out sr)) return sr;
            sr = PlanSearch.Beam(Data, Sim(missionId), Corroborated(missionId));
            _searches[missionId] = sr;
            return sr;
        }

        /// <summary>아홉 위상 전부에서 이기는 눈먼 계획들. 가장 무거운 계산이다.</summary>
        public static List<SquadPlan> BlindWinners(string missionId)
        {
            List<SquadPlan> list;
            if (_blind.TryGetValue(missionId, out list)) return list;
            list = PlanSearch.BlindRobustWinners(Data, Data.Mission(missionId));
            _blind[missionId] = list;
            return list;
        }
    }
}
