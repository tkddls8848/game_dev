using System.Text;
using SilentBaton.Data;

namespace SilentBaton.Sim
{
    /// <summary>백분율 배율의 나머지를 버리지 않고 누적한다 (설계 원칙 4).</summary>
    public static class Ratio
    {
        public static int Scale(int amount, int percent, ref int carry)
        {
            long t = (long)amount * percent + carry;
            int result = (int)(t / 100);
            carry = (int)(t % 100);
            return result;
        }

        /// <summary>정수만으로 가까운 눈금에 맞춘다. 반내림/반올림이 부호에 따라 갈라지지 않게 대칭으로 둔다.</summary>
        public static int Quantize(int value, int step)
        {
            if (step <= 0) return value;
            return value >= 0 ? (value + step / 2) / step : -((-value + step / 2) / step);
        }

        public static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }
    }

    /// <summary>
    /// ★ **지휘대에서 보이는 것 전부.** 이 형에 없는 값은 지휘자가 알 수 없다.
    ///
    /// 세 채널뿐이다:
    ///   Breath — 호흡. **누적 어긋남**의 눈금 (원의 크기)
    ///   Bow    — 활 각도. **한 박 사이의 변화**, 곧 쏠림의 속도 (기울기). 활이 없는 무리는 언제나 0이다
    ///   Face   — 표정. **세기의 어긋남**의 눈금 (획)
    ///
    /// 소리에 해당하는 값(정확한 ms, 정확한 세기, 다른 무리와의 정확한 차)은 **여기에 없다.**
    /// 검사기 VisualCuesSuffice 가 이 형의 공개 필드 목록을 반사로 확인한다 —
    /// 누가 숨은 값을 하나라도 끼워 넣으면 그 검사기가 깨진다.
    /// </summary>
    public sealed class VisibleFrame
    {
        public int Beat;
        public int Bar;
        /// <summary>이 소절에 악보가 요구하는 세기. 악보는 지휘자도 읽을 수 있다 — 소리가 아니다.</summary>
        public int RequiredDynamic;
        public int[] Breath;
        public int[] Bow;
        public int[] Face;
        public bool[] Playing;
        /// <summary>활이 보이는 무리인가. 목관·금관은 false — 속도를 볼 길이 없다.</summary>
        public bool[] BowReadable;
        /// <summary>그 무리의 눈금. **무리마다 다르다** — 지휘자는 자기 악단의 눈금을 안다.</summary>
        public int[] BreathQuantMs;
        public int[] BowQuantMs;
        public int[] FaceQuantLevel;

        public VisibleFrame(int sectionCount)
        {
            Breath = new int[sectionCount];
            Bow = new int[sectionCount];
            Face = new int[sectionCount];
            Playing = new bool[sectionCount];
            BowReadable = new bool[sectionCount];
            BreathQuantMs = new int[sectionCount];
            BowQuantMs = new int[sectionCount];
            FaceQuantLevel = new int[sectionCount];
        }

        public static int BreathQuant(GameData d, int i)
        {
            int o = d.AllSections[i].breathQuantMsOverride;
            return o > 0 ? o : d.Balance.visible.breathQuantMs;
        }

        public static int BowQuant(GameData d, int i)
        {
            int o = d.AllSections[i].bowQuantMsOverride;
            return o > 0 ? o : d.Balance.visible.bowQuantMs;
        }

        public static int FaceQuant(GameData d, int i)
        {
            int o = d.AllSections[i].faceQuantLevelOverride;
            return o > 0 ? o : d.Balance.visible.faceQuantLevel;
        }

        public VisibleFrame Copy()
        {
            VisibleFrame f = new VisibleFrame(Breath.Length)
            { Beat = Beat, Bar = Bar, RequiredDynamic = RequiredDynamic };
            for (int i = 0; i < Breath.Length; i++)
            {
                f.Breath[i] = Breath[i]; f.Bow[i] = Bow[i]; f.Face[i] = Face[i];
                f.Playing[i] = Playing[i]; f.BowReadable[i] = BowReadable[i];
                f.BreathQuantMs[i] = BreathQuantMs[i]; f.BowQuantMs[i] = BowQuantMs[i];
                f.FaceQuantLevel[i] = FaceQuantLevel[i];
            }
            return f;
        }

        /// <summary>채널 하나를 가린 사본. 음영 대조군이 이것으로 "그 채널이 짐을 지는가"를 본다.</summary>
        public VisibleFrame Masked(VisibleChannel channel)
        {
            VisibleFrame f = Copy();
            for (int i = 0; i < f.Breath.Length; i++)
            {
                if (channel == VisibleChannel.Breath) f.Breath[i] = 0;
                if (channel == VisibleChannel.Bow) { f.Bow[i] = 0; f.BowReadable[i] = false; }
                if (channel == VisibleChannel.Face) f.Face[i] = 0;
            }
            return f;
        }

        /// <summary>등가류를 묶는 열쇠. 이 문자열이 같으면 지휘자가 본 것이 완전히 같다.</summary>
        public void AppendKey(StringBuilder sb)
        {
            sb.Append(Bar).Append(':').Append(Beat).Append(':').Append(RequiredDynamic).Append('|');
            for (int i = 0; i < Breath.Length; i++)
                sb.Append(Playing[i] ? 1 : 0).Append(',').Append(Breath[i]).Append(',')
                  .Append(Bow[i]).Append(',').Append(Face[i]).Append(';');
            sb.Append('/');
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            AppendKey(sb);
            return sb.ToString();
        }

        /// <summary>숨은 상태에서 보이는 프레임을 만든다. **여기가 정보가 깎이는 단 한 곳이다.**</summary>
        public static VisibleFrame From(GameData d, Players p, BarDef bar, int beat)
        {
            VisibleBalance v = d.Balance.visible;
            VisibleFrame f = new VisibleFrame(d.SectionCount)
            { Beat = beat, Bar = bar.index, RequiredDynamic = bar.requiredDynamic };
            for (int i = 0; i < d.SectionCount; i++)
            {
                SectionDef sec = d.AllSections[i];
                f.Playing[i] = p.Playing[i];
                f.BowReadable[i] = sec.bowVisible;
                f.BreathQuantMs[i] = BreathQuant(d, i);
                f.BowQuantMs[i] = BowQuant(d, i);
                f.FaceQuantLevel[i] = FaceQuant(d, i);
                if (!p.Playing[i]) continue;
                if (sec.breathVisible)
                    f.Breath[i] = Ratio.Clamp(Ratio.Quantize(p.OffsetMs[i], f.BreathQuantMs[i]),
                                              -v.breathLevels, v.breathLevels);
                if (sec.bowVisible)
                    f.Bow[i] = Ratio.Clamp(Ratio.Quantize(p.DeltaMs[i], f.BowQuantMs[i]),
                                           -v.bowLevels, v.bowLevels);
                if (sec.faceVisible)
                    f.Face[i] = Ratio.Clamp(Ratio.Quantize(p.Dyn[i] - bar.requiredDynamic, f.FaceQuantLevel[i]),
                                            -v.faceLevels, v.faceLevels);
            }
            return f;
        }
    }

    public enum VisibleChannel { None = 0, Breath, Bow, Face }

    public static class VisibleChannels
    {
        public static VisibleChannel[] All
        {
            get { return new[] { VisibleChannel.Breath, VisibleChannel.Bow, VisibleChannel.Face }; }
        }

        public static string Korean(VisibleChannel c)
        {
            switch (c)
            {
                case VisibleChannel.Breath: return Localization.Text("chan.breath", "호흡");
                case VisibleChannel.Bow: return Localization.Text("chan.bow", "활 각도");
                case VisibleChannel.Face: return Localization.Text("chan.face", "표정");
                default: return Localization.Text("chan.none", "없음");
            }
        }
    }
}
