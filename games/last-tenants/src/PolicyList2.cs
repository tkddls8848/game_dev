using System.Collections.Generic;
using Tenants.Data;

namespace Tenants.Sim
{
    /// <summary>침묵한 칸은 내용이 없다고 보고 아예 지나친다. **이 정책이 벌을 받는가**가 핵 검사기 하나다.</summary>
    public sealed class NeverListenSilent : Policy
    {
        public override string Name { get { return "침묵한 칸은 건너뛴다"; } }
        public override string Note { get { return "소리가 없는 칸에는 들을 것이 없다고 보고 지나친다. 침묵을 내용 없음으로 취급하는 정책"; } }
        public override int ChooseTarget(Night night, Knowledge k)
        {
            int best = -1, bestV = 0;
            for (int i = 0; i < night.Households.Count; i++)
            {
                if (night.Households[i].voice == Voices.Silent) continue;
                if (KnownVacant(k, i)) continue;
                int v = Estimate(night, k, i);
                if (v > bestV) { bestV = v; best = i; }
            }
            return bestV > 0 ? best : -1;
        }
        public override int ChooseDoor(Night night, Knowledge k, int slot)
        {
            List<int> loud = ByLoudness(night);
            for (int j = 0; j < loud.Count; j++)
            {
                int i = loud[j];
                if (night.Households[i].voice == Voices.Silent) continue;
                if (k.WorthListening(i, slot)) return i;
            }
            return -1;
        }
    }

    /// <summary>101호 문 하나만 여섯 칸 내내 엿듣는다. **한 가지 수를 반복하는 것**의 표본.</summary>
    public sealed class RepeatOneDoor : Policy
    {
        public override string Name { get { return "한 문만 되풀이"; } }
        public override string Note { get { return "101호 문에만 여섯 칸 내내 귀를 댄다. 같은 수를 반복하는 것이 최적인지 보는 대조군"; } }
        public override int ChooseTarget(Night night, Knowledge k) { return 0; }
        public override int ChooseDoor(Night night, Knowledge k, int slot) { return 0; }
    }

    /// <summary>유예가 가장 싼 사정을 돕는다. 나머지에게 물리는 값을 줄이려는 정책.</summary>
    public sealed class CheapestGrace : Policy
    {
        public override string Name { get { return "가장 싼 사정"; } }
        public override string Note { get { return "유예를 적게 먹는 사정을 골라 나머지에게 덜 물린다"; } }
        public override int ChooseTarget(Night night, Knowledge k)
        {
            int best = -1, bestGrace = int.MaxValue, bestV = 0;
            for (int i = 0; i < night.Households.Count; i++)
            {
                Belief b = k.Of(i);
                if (b.Level < 2 || b.NeedId == null) continue;
                if (b.NeedId == "need_none") continue;
                int g = night.Data.Need(b.NeedId).graceCost;
                int v = Estimate(night, k, i);
                if (g < bestGrace || (g == bestGrace && v > bestV))
                { bestGrace = g; bestV = v; best = i; }
            }
            return best;
        }
        public override int ChooseDoor(Night night, Knowledge k, int slot)
        {
            List<int> loud = ByLoudness(night);
            for (int j = 0; j < loud.Count; j++)
                if (k.WorthListening(loud[j], slot)) return loud[j];
            return -1;
        }
    }

    /// <summary>
    /// 이웃의 말을 따라간다. 소문이 가리킨 침묵한 칸을 확인하고, 필요한 것만 아는 집은 한 번 더 캔다.
    /// 이 PoC가 사람에게 기대하는 수에 가장 가까운 정책이다.
    /// </summary>
    public sealed class FollowTheNeighbours : Policy
    {
        public override string Name { get { return "이웃의 말을 따라간다"; } }
        public override string Note { get { return "소문이 가리킨 칸을 확인하고, 필요한 것만 아는 집은 한 번 더 캔다"; } }

        public override int ChooseTarget(Night night, Knowledge k) { return BestByEstimate(night, k); }

        public override int ChooseDoor(Night night, Knowledge k, int slot)
        {
            int n = night.Households.Count;
            // 1. 소문은 들었는데 아직 문 앞에 서 보지 않은 침묵한 칸 — 소문을 확신으로 올린다
            for (int i = 0; i < n; i++)
            {
                if (night.Households[i].voice != Voices.Silent) continue;
                Belief b = k.Of(i);
                if (!b.SilenceConfirmed && b.Level >= 1 && k.WorthListening(i, slot)) return i;
            }
            // 2. 지금 가장 큰 짐작인 집이 「필요한 것만」 아는 상태면, 그 얘기를 들은 문에 한 번 더
            int t = BestByEstimate(night, k);
            if (t >= 0 && k.Of(t).Level == 2)
            {
                int door = TalkerAbout(night, k, t, slot);
                if (door >= 0) return door;
            }
            // 3. 아직 아무것도 모르는 집 중 소리가 큰 문
            List<int> loud = ByLoudness(night);
            for (int j = 0; j < loud.Count; j++)
            {
                int i = loud[j];
                if (k.Of(i).Level >= 2) continue;
                if (k.WorthListening(i, slot)) return i;
            }
            // 4. 아직 확인 안 한 침묵한 칸
            for (int i = 0; i < n; i++)
                if (night.Households[i].voice == Voices.Silent
                    && !k.Of(i).SilenceConfirmed && k.WorthListening(i, slot)) return i;
            for (int j = 0; j < loud.Count; j++)
                if (k.WorthListening(loud[j], slot)) return loud[j];
            return -1;
        }

        /// <summary>그 집 얘기가 나올 수 있는 문 — 그 집 자신이거나 인접한 칸.
        /// 어느 칸에서 무엇이 들리는지는 미리 알 수 없으므로 소리가 큰 쪽부터 고른다.</summary>
        private static int TalkerAbout(Night night, Knowledge k, int about, int slot)
        {
            List<int> loud = ByLoudness(night);
            if (night.Households[about].voice != Voices.Silent
                && k.WorthListening(about, slot)) return about;
            for (int j = 0; j < loud.Count; j++)
            {
                int i = loud[j];
                if (i == about) continue;
                if (!night.Data.AreAdjacent(night.Households[i].id, night.Households[about].id)) continue;
                if (k.WorthListening(i, slot)) return i;
            }
            return -1;
        }
    }
}
