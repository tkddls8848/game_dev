using System.Collections.Generic;
using Secretary.Data;

namespace Secretary.Sim
{
    /// <summary>
    /// 비서가 쓸 만한 수 하나. 정책은 장부를 볼 수 있다 — 천상의 서식에는 숨은 값이 없다.
    /// 숨은 것은 **앞으로 어떤 편지가 올지**뿐이고, 그것이 이 게임의 불확실성 전부다.
    /// </summary>
    public abstract class Policy
    {
        public abstract string Name { get; }
        public abstract string Note { get; }
        /// <summary>도장을 어디에 찍는가. null 이면 오늘은 그만둔다.</summary>
        public abstract Verdict Decide(SimState st);

        public static SimState Run(GameData d, string volumeId, int seed, Policy p)
        {
            return Run(d, volumeId, seed, p, d.Balance.conservation.enabled);
        }

        public static SimState Run(GameData d, string volumeId, int seed, Policy p, bool conservation)
        {
            SimState st = Office.Begin(d, volumeId, seed, conservation);
            while (!st.Finished)
            {
                while (st.StampsLeft > 0)
                {
                    Verdict v = p.Decide(st);
                    if (v == null) break;
                    if (!Office.Stamp(st, v)) break;
                }
                Office.EndDay(st);
            }
            return st;
        }

        // ── 정책들이 같이 쓰는 도구 ──────────────────────────────────────────

        /// <summary>가장 덜 아픈 자리부터. 한 단위를 뺄 때 고통이 얼마나 오르는지로 세운다.</summary>
        protected static string[] CheapestFirst(SimState st, Letter l)
        {
            List<SourceDef> src = new List<SourceDef>(l.Def.sources);
            src.Sort(delegate (SourceDef a, SourceDef b)
            {
                int ca = st.L.CostOfDrawing(a.soulId, l.Def.domain, 1);
                int cb = st.L.CostOfDrawing(b.soulId, l.Def.domain, 1);
                if (ca != cb) return ca - cb;
                int ha = st.L.Have(a.soulId, l.Def.domain), hb = st.L.Have(b.soulId, l.Def.domain);
                if (ha != hb) return hb - ha;
                return string.CompareOrdinal(a.soulId, b.soulId);
            });
            string[] outp = new string[src.Count];
            for (int i = 0; i < src.Count; i++) outp[i] = src[i].soulId;
            return outp;
        }

        /// <summary>가장 많이 가진 자리부터. 「넉넉해 보이는 사람에게서 빼면 된다」는 생각.</summary>
        protected static string[] RichestFirst(SimState st, Letter l)
        {
            List<SourceDef> src = new List<SourceDef>(l.Def.sources);
            src.Sort(delegate (SourceDef a, SourceDef b)
            {
                int ha = st.L.Have(a.soulId, l.Def.domain), hb = st.L.Have(b.soulId, l.Def.domain);
                if (ha != hb) return hb - ha;
                return string.CompareOrdinal(a.soulId, b.soulId);
            });
            string[] outp = new string[src.Count];
            for (int i = 0; i < src.Count; i++) outp[i] = src[i].soulId;
            return outp;
        }

        /// <summary>이 허가로 세상의 고통이 얼마나 줄어드는가. 음수면 들어주는 것이 손해다.</summary>
        protected static int NetGain(SimState st, Letter l, string[] order)
        {
            int before = st.L.TotalHardship();
            SimState c = st.Clone();
            if (!Office.Stamp(c, new Verdict { Kind = Verdicts.Grant, PrayerId = l.Def.id, SourceOrder = order }))
                return int.MinValue;
            return before - c.L.TotalHardship();
        }

        protected static Letter Oldest(SimState st)
        {
            Letter best = null;
            foreach (Letter l in st.Docket)
                if (best == null || l.ArrivedDay < best.ArrivedDay
                    || (l.ArrivedDay == best.ArrivedDay && string.CompareOrdinal(l.Def.id, best.Def.id) < 0))
                    best = l;
            return best;
        }

        public static List<Policy> All()
        {
            List<Policy> list = new List<Policy>();
            list.Add(new GrantEverything());
            list.Add(new GrantFromTheRich());
            list.Add(new RefuseEverything());
            list.Add(new HoldEverything());
            list.Add(new DeepestNeedFirst());
            list.Add(new OnlyWhenItHelps());
            list.Add(new RotateTheSeals());
            list.Add(new BreathBeforeBread());
            return list;
        }
    }
}
