using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 사람이 손으로 적은 "의도한 해". 검사기는 이것과 **계획 탐색을 둘 다** 본다 —
    /// 참조 계획만 보면 "내가 아는 한 수"를 확인하는 것이고, 그건 퍼즐 확인이다(PLAN_TACTICS §2).
    ///
    /// 셋 다 **눈먼 계획**이다: 마을에 한 마디도 묻지 않고, 아홉 위상 전부에서 이긴다.
    /// 그게 이 PoC가 증명하려는 첫 줄이다 — 정보는 관문이 아니라 선택이다.
    /// </summary>
    public static class ReferencePlans
    {
        /// <summary>
        /// 해질 무렵 검문소 — 과수원에서 두 번 쏜다.
        /// 과수원은 아무 데서도 보이지 않고, 총성 90은 차폐 40 + 홉 40에 먹혀 10이 되어 아무도 듣지 않는다.
        /// 표적이 골목에 들어올 때까지 **기다린다**(holdWindowMs 120초). 그래서 위상을 몰라도 된다.
        /// </summary>
        public static SquadPlan DuskCheckpointBlind()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("m_sniper", ActionKinds.Shoot, "z_orchard", "g_laneguard", 30000),
                new SquadOrder("m_sniper", ActionKinds.Shoot, "z_orchard", "g_bellguard", 90000)
            });
        }

        /// <summary>같은 임무, 표적 순서를 바꾼 해. 해가 하나가 아니라는 증거다.</summary>
        public static SquadPlan DuskCheckpointBlindSwapped()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("m_sniper", ActionKinds.Shoot, "z_orchard", "g_bellguard", 30000),
                new SquadOrder("m_sniper", ActionKinds.Shoot, "z_orchard", "g_laneguard", 90000)
            });
        }

        /// <summary>
        /// 곳간의 징발 곡물 — 자루 뒤에 몸을 묻고 폭약을 놓고, 과수원으로 물러나 터뜨린다.
        /// 제 발밑의 폭약은 터뜨릴 수 없으므로 **물러나는 7초**가 값에 든다.
        /// 폭음 100은 어디에 서 있든 두 순찰 모두에게 닿는다(15 x 2 = 30) — 그래서 임계가 30이다.
        /// </summary>
        public static SquadPlan GranaryChargeBlind()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("m_sapper", ActionKinds.PlantCharge, "z_granary", "t_granary_grain", 30000, true),
                new SquadOrder("m_sapper", ActionKinds.Detonate, "z_orchard", "t_granary_grain", 60000)
            });
        }

        /// <summary>
        /// 징발 장부 — 곡물 자루 뒤에 묻고(6초) 장부를 빼낸다.
        /// 묻지 않으면 탈취 중 노출 25에 걸려 120 - 25 = 95 로 보인다. 묻으면 155 - 25 = 130.
        /// 아무도 쓰러뜨리지 않고 경보 0으로 끝난다.
        /// </summary>
        public static SquadPlan LedgerTheftBlind()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("m_scout", ActionKinds.Infiltrate, "z_granary", "doc_requisition", 30000, true)
            });
        }

        public static SquadPlan For(string missionId)
        {
            if (missionId == "m_dusk_checkpoint") return DuskCheckpointBlind();
            if (missionId == "m_granary_charge") return GranaryChargeBlind();
            if (missionId == "m_ledger_theft") return LedgerTheftBlind();
            return null;
        }

        public static SquadPlan[] All()
        {
            return new[]
            {
                DuskCheckpointBlind(), DuskCheckpointBlindSwapped(),
                GranaryChargeBlind(), LedgerTheftBlind()
            };
        }
    }
}
