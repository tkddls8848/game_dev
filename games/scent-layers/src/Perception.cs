using System.Collections.Generic;
using Scent.Data;

namespace Scent.Sim
{
    /// <summary>한 번 맡아서 알아낸 것.</summary>
    public sealed class Reading
    {
        public string roomId;
        public int atMin;
        public int[] total;                                  // 계열별로 코에 잡힌 총량
        public List<string> recognized = new List<string>(); // 알아본 사람들
        public List<DatedLayer> dated = new List<DatedLayer>();
        public List<string> notes = new List<string>();      // 사람이 읽을 기록
    }

    public sealed class DatedLayer
    {
        public string layerId;
        public string actorId;
        public int fittedAtMin;
        public string fastNote;
        public string slowNote;
    }

    /// <summary>
    /// 후각 모형. 이 파일 하나가 이 PoC의 규칙 전부다.
    ///
    /// **알아보기(누가)** — 어느 계열에서 그 사람 몫이 방 전체의 dominancePercent 이상이면 알아본다.
    /// 사람마다 자기만 가진 잔향(marker)이 하나씩 있어서, 알아보는 것 자체는 늦게라도 늘 된다.
    ///
    /// **되짚기(언제)** — 어려운 쪽이다. 절대 세기로는 못 잰다:
    /// 그 사람이 그날 얼마나 진하게 남겼는지(strengthPercent)를 플레이어가 모르기 때문이다.
    /// 대신 **머리향과 잔향의 비율**을 본다 — 비율은 진하기에 상관없이 나이에만 달려 있다.
    /// 그러려면 두 계열 모두 purityPercent 이상 순수해야 하고 datingFloor 이상 남아 있어야 한다.
    ///
    /// 그래서 되짚기는 두 힘에 함께 물린다:
    ///   · 감쇠 — 머리향이 datingFloor 아래로 내려가면 끝이다 (늦으면 못 잰다)
    ///   · 섞임 — 남이 같은 계열을 더 많이 깔아 두면 순도가 안 나온다.
    ///            그 남의 몫이 perceptionFloor 아래로 내려가는 순간 비로소 열린다 (이르면 못 잰다)
    /// 두 힘이 각각 창의 뒷문과 앞문을 잡고 있고, 창이 방마다 어긋나 있다 → 되짚어 가야 한다.
    /// </summary>
    public sealed class Nose
    {
        private readonly GameData _d;
        private readonly ScentField _f;
        private readonly PerceptionDef _p;

        public Nose(GameData d, ScentField f) { _d = d; _f = f; _p = d.Balance.perception; }

        public Reading Sniff(string roomId, int atMin, World w)
        {
            int nn = _d.NoteCount;
            Reading r = new Reading { roomId = roomId, atMin = atMin, total = new int[nn] };

            IList<Layer> layers = _f.InRoom(roomId);
            List<Layer> present = new List<Layer>();
            foreach (Layer L in layers) if (L.atMin <= atMin) present.Add(L);

            int[][] sensed = new int[present.Count][];
            for (int i = 0; i < present.Count; i++)
            {
                sensed[i] = new int[nn];
                for (int n = 0; n < nn; n++)
                {
                    int v = _f.Sense(present[i], n, atMin, w);
                    sensed[i][n] = v;
                    r.total[n] += v;
                }
            }

            // 사람별로 모은다
            Dictionary<string, List<int>> byActor = new Dictionary<string, List<int>>();
            for (int i = 0; i < present.Count; i++)
            {
                if (!present[i].IsVisit) continue;
                string a = present[i].actorId;
                if (!byActor.ContainsKey(a)) byActor[a] = new List<int>();
                byActor[a].Add(i);
            }

            foreach (KeyValuePair<string, List<int>> kv in byActor)
            {
                int[] mine = new int[nn];
                bool any = false;
                foreach (int i in kv.Value)
                    for (int n = 0; n < nn; n++) { mine[n] += sensed[i][n]; if (sensed[i][n] > 0) any = true; }
                if (!any) continue;

                // 섞임을 끈 세계에서는 견줄 상대가 자기 자신뿐이다
                int[] against = w.mixing ? r.total : mine;

                bool recognized = false;
                for (int n = 0; n < nn; n++)
                {
                    if (mine[n] < _d.Notes.perceptionFloor) continue;
                    if ((long)mine[n] * 100 >= (long)_p.dominancePercent * against[n]) { recognized = true; break; }
                }
                if (!recognized) continue;
                r.recognized.Add(kv.Key);

                // 같은 사람이 이 방에 두 번 다녀갔으면 두 층이 겹쳐 더해진다 → 시각을 못 가른다
                if (kv.Value.Count != 1) { r.notes.Add(kv.Key + ": 두 층이 겹쳐 시각 미상"); continue; }

                Layer only = present[kv.Value[0]];
                DatedLayer dl = TryDate(only, sensed[kv.Value[0]], against, atMin, w);
                if (dl != null) r.dated.Add(dl);
            }

            r.recognized.Sort();
            r.dated.Sort((x, y) => string.CompareOrdinal(x.layerId, y.layerId));
            return r;
        }

        /// <summary>순수한 (머리향, 잔향) 쌍을 찾아 비율로 나이를 맞춘다. 맞는 나이가 하나뿐이어야 한다.</summary>
        private DatedLayer TryDate(Layer L, int[] mine, int[] against, int atMin, World w)
        {
            ActorDef actor = _d.Actor(L.actorId);

            // 감쇠가 없는 세계에서는 층이 굳어 있어 시각이 그대로 읽힌다 (대조군의 정의)
            if (!w.decay)
                return new DatedLayer { layerId = L.id, actorId = L.actorId, fittedAtMin = L.atMin, fastNote = "-", slowNote = "-" };

            int fast = -1, slow = -1;
            for (int n = 0; n < _d.NoteCount; n++)
            {
                if (mine[n] < _p.datingFloor) continue;
                if ((long)mine[n] * 100 < (long)_p.purityPercent * against[n]) continue;   // 남의 냄새가 섞여 있다
                int rp = _d.Note(n).retainPermille;
                if (fast < 0 || rp < _d.Note(fast).retainPermille) fast = n;
                if (slow < 0 || rp > _d.Note(slow).retainPermille) slow = n;
            }
            if (fast < 0 || slow < 0 || fast == slow) return null;

            long obs = (long)against[fast] * _p.ratioScale / against[slow];
            int iFast = _d.ProfileStrength(actor, _d.Note(fast).id);
            int iSlow = _d.ProfileStrength(actor, _d.Note(slow).id);
            if (iFast <= 0 || iSlow <= 0) return null;

            int hit = -1, hits = 0;
            for (int k = 0; k <= _p.maxAgeSteps; k++)
            {
                int pf = Decay.Remain(iFast, _d.Note(fast).retainPermille, k);
                int ps = Decay.Remain(iSlow, _d.Note(slow).retainPermille, k);
                if (ps <= 0) break;
                long pred = (long)pf * _p.ratioScale / ps;
                if (pred <= 0) break;
                long diff = obs > pred ? obs - pred : pred - obs;
                if (diff * 1000 <= (long)_p.ratioTolPermille * pred) { hits++; hit = k; if (hits > 1) return null; }
            }
            if (hits != 1) return null;

            return new DatedLayer
            {
                layerId = L.id,
                actorId = L.actorId,
                fittedAtMin = Decay.GridTimeFromSteps(atMin, hit, _d.StepMin),
                fastNote = _d.Note(fast).id,
                slowNote = _d.Note(slow).id
            };
        }
    }
}
