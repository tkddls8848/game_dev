using System;
using System.Collections.Generic;
using Tenants.Data;

namespace Tenants.Sim
{
    /// <summary>씨드가 정해 준 소리 하나. 언제 나는지와 거짓인지가 여기서 굳는다.</summary>
    public sealed class ResolvedCue
    {
        public string CueId;
        public string AtHouseholdId;
        public string AboutHouseholdId;
        public int Slot;
        public int LoudnessPercent;
        /// <summary>거짓으로 뽑혔으면 mislead 이고, 그 밖에는 데이터의 effect 그대로다.</summary>
        public string Effect;
        /// <summary>이 소리가 가리키는 need. 거짓이면 misleadNeedId 다.</summary>
        public string NeedId;
        public bool IsLie;
        public string Text;
    }

    /// <summary>
    /// 건물 하나의 하루. **씨드가 하는 일은 딱 둘이다.**
    ///
    ///   (1) 소리마다 slotOptions 중 하나를 고른다 — 같은 소리가 낮에 날 수도 새벽에 날 수도 있다
    ///   (2) falsifiable 인 소리 중 lieCountPerBuilding 개를 거짓으로 뽑는다
    ///
    /// 그 밖에 난수는 쓰지 않는다. System.Random 하나만 쓰고 씨드는 데이터에 있다
    /// (뿌리 CLAUDE.md 설계 원칙 5). 소리를 id 순서로 돌기 때문에 JSON 줄 순서를 바꿔도 결과가 같다.
    ///
    /// silenceAsSound 가 true 면 **대조 세계**다: 침묵한 칸에 대한 이웃의 말을 그 칸 자기 문으로
    /// 옮기고 게이트를 없앤다. 침묵이 소리 있는 칸과 똑같아진다 — SilenceIsInformation 이 쓰는 세계다.
    /// </summary>
    public sealed class Night
    {
        public GameData Data { get; private set; }
        public BuildingDef Building { get; private set; }
        public int Seed { get; private set; }
        public bool SilenceAsSound { get; private set; }
        public bool SilenceGate { get { return !SilenceAsSound && Data.Balance.silence.gateEnabled; } }

        public List<ResolvedCue> Cues { get; private set; }

        /// <summary>[세대 index][슬롯] 그 문에 그 시간대에 귀를 댔을 때 들리는 것들.</summary>
        public List<ResolvedCue>[][] Heard { get; private set; }

        public IList<HouseholdDef> Households { get; private set; }
        public int SlotCount { get; private set; }

        public static Night Resolve(GameData data, string buildingId, int seed)
        {
            return Resolve(data, buildingId, seed, false);
        }

        public static Night Resolve(GameData data, string buildingId, int seed, bool silenceAsSound)
        {
            Night n = new Night();
            n.Data = data;
            n.Building = data.Building(buildingId);
            n.Seed = seed;
            n.SilenceAsSound = silenceAsSound;
            n.Households = data.HouseholdsOf(buildingId);
            n.SlotCount = data.Balance.day.slotCount;

            Random rng = new Random(unchecked(n.Building.seed * 31 + seed * 7919));
            IList<CueDef> defs = data.CuesOf(buildingId);

            List<ResolvedCue> resolved = new List<ResolvedCue>();
            List<int> falsifiable = new List<int>();
            for (int i = 0; i < defs.Count; i++)
            {
                CueDef c = defs[i];
                ResolvedCue r = new ResolvedCue();
                r.CueId = c.id;
                r.AtHouseholdId = c.atHouseholdId;
                r.AboutHouseholdId = c.aboutHouseholdId;
                r.Slot = c.slotOptions[rng.Next(c.slotOptions.Length)];
                r.LoudnessPercent = c.loudnessPercent;
                r.Effect = c.effect;
                r.NeedId = c.needId;
                r.IsLie = false;
                r.Text = c.text;

                // 대조 세계: 침묵한 칸 얘기를 그 칸 자기 문으로 옮긴다.
                if (silenceAsSound
                    && r.AboutHouseholdId != r.AtHouseholdId
                    && data.IsSilent(r.AboutHouseholdId))
                    r.AtHouseholdId = r.AboutHouseholdId;

                if (c.falsifiable) falsifiable.Add(i);
                resolved.Add(r);
            }

            // 거짓을 뽑는다. Fisher-Yates 로 섞고 앞에서 lieCount 개.
            int lieCount = data.Balance.night.lieCountPerBuilding;
            for (int i = falsifiable.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = falsifiable[i]; falsifiable[i] = falsifiable[j]; falsifiable[j] = t;
            }
            for (int k = 0; k < lieCount && k < falsifiable.Count; k++)
            {
                ResolvedCue r = resolved[falsifiable[k]];
                r.IsLie = true;
                r.Effect = CueEffects.Mislead;
                r.NeedId = defs[falsifiable[k]].misleadNeedId;
            }

            n.Cues = resolved;

            n.Heard = new List<ResolvedCue>[n.Households.Count][];
            for (int i = 0; i < n.Households.Count; i++)
            {
                n.Heard[i] = new List<ResolvedCue>[n.SlotCount];
                for (int s = 0; s < n.SlotCount; s++) n.Heard[i][s] = new List<ResolvedCue>();
            }
            for (int i = 0; i < resolved.Count; i++)
            {
                int at = n.IndexOf(resolved[i].AtHouseholdId);
                if (at < 0) continue;
                n.Heard[at][resolved[i].Slot].Add(resolved[i]);
            }
            return n;
        }

        public int IndexOf(string householdId)
        {
            for (int i = 0; i < Households.Count; i++)
                if (Households[i].id == householdId) return i;
            return -1;
        }

        /// <summary>복도에서 그 문 앞을 지나며 알 수 있는 것 — 소리의 크기뿐이다.
        /// 침묵한 칸은 0 이고, 그 0 이 눈에 띄는 것이 이 PoC의 전부다.</summary>
        public int AmbientOf(int householdIndex)
        {
            return Households[householdIndex].ambientLoudnessPercent;
        }

        /// <summary>거짓으로 뽑힌 소리들. 검사기와 목업이 쓴다.</summary>
        public List<ResolvedCue> Lies()
        {
            List<ResolvedCue> outp = new List<ResolvedCue>();
            foreach (ResolvedCue r in Cues) if (r.IsLie) outp.Add(r);
            return outp;
        }
    }
}
