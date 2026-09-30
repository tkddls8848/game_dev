using System.Collections.Generic;
using Whisper.Data;

namespace Whisper.Sim
{
    public sealed class AskOutcome
    {
        public bool Granted;
        public int TrustSpent;
        public string Refusal = "";
        public Rumor Answer;

        public override string ToString()
        {
            return Granted ? ("허락 -" + TrustSpent + " " + Answer) : ("거절: " + Refusal);
        }
    }

    public static class Refusals
    {
        /// <summary>마을이 등을 돌렸다. 신뢰가 바닥이라 아무도 말해 주지 않는다 — 눈이 먼다.</summary>
        public static string VillageClosed { get { return "VillageClosed"; } }
        /// <summary>낼 신뢰가 없다.</summary>
        public static string CannotAfford { get { return "CannotAfford"; } }
        /// <summary>이 사람이 위험해져서 얼마간 입을 닫았다.</summary>
        public static string Silent { get { return "Silent"; } }
        /// <summary>이 사람은 그것을 모른다.</summary>
        public static string DoesNotKnow { get { return "DoesNotKnow"; } }
    }

    /// <summary>
    /// ★ **거점 층.** 마을의 신뢰와 사람들의 위험을 회차 사이로 이어 나른다.
    ///
    /// 두 층을 잇는 줄이 둘이다 — 이 PoC가 시험하려는 가장 약한 이음매다:
    ///   ① **신뢰 → 정찰** : 묻는 데 신뢰를 쓴다. 바닥나면 눈이 먼다
    ///   ② **임무 → 신뢰** : 성공하면 오르고, 실패·보복으로 내려간다
    ///
    /// ②가 ①을 되살리기 때문에 막히지 않는다 — 단 **눈이 먼 채로도 임무를 깰 수 있을 때만** 그렇다.
    /// 그래서 IntelEconomy 의 바닥 시험은 BlindSolvability 위에 서 있다. 둘은 한 검사기의 두 얼굴이다.
    ///
    /// 백분율 곱셈의 나머지는 **버리지 않고 누적한다** (설계 원칙 4) — 부동소수를 쓰지 않는 값.
    /// </summary>
    public sealed class VillageLedger
    {
        private readonly GameData _data;
        private readonly Dictionary<string, int> _exposure = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _silentUntil = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _asksThisMission = new Dictionary<string, int>();

        public int TrustPercent { get; private set; }
        public int MissionIndex { get; private set; }
        public int CostRemainder { get; private set; }
        public int ReprisalRemainder { get; private set; }
        public int CeilingClamps { get; private set; }
        public int FloorClamps { get; private set; }
        public int TotalTrustSpent { get; private set; }
        public int TotalAsks { get; private set; }

        public VillageLedger(GameData data)
        {
            _data = data;
            TrustPercent = data.Balance.trust.startPercent;
            foreach (VillagerDef v in data.AllVillagers) { _exposure[v.id] = 0; _silentUntil[v.id] = -1; }
        }

        /// <summary>검사기가 바닥 상태를 만들 때 쓴다.</summary>
        public void ForceTrust(int percent) { TrustPercent = percent; }

        public int Exposure(string villagerId) { return _exposure[villagerId]; }
        public bool IsSilent(string villagerId) { return _silentUntil[villagerId] > MissionIndex; }

        /// <summary>이번 임무에 입을 닫은 사람들. Whispers 가 이것을 받아 거짓 배분을 줄인다.</summary>
        public HashSet<string> SilencedNow()
        {
            HashSet<string> set = new HashSet<string>();
            foreach (VillagerDef v in _data.AllVillagers) if (IsSilent(v.id)) set.Add(v.id);
            return set;
        }

        /// <summary>마을이 아직 입을 여는가.</summary>
        public bool VillageSpeaks { get { return TrustPercent >= _data.Balance.trust.askFloorPercent; } }

        public void BeginMission() { _asksThisMission.Clear(); }

        /// <summary>이 사람에게 이번 임무에 몇 번째로 묻는가에 따라 값이 오른다.</summary>
        public int QuotedCost(string villagerId)
        {
            AskBalance ab = _data.Balance.ask;
            int asked = _asksThisMission.ContainsKey(villagerId) ? _asksThisMission[villagerId] : 0;
            int surcharge = asked < ab.repeatSurchargePercent.Length
                ? ab.repeatSurchargePercent[asked]
                : ab.repeatSurchargeBeyondPercent;
            int scaled = _data.Villager(villagerId).baseCostPercent * surcharge + CostRemainder;
            return scaled / 100;
        }

        /// <summary>
        /// 묻는다. 신뢰를 쓰고, 그 사람에게 위험이 쌓인다.
        /// 같은 사람에게 계속 물으면 값이 오르고(repeatSurchargePercent), 위험이 임계를 넘으면 입을 닫는다.
        /// </summary>
        public AskOutcome Ask(Whispers whispers, IntelKnowledge knowledge, string villagerId, string itemId)
        {
            AskBalance ab = _data.Balance.ask;
            AskOutcome outcome = new AskOutcome();

            if (!VillageSpeaks)
            {
                outcome.Refusal = Refusals.VillageClosed;
                return outcome;
            }
            if (IsSilent(villagerId))
            {
                outcome.Refusal = Refusals.Silent;
                return outcome;
            }

            Rumor answer = whispers.Answer(villagerId, itemId);
            if (answer == null)
            {
                outcome.Refusal = Refusals.DoesNotKnow;
                return outcome;
            }

            int asked = _asksThisMission.ContainsKey(villagerId) ? _asksThisMission[villagerId] : 0;
            int surcharge = asked < ab.repeatSurchargePercent.Length
                ? ab.repeatSurchargePercent[asked]
                : ab.repeatSurchargeBeyondPercent;
            int scaled = _data.Villager(villagerId).baseCostPercent * surcharge + CostRemainder;
            int cost = scaled / 100;
            if (cost > TrustPercent)
            {
                outcome.Refusal = Refusals.CannotAfford;
                return outcome;
            }

            CostRemainder = scaled % 100;
            TrustPercent -= cost;
            TotalTrustSpent += cost;
            TotalAsks++;
            _asksThisMission[villagerId] = asked + 1;

            _exposure[villagerId] += _data.Villager(villagerId).exposureRiskPercent;
            if (_exposure[villagerId] >= ab.silenceThresholdPercent)
            {
                // 그 사람이 위험해졌다. 얼마간 사라진다 — 되돌아온다(영구 상실이 아니다).
                _silentUntil[villagerId] = MissionIndex + ab.silenceMissions;
                _exposure[villagerId] = 0;
            }

            if (knowledge != null) knowledge.Accept(answer);
            outcome.Granted = true;
            outcome.TrustSpent = cost;
            outcome.Answer = answer;
            return outcome;
        }

        /// <summary>
        /// 회차를 정산한다. 성공은 신뢰를 올리고, 실패와 보복은 내린다.
        /// 보복은 **들킨 자리**가 정한다 — 골목(마을 안)에서 들키는 것이 가장 비싸다.
        /// </summary>
        public void Settle(MissionResult result)
        {
            TrustBalance tb = _data.Balance.trust;
            ReprisalBalance rb = _data.Balance.reprisal;

            if (result.Won) TrustPercent += tb.gainOnSuccess;
            else TrustPercent -= tb.lossOnFailure;

            int scaled = result.ReprisalPoints * rb.trustPerHundredPoints + ReprisalRemainder;
            TrustPercent -= scaled / 100;
            ReprisalRemainder = scaled % 100;

            if (TrustPercent > tb.ceilingPercent) { TrustPercent = tb.ceilingPercent; CeilingClamps++; }
            if (TrustPercent < tb.floorPercent) { TrustPercent = tb.floorPercent; FloorClamps++; }

            foreach (VillagerDef v in _data.AllVillagers)
            {
                int decayed = _exposure[v.id] - _data.Balance.ask.exposureDecayPerMission;
                _exposure[v.id] = decayed < 0 ? 0 : decayed;
            }

            MissionIndex++;
            _asksThisMission.Clear();
        }

        public override string ToString()
        {
            return "신뢰 " + TrustPercent + "% · 회차 " + MissionIndex + " · 물은 횟수 " + TotalAsks;
        }
    }
}
