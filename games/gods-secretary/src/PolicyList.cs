using System.Collections.Generic;
using Secretary.Data;

namespace Secretary.Sim
{
    /// <summary>온 것을 다 들어준다. 가장 덜 아픈 자리에서 뺀다.</summary>
    public sealed class GrantEverything : Policy
    {
        public override string Name { get { return "전부 허가"; } }
        public override string Note { get { return "온 편지를 오래된 것부터 다 들어준다. 가장 덜 아픈 이름에서 뺀다"; } }
        public override Verdict Decide(SimState st)
        {
            foreach (Letter l in Sorted(st))
                if (Office.GrantableUnits(st, l) > 0)
                    return new Verdict { Kind = Verdicts.Grant, PrayerId = l.Def.id, SourceOrder = CheapestFirst(st, l) };
            Letter x = Oldest(st);
            return x == null ? null : new Verdict { Kind = Verdicts.Deny, PrayerId = x.Def.id };
        }
        private static List<Letter> Sorted(SimState st)
        {
            List<Letter> ls = new List<Letter>(st.Docket);
            ls.Sort(delegate (Letter a, Letter b)
            {
                if (a.ArrivedDay != b.ArrivedDay) return a.ArrivedDay - b.ArrivedDay;
                return string.CompareOrdinal(a.Def.id, b.Def.id);
            });
            return ls;
        }
    }

    /// <summary>다 들어주되 **넉넉해 보이는 사람**에게서 뺀다. 보이는 양과 아픈 정도가 다르다는 것을 모르는 정책.</summary>
    public sealed class GrantFromTheRich : Policy
    {
        public override string Name { get { return "넉넉한 쪽에서"; } }
        public override string Note { get { return "다 들어주되 가진 것이 많아 보이는 이름에서 뺀다. 한도를 넘기는 순간 값이 두 배가 되는 것을 모른다"; } }
        public override Verdict Decide(SimState st)
        {
            foreach (Letter l in st.Docket)
                if (Office.GrantableUnits(st, l) > 0)
                    return new Verdict { Kind = Verdicts.Grant, PrayerId = l.Def.id, SourceOrder = RichestFirst(st, l) };
            Letter x = Oldest(st);
            return x == null ? null : new Verdict { Kind = Verdicts.Deny, PrayerId = x.Def.id };
        }
    }

    /// <summary>전부 기각. 아무 데서도 빼지 않는다 — 대가가 없는 대신 아무도 낫지 않는다.</summary>
    public sealed class RefuseEverything : Policy
    {
        public override string Name { get { return "전부 기각"; } }
        public override string Note { get { return "봉랍을 기각에만 쓴다. 장부는 흔들리지 않고 아무도 낫지 않는다"; } }
        public override Verdict Decide(SimState st)
        {
            Letter x = Oldest(st);
            return x == null ? null : new Verdict { Kind = Verdicts.Deny, PrayerId = x.Def.id };
        }
    }

    /// <summary>전부 보류. 결정을 미루면 사정이 커진다.</summary>
    public sealed class HoldEverything : Policy
    {
        public override string Name { get { return "전부 보류"; } }
        public override string Note { get { return "아무것도 정하지 않는다. 미룬 기도는 작아지지 않는다"; } }
        public override Verdict Decide(SimState st)
        {
            Letter x = Oldest(st);
            return x == null ? null : new Verdict { Kind = Verdicts.Defer, PrayerId = x.Def.id };
        }
    }

    /// <summary>가장 크게 모자란 사람부터 들어준다. 사람의 상식에 가장 가까운 정책.</summary>
    public sealed class DeepestNeedFirst : Policy
    {
        public override string Name { get { return "가장 모자란 쪽부터"; } }
        public override string Note { get { return "부족이 가장 큰 사람의 편지를 먼저 들어준다. 싼 자리에서 뺀다"; } }
        public override Verdict Decide(SimState st)
        {
            Letter best = null; int bestDeficit = 0;
            foreach (Letter l in st.Docket)
            {
                if (Office.GrantableUnits(st, l) <= 0) continue;
                int def = st.L.Deficit(l.Def.fromSoulId, l.Def.domain);
                if (def > bestDeficit) { bestDeficit = def; best = l; }
            }
            if (best != null)
                return new Verdict { Kind = Verdicts.Grant, PrayerId = best.Def.id, SourceOrder = CheapestFirst(st, best) };
            Letter x = Oldest(st);
            return x == null ? null : new Verdict { Kind = Verdicts.Deny, PrayerId = x.Def.id };
        }
    }

    /// <summary>
    /// **세상의 고통이 줄어드는 허가만** 한다. 줄지 않으면 기각한다 —
    /// 대가가 늘 있다는 것을 알고, 그래도 들어줄 만한 것이 있다는 것도 아는 정책.
    /// </summary>
    public sealed class OnlyWhenItHelps : Policy
    {
        public override string Name { get { return "덜어질 때만"; } }
        public override string Note { get { return "그 허가로 세상의 고통이 줄어들 때만 들어준다. 줄지 않으면 기각해 더 커지지 않게 한다"; } }
        public override Verdict Decide(SimState st)
        {
            Letter best = null; string[] bestOrder = null; int bestGain = 0;
            foreach (Letter l in st.Docket)
            {
                if (Office.GrantableUnits(st, l) <= 0) continue;
                foreach (string[] order in Office.SourceOrders(l.Def))
                {
                    int gain = NetGain(st, l, order);
                    if (gain > bestGain) { bestGain = gain; best = l; bestOrder = order; }
                }
            }
            if (best != null)
                return new Verdict { Kind = Verdicts.Grant, PrayerId = best.Def.id, SourceOrder = bestOrder };
            Letter x = Oldest(st);
            return x == null ? null : new Verdict { Kind = Verdicts.Deny, PrayerId = x.Def.id };
        }
    }

    /// <summary>봉랍 셋을 돌아가며 찍는다. 도장을 고루 쓰는 것이 공평이라고 믿는 정책.</summary>
    public sealed class RotateTheSeals : Policy
    {
        public override string Name { get { return "봉랍을 돌려 찍는다"; } }
        public override string Note { get { return "허가·기각·보류를 차례로 찍는다. 한 가지 수를 반복하지 않는 것이 곧 좋은 수인지 보는 대조군"; } }
        public override Verdict Decide(SimState st)
        {
            Letter x = Oldest(st);
            if (x == null) return null;
            int turn = st.Log.Count % 3;
            if (turn == 0 && Office.GrantableUnits(st, x) > 0)
                return new Verdict { Kind = Verdicts.Grant, PrayerId = x.Def.id, SourceOrder = CheapestFirst(st, x) };
            if (turn == 1) return new Verdict { Kind = Verdicts.Deny, PrayerId = x.Def.id };
            return new Verdict { Kind = Verdicts.Defer, PrayerId = x.Def.id };
        }
    }

    /// <summary>숨 · 곡식 · 비 순으로 본다. 영역에 순서를 매기는 정책.</summary>
    public sealed class BreathBeforeBread : Policy
    {
        public override string Name { get { return "숨을 먼저"; } }
        public override string Note { get { return "숨 · 곡식 · 비 순으로 들어준다. 가장 아픈 영역을 먼저 보는 것이 옳다고 믿는다"; } }
        private static readonly string[] Order = { "d_breath", "d_grain", "d_rain" };
        public override Verdict Decide(SimState st)
        {
            foreach (string dom in Order)
                foreach (Letter l in st.Docket)
                {
                    if (l.Def.domain != dom) continue;
                    if (Office.GrantableUnits(st, l) <= 0) continue;
                    return new Verdict { Kind = Verdicts.Grant, PrayerId = l.Def.id, SourceOrder = CheapestFirst(st, l) };
                }
            Letter x = Oldest(st);
            return x == null ? null : new Verdict { Kind = Verdicts.Deny, PrayerId = x.Def.id };
        }
    }
}
