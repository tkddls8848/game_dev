using System;
using System.Collections.Generic;
using Scent.Data;

namespace Scent.Sim
{
    /// <summary>방에 얹힌 층 하나. 사람이 남긴 것이거나 집이 늘 풍기는 잡내다.</summary>
    public sealed class Layer
    {
        public string id;
        public string actorId;          // 잡내면 null
        public string roomId;
        public int atMin;
        public int[] initial;           // 계열별 처음 세기
        public bool IsVisit { get { return actorId != null; } }
    }

    /// <summary>
    /// 켜고 끌 수 있는 두 힘. **핵 검사기는 이 둘을 끈 세계와 켠 세계를 견준다.**
    /// mixing=false: 층이 서로 섞이지 않는다 — 코가 층 하나씩 따로 맡는다
    /// decay=false : 층이 옅어지지 않는다 — 세기가 굳어 있어 시각이 그대로 읽힌다
    /// </summary>
    public struct World
    {
        public bool mixing;
        public bool decay;
        public static World On { get { return new World { mixing = true, decay = true }; } }
        public static World Off { get { return new World { mixing = false, decay = false }; } }
        public static World MixingOnly { get { return new World { mixing = true, decay = false }; } }
        public static World DecayOnly { get { return new World { mixing = false, decay = true }; } }
        public string Name
        {
            get
            {
                return (mixing ? "섞임o" : "섞임x") + "·" + (decay ? "감쇠o" : "감쇠x");
            }
        }
    }

    /// <summary>집 전체의 냄새 지형. 방문 층 + 씨드가 만든 잡내 층.</summary>
    public sealed class ScentField
    {
        public GameData D { get; private set; }
        public int Seed { get; private set; }
        private readonly Dictionary<string, List<Layer>> _byRoom = new Dictionary<string, List<Layer>>();
        private readonly List<Layer> _all = new List<Layer>();

        public IList<Layer> All { get { return _all; } }
        public IList<Layer> InRoom(string roomId) { return _byRoom[roomId]; }

        public static ScentField Build(GameData d) { return Build(d, d.Day.seed); }

        public static ScentField Build(GameData d, int seed)
        {
            ScentField f = new ScentField();
            f.D = d;
            f.Seed = seed;
            foreach (string r in d.RoomIds) f._byRoom[r] = new List<Layer>();

            foreach (VisitDef v in d.Day.visits)
            {
                ActorDef a = d.Actor(v.actor);
                int[] init = new int[d.NoteCount];
                foreach (NoteWeight w in a.profile)
                {
                    long units = (long)d.Day.depositUnits * v.strengthPercent / 100;
                    init[d.NoteIndex(w.note)] = (int)(units * w.permille / 1000);
                }
                Layer L = new Layer { id = v.id, actorId = v.actor, roomId = v.room, atMin = v.atMin, initial = init };
                f._all.Add(L);
                f._byRoom[v.room].Add(L);
            }

            // 잡내 — 씨드가 만든다. System.Random 만 쓴다(설계 원칙 5).
            AmbientSpec s = d.Day.ambient;
            Random rng = new Random(seed);
            int n = 0;
            foreach (string roomId in d.RoomIds)
            {
                int count = s.perRoomMin + rng.Next(s.perRoomMax - s.perRoomMin + 1);
                for (int i = 0; i < count; i++)
                {
                    string noteId = s.notePool[rng.Next(s.notePool.Count)];
                    int strength = s.strengthMin + rng.Next(s.strengthMax - s.strengthMin + 1);
                    int steps = (s.atMinMax - s.atMinMin) / d.StepMin;
                    int at = s.atMinMin + rng.Next(steps + 1) * d.StepMin;
                    int[] init = new int[d.NoteCount];
                    init[d.NoteIndex(noteId)] = strength;
                    Layer L = new Layer { id = "amb_" + (++n).ToString("D2"), actorId = null, roomId = roomId, atMin = at, initial = init };
                    f._all.Add(L);
                    f._byRoom[roomId].Add(L);
                }
            }
            foreach (string r in d.RoomIds) f._byRoom[r].Sort((x, y) => x.atMin.CompareTo(y.atMin));
            return f;
        }

        /// <summary>
        /// 이 층의 이 계열이 지금 얼마나 남아 있는가. **역치 아래는 0이다** —
        /// 코에 잡히지 않는 것은 섞임에도 끼지 않는다. 이 문턱이 이 게임의 시간축을 만든다:
        /// 남의 냄새가 역치 아래로 내려가야 내 냄새가 비로소 또렷해진다.
        /// </summary>
        public int Sense(Layer L, int noteIdx, int atMin, World w)
        {
            if (atMin < L.atMin) return 0;
            int init = L.initial[noteIdx];
            if (init <= 0) return 0;
            int v = w.decay
                ? Decay.Remain(init, D.Note(noteIdx).retainPermille, Decay.Steps(L.atMin, atMin, D.StepMin))
                : init;
            return v < D.Notes.perceptionFloor ? 0 : v;
        }
    }
}
