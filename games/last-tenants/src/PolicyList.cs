using System.Collections.Generic;
using Tenants.Data;

namespace Tenants.Sim
{
    /// <summary>아무도 돕지 않는다. 비교의 바닥 — 이것보다 나쁜 선택이 있는지가 이 PoC의 수치 하나다.</summary>
    public sealed class HelpNoOne : Policy
    {
        public override string Name { get { return "아무도 돕지 않는다"; } }
        public override string Note { get { return "여섯 칸을 다 엿듣고 문을 두드리지 않는다. 유예가 한 푼도 빠지지 않는다"; } }
        public override int ChooseTarget(Night night, Knowledge k) { return -1; }
        public override int ChooseDoor(Night night, Knowledge k, int slot)
        {
            for (int i = 0; i < night.Households.Count; i++)
                if (k.WorthListening(i, slot)) return i;
            return -1;
        }
    }

    /// <summary>호수 순서로 한 칸씩 엿듣고, 짐작이 가장 큰 집을 돕는다.</summary>
    public sealed class SweepInOrder : Policy
    {
        public override string Name { get { return "호수 순서로 훑는다"; } }
        public override string Note { get { return "101부터 차례로. 배치도를 위에서 아래로 읽는 사람이 하는 일"; } }
        public override int ChooseTarget(Night night, Knowledge k) { return BestByEstimate(night, k); }
        public override int ChooseDoor(Night night, Knowledge k, int slot)
        {
            for (int i = 0; i < night.Households.Count; i++)
                if (k.WorthListening(i, slot)) return i;
            return -1;
        }
    }

    /// <summary>가장 시끄러운 문에 계속 귀를 댄다. **큰 소리가 큰 사정이라고 믿는 정책.**</summary>
    public sealed class LoudestDoor : Policy
    {
        public override string Name { get { return "가장 시끄러운 문"; } }
        public override string Note { get { return "복도에서 소리가 가장 큰 칸에 계속 귀를 댄다. 큰 소리가 큰 사정이라고 믿는다"; } }
        public override int ChooseTarget(Night night, Knowledge k)
        {
            List<int> order = ByLoudness(night);
            for (int i = 0; i < order.Count; i++)
                if (!KnownVacant(k, order[i]) && k.Of(order[i]).Level > 0) return order[i];
            return order.Count > 0 ? order[0] : -1;
        }
        public override int ChooseDoor(Night night, Knowledge k, int slot)
        {
            List<int> order = ByLoudness(night);
            for (int i = 0; i < order.Count; i++)
                if (k.WorthListening(order[i], slot)) return order[i];
            return -1;
        }
    }

    /// <summary>
    /// 소리 없는 칸을 먼저 확인하고, 그 칸의 이웃을 캐고, 침묵한 칸을 돕는다.
    /// **침묵이 곧 급함이라고 믿는 정책** — 그래서 침묵이 빈 집인 건물에서 크게 진다.
    /// </summary>
    public sealed class SilentFirst : Policy
    {
        public override string Name { get { return "침묵한 칸부터"; } }
        public override string Note { get { return "파형 자리가 빈 칸을 먼저 확인하고 그 이웃을 캔다. 침묵을 곧 급함으로 읽는다"; } }
        public override int ChooseTarget(Night night, Knowledge k)
        {
            int best = -1, bestV = -1;
            for (int i = 0; i < night.Households.Count; i++)
            {
                if (night.Households[i].voice != Voices.Silent) continue;
                if (KnownVacant(k, i)) continue;
                int v = Estimate(night, k, i);
                if (v > bestV) { bestV = v; best = i; }
            }
            if (best >= 0) return best;
            return BestByEstimate(night, k);   // 침묵이 전부 빈 집이었다. 그제야 돌아선다
        }
        public override int ChooseDoor(Night night, Knowledge k, int slot)
        {
            // 1. 아직 귀를 대 보지 않은 침묵한 칸
            for (int i = 0; i < night.Households.Count; i++)
                if (night.Households[i].voice == Voices.Silent
                    && !k.Of(i).SilenceConfirmed && k.WorthListening(i, slot)) return i;
            // 2. 침묵한 칸에 인접한 칸 중 소리가 큰 쪽 — 그 칸 얘기를 들으려고
            List<int> loud = ByLoudness(night);
            for (int j = 0; j < loud.Count; j++)
            {
                int i = loud[j];
                if (!k.WorthListening(i, slot)) continue;
                for (int s = 0; s < night.Households.Count; s++)
                    if (night.Households[s].voice == Voices.Silent
                        && night.Data.AreAdjacent(night.Households[i].id, night.Households[s].id))
                        return i;
            }
            // 3. 나머지
            for (int j = 0; j < loud.Count; j++)
                if (k.WorthListening(loud[j], slot)) return loud[j];
            return -1;
        }
    }
}
