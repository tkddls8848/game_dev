using System;
using System.Collections.Generic;
using Secretary.Data;

namespace Secretary.Sim
{
    /// <summary>
    /// 비서실. 하루에 봉랍 도장을 stampsPerDay 번 찍고, 도장을 못 받은 편지는 밤에 저절로 보류된다.
    ///
    /// **씨드가 하는 일은 하나뿐이다**: 편지마다 dayOptions 중 하나를 골라 도착 날을 정한다
    /// (뿌리 CLAUDE.md 설계 원칙 5 — System.Random 하나, 씨드는 데이터에 있다).
    /// 편지를 id 순서로 돌기 때문에 JSON 줄 순서를 바꿔도 결과가 같다.
    /// </summary>
    public static class Office
    {
        public static SimState Begin(GameData d, string volumeId, int seed)
        {
            return Begin(d, volumeId, seed, d.Balance.conservation.enabled);
        }

        public static SimState Begin(GameData d, string volumeId, int seed, bool conservation)
        {
            return Begin(d, d.Volume(volumeId), seed, conservation);
        }

        /// <summary>
        /// 장부를 직접 건네받아 시작한다. 검사기가 **일부러 고친 장부**로 돌릴 때 쓴다 —
        /// NoFreeGrant 의 대조군이 여기로 들어온다(원본 data/ 는 건드리지 않는다).
        /// </summary>
        public static SimState Begin(GameData d, VolumeDef v, int seed, bool conservation)
        {
            SimState st = new SimState();
            st.Data = d; st.Volume = v;
            st.L = new Ledger(d, v, conservation);
            Random rng = new Random(unchecked(v.seed * 31 + seed * 7919));
            foreach (PrayerDef p in d.PrayersOf(v.id))
                st.Pending.Add(new Letter { Def = p, ArrivedDay = p.dayOptions[rng.Next(p.dayOptions.Length)], Grown = 0 });
            st.Day = 0;
            st.StampsLeft = d.Balance.day.stampsPerDay;
            Arrive(st);
            return st;
        }

        private static void Arrive(SimState st)
        {
            for (int i = st.Pending.Count - 1; i >= 0; i--)
                if (st.Pending[i].ArrivedDay <= st.Day)
                {
                    st.Docket.Add(st.Pending[i]);
                    st.Pending.RemoveAt(i);
                }
            st.Docket.Sort(delegate (Letter a, Letter b) { return string.CompareOrdinal(a.Def.id, b.Def.id); });
        }

        /// <summary>이 편지를 지금 허가하면 실제로 건너갈 수 있는 양. 0 이면 허가가 뜻이 없다.</summary>
        public static int GrantableUnits(SimState st, Letter l)
        {
            int want = l.Def.askUnits + l.Grown;
            int deficit = st.L.Deficit(l.Def.fromSoulId, l.Def.domain);
            return Math.Min(want, deficit);
        }

        public static SourceDef SourceOf(PrayerDef p, string soulId)
        {
            foreach (SourceDef s in p.sources) if (s.soulId == soulId) return s;
            return null;
        }

        /// <summary>그 순서로 뺄 때 실제로 모을 수 있는 양.</summary>
        public static int DrawableUnits(SimState st, Letter l, IList<string> order, int want)
        {
            int remaining = want, got = 0;
            foreach (string sid in order)
            {
                SourceDef src = SourceOf(l.Def, sid);
                if (src == null) continue;
                int can = Math.Min(src.maxUnits, st.L.Have(sid, l.Def.domain));
                if (can > remaining) can = remaining;
                if (can <= 0) continue;
                got += can; remaining -= can;
                if (remaining <= 0) break;
            }
            return got;
        }

        /// <summary>도장 한 번. 찍을 수 없는 판정이면 false 를 주고 장부를 건드리지 않는다.</summary>
        public static bool Stamp(SimState st, Verdict v)
        {
            if (st.Finished || st.StampsLeft <= 0) return false;
            Letter l = st.Find(v.PrayerId);
            if (l == null) return false;

            VerdictRecord rec = new VerdictRecord
            {
                Day = st.Day, PrayerId = l.Def.id, Kind = v.Kind,
                FromSoulId = l.Def.fromSoulId, Domain = l.Def.domain
            };

            if (v.Kind == Verdicts.Grant)
            {
                int want = GrantableUnits(st, l);
                if (want <= 0) return false;
                if (v.SourceOrder == null || v.SourceOrder.Length == 0) return false;
                int remaining = want;
                foreach (string sid in v.SourceOrder)
                {
                    SourceDef src = SourceOf(l.Def, sid);
                    if (src == null) continue;
                    int can = Math.Min(src.maxUnits, st.L.Have(sid, l.Def.domain));
                    if (can > remaining) can = remaining;
                    if (can <= 0) continue;
                    rec.CostAtSources += st.L.CostOfDrawing(sid, l.Def.domain, can);
                    st.L.Draw(sid, l.Def.domain, can);
                    rec.Debits.Add(new DebitLine { SoulId = sid, Domain = l.Def.domain, Units = can });
                    rec.Drawn += can;
                    remaining -= can;
                    if (remaining <= 0) break;
                }
                if (rec.Drawn <= 0) return false;
                st.L.Give(l.Def.fromSoulId, l.Def.domain, rec.Drawn);
                rec.Granted = rec.Drawn;
                st.Grants++; st.GrantedUnits += rec.Granted; st.DrawnUnits += rec.Drawn;
                st.Docket.Remove(l);
            }
            else if (v.Kind == Verdicts.Deny)
            {
                st.Denies++;
                st.Docket.Remove(l);
            }
            else if (v.Kind == Verdicts.Defer)
            {
                rec.NeedGrew = Grow(st, l);
                st.Defers++;
            }
            else return false;

            string why;
            if (st.L.Conservation && !st.L.Balanced(out why)) st.ConservationBreaks.Add(why);
            st.Log.Add(rec);
            st.StampsLeft--;
            return true;
        }

        private static int Grow(SimState st, Letter l)
        {
            int cap = st.Data.Balance.day.deferCapUnits;
            int add = l.Def.deferPenaltyUnits;
            if (l.Grown + add > cap) add = cap - l.Grown;
            if (add <= 0) return 0;
            l.Grown += add;
            st.L.GrowNeed(l.Def.fromSoulId, l.Def.domain, add);
            return add;
        }

        /// <summary>하루를 닫는다. 도장을 못 받은 편지는 저절로 보류되고, 그날의 고통이 쌓인다.</summary>
        public static void EndDay(SimState st)
        {
            if (st.Finished) return;
            if (st.Data.Balance.day.autoDeferUnprocessed)
                foreach (Letter l in st.Docket)
                {
                    int grew = Grow(st, l);
                    if (grew > 0) st.AutoDefers++;
                }
            st.Accrued += st.L.TotalHardship();
            st.Day++;
            st.StampsLeft = st.Data.Balance.day.stampsPerDay;
            if (!st.Finished) Arrive(st);
        }

        /// <summary>이 편지에 대해 신이 쓸 수 있는 출처 순서 전부(최대 3곳이라 6가지).</summary>
        public static List<string[]> SourceOrders(PrayerDef p)
        {
            List<string[]> outp = new List<string[]>();
            string[] ids = new string[p.sources.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = p.sources[i].soulId;
            Permute(ids, 0, outp);
            return outp;
        }

        private static void Permute(string[] a, int k, List<string[]> outp)
        {
            if (k == a.Length) { outp.Add((string[])a.Clone()); return; }
            for (int i = k; i < a.Length; i++)
            {
                string t = a[k]; a[k] = a[i]; a[i] = t;
                Permute(a, k + 1, outp);
                t = a[k]; a[k] = a[i]; a[i] = t;
            }
        }
    }
}
