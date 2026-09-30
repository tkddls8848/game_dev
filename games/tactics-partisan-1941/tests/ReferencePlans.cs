using System.Collections.Generic;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 사람이 손으로 적은 "의도한 해". 데이터를 설계할 때 실제로 계산한 창들이다.
    ///
    /// 검사기는 이것과 계획 탐색을 **둘 다** 본다:
    ///   · 참조 계획이 이긴다   → 데이터가 설계대로다 (빠르고 정확한 회귀 검사)
    ///   · 탐색도 해를 찾는다   → 사람의 의도를 몰라도 풀린다 (AmbushSolvability 의 실제 주장)
    /// 참조 계획만 보면 "내가 아는 한 수"를 확인하는 것이고, 그건 퍼즐 확인이다.
    /// </summary>
    public static class ReferencePlans
    {
        /// <summary>
        /// 능선 초소: 함정 둘. 두 순찰이 동시에 초소에 있는 [0,45000) 창에 창고를 깔고,
        /// 순회병이 골짜기에 들어오기 전에 골짜기를 깐다. 총성이 없으므로 경보가 0이다.
        /// </summary>
        public static SquadPlan RidgeDuskTraps()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("sapper", ActionKinds.PlantTrap, "z_shed",  null, 30000),
                new SquadOrder("sapper", ActionKinds.PlantTrap, "z_gully", null, 63000)
            });
        }

        /// <summary>
        /// 능선 초소: 저격 둘. 골짜기는 소리가 새지 않아(noiseBlocked 셋) 거기서 쏘면 아무도 못 듣는다.
        /// 순회병이 골짜기에 **닿는 순간**에 맞춰 쏘고(85,000ms), 감시병은 창고에 있는 동안 능선에서 쏜다.
        /// </summary>
        public static SquadPlan RidgeDuskShots()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("sniper", ActionKinds.Shoot, "z_gully", "g_rover",  77000),
                new SquadOrder("sniper", ActionKinds.Shoot, "z_ridge", "g_sentry", 97000)
            });
        }

        /// <summary>
        /// 철로 측선: 암거에서 선로 둑을 두 번 쏜다. 암거는 엄폐 80이라 조준 중에도 보이지 않고,
        /// 암거↔둑·암거↔침목더미가 소음 차폐라 총성이 측선까지 가지 않는다.
        /// 초병 둘이 사라진 뒤에야 측선에 들어갈 수 있다 — 측선은 살아 있는 누구에게든 보인다.
        /// </summary>
        public static SquadPlan RailSupply()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("sniper", ActionKinds.Shoot,       "z_culvert", "g_crossguard", 85000),
                new SquadOrder("sniper", ActionKinds.Shoot,       "z_culvert", "g_railguard",  115000),
                new SquadOrder("sapper", ActionKinds.PlantCharge, "z_siding",  "t_wagon",      140000),
                new SquadOrder("sapper", ActionKinds.Detonate,    "z_siding",  "t_wagon",      155000)
            });
        }

        /// <summary>
        /// 수송 일람표: 아무도 쓰러뜨리지 않는다. 감시병이 초소로 돌아가고(165,000ms)
        /// 순회병도 초소에 있는 45초 창에 창고에 들어간다.
        /// </summary>
        public static SquadPlan RidgeRoster()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("scout", ActionKinds.Infiltrate, "z_shed", "i_roster", 165000)
            });
        }

        public static Dictionary<string, SquadPlan> ByMission()
        {
            return new Dictionary<string, SquadPlan>
            {
                { "m_ridge_dusk",   RidgeDuskTraps() },
                { "m_rail_supply",  RailSupply() },
                { "m_ridge_roster", RidgeRoster() }
            };
        }
    }
}
