using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    /// <summary>
    /// ★ **핵 검사기 2 의 뒤쪽 절반 — 두 세계 비교와 정책 대조군.**
    ///
    /// 침묵을 **소리 있는 칸과 똑같이 다룬 세계**(이웃의 말이 그 칸 자기 문에서 바로 들리는 세계)를
    /// 같은 씨드로 돌려 켠 세계와 견준다. 값이 달라야 침묵이 규칙이고, 같으면 장식이다.
    /// </summary>
    [TestFixture]
    public class SilenceTwoWorldsTests
    {
        private static List<string> WithUnable()
        {
            List<string> outp = new List<string>();
            foreach (string bid in TestWorld.BuildingIds())
                if (TestWorld.Silent(bid, SilenceKinds.Unable) != null) outp.Add(bid);
            return outp;
        }

        [Test]
        public void C_침묵을_소리로_바꾸면_정보가_싸진다()
        {
            GameData d = TestWorld.Data;
            int need = d.Balance.checkers.silenceWorldGapMin;
            StringBuilder sb = new StringBuilder();
            int maxGap = 0;
            foreach (string bid in WithUnable())
            {
                HouseholdDef u = TestWorld.Silent(bid, SilenceKinds.Unable);
                int sumOn = 0, sumAs = 0, gapMax = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceRow on = TestWorld.Matrix(bid, seed).Row(u.id);
                    ChoiceRow asSound = TestWorld.MatrixSilenceAsSound(bid, seed).Row(u.id);
                    Assert.That(asSound.Total, Is.LessThanOrEqualTo(on.Total),
                        bid + " 씨드" + seed + ": 침묵을 없앤 세계가 오히려 나쁘다 — 모델이 뒤집혀 있다");
                    Assert.That(asSound.Listens, Is.LessThan(on.Listens),
                        bid + " 씨드" + seed + ": 침묵을 없앴는데 엿듣기 횟수가 줄지 않았다 —"
                        + " 침묵이 값을 물리지 않는다는 뜻이다");
                    sumOn += on.Total; sumAs += asSound.Total;
                    int gap = on.Total - asSound.Total;
                    if (gap > gapMax) gapMax = gap;
                }
                if (gapMax > maxGap) maxGap = gapMax;
                sb.AppendLine(bid + " " + u.id + ": 켠 세계 합 " + sumOn + " · 소리로 바꾼 세계 합 " + sumAs
                              + " · 가장 크게 벌어진 날 " + gapMax + "점");
            }
            Assert.That(maxGap, Is.GreaterThanOrEqualTo(need),
                "어느 날에도 두 세계가 " + need + "점 이상 벌어지지 않는다 — 침묵이 장식이다");
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void C2_침묵이_이_PoC의_불확실성을_만든다()
        {
            // 켠 세계에서는 씨드에 따라 결과가 갈리고, 침묵을 없애면 한 값으로 굳는다.
            StringBuilder sb = new StringBuilder();
            foreach (string bid in WithUnable())
            {
                HouseholdDef u = TestWorld.Silent(bid, SilenceKinds.Unable);
                HashSet<int> on = new HashSet<int>(), asSound = new HashSet<int>();
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    on.Add(TestWorld.Matrix(bid, seed).Row(u.id).Total);
                    asSound.Add(TestWorld.MatrixSilenceAsSound(bid, seed).Row(u.id).Total);
                }
                Assert.That(on.Count, Is.GreaterThanOrEqualTo(2),
                    bid + ": 켠 세계에서 " + u.id + " 의 결과가 씨드와 무관하다 — 고를 이유가 씨드에 없다");
                Assert.That(asSound.Count, Is.EqualTo(1),
                    bid + ": 침묵을 없앤 세계에서도 결과가 " + asSound.Count
                    + "가지다 — 불확실성이 침묵에서만 오는 것이 아니다");
                sb.AppendLine(bid + ": 켠 세계 " + on.Count + "가지 · 소리로 바꾼 세계 " + asSound.Count + "가지");
            }
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void D_침묵을_맹신하는_정책은_어느_건물에서도_이기지_못한다()
        {
            GameData d = TestWorld.Data;
            int need = d.Balance.checkers.silencePolicyGapMin;
            StringBuilder sb = new StringBuilder();
            List<Policy> all = Policy.All();
            int minGap = int.MaxValue;
            foreach (string bid in TestWorld.BuildingIds())
            {
                Dictionary<string, int> sums = Sums(bid, all);
                int best = int.MaxValue;
                string bestName = null;
                foreach (KeyValuePair<string, int> kv in sums)
                    if (kv.Value < best) { best = kv.Value; bestName = kv.Key; }
                int silentFirst = sums["침묵한 칸부터"];
                int gap = (silentFirst - best) / TestWorld.SeedSweep;
                if (gap < minGap) minGap = gap;
                Assert.That(gap, Is.GreaterThanOrEqualTo(need),
                    bid + ": 침묵을 맹신하는 정책이 최선 정책(" + bestName + ")보다 한 회차당 "
                    + gap + "점밖에 나쁘지 않다 — 침묵이 그냥 정답이면 고민이 없다");
                sb.AppendLine(bid + ": 최선 정책 「" + bestName + "」 평균 " + (best / TestWorld.SeedSweep)
                              + " · 침묵 맹신 평균 " + (silentFirst / TestWorld.SeedSweep)
                              + " · 벌어짐 " + gap);
            }
            sb.AppendLine("가장 작은 벌어짐 " + minGap + " (기준 " + need + ")");
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void D2_침묵을_맹신하는_것과_무시하는_것이_모두_최선이_아니다()
        {
            // 이 둘이 **동시에** 벌을 받아야 침묵이 「확인해야 하는 신호」가 된다.
            // 맹신만 벌을 받으면 침묵은 함정이고, 무시만 벌을 받으면 침묵은 그냥 정답이다.
            StringBuilder sb = new StringBuilder();
            List<Policy> all = Policy.All();
            int flips = 0;
            bool skipAlwaysWorse = true, blindAlwaysWorse = true;
            int prevSign = 0;
            foreach (string bid in TestWorld.BuildingIds())
            {
                Dictionary<string, int> sums = Sums(bid, all);
                int best = int.MaxValue;
                string bestName = null;
                foreach (KeyValuePair<string, int> kv in sums)
                    if (kv.Value < best) { best = kv.Value; bestName = kv.Key; }
                int skip = sums["침묵한 칸은 건너뛴다"];
                int blind = sums["침묵한 칸부터"];
                if (skip <= best) skipAlwaysWorse = false;
                if (blind <= best) blindAlwaysWorse = false;
                int sign = skip == blind ? 0 : (skip < blind ? 1 : -1);
                if (prevSign != 0 && sign != 0 && sign != prevSign) flips++;
                if (sign != 0) prevSign = sign;
                sb.AppendLine(string.Format(
                    "{0}: 최선 「{1}」 {2} · 침묵 맹신 {3} · 침묵 무시 {4} · 무시가 맹신보다 {5}",
                    bid, bestName, best / TestWorld.SeedSweep, blind / TestWorld.SeedSweep,
                    skip / TestWorld.SeedSweep, skip < blind ? "낫다" : (skip > blind ? "나쁘다" : "같다")));
            }
            sb.AppendLine("무시가 맹신보다 나은지가 건물에 따라 뒤바뀐 횟수: " + flips);
            TestContext.Out.WriteLine(sb.ToString());

            Assert.That(blindAlwaysWorse, Is.True,
                "침묵을 맹신하는 정책이 어느 건물에서 최선이다 — 그러면 침묵은 확인할 것 없는 정답이다");
            Assert.That(skipAlwaysWorse, Is.True,
                "침묵을 무시하는 정책이 어느 건물에서 최선이다 — 그러면 침묵에 값이 물리지 않는다");
        }

        private static Dictionary<string, int> Sums(string bid, List<Policy> all)
        {
            Dictionary<string, int> sums = new Dictionary<string, int>();
            foreach (Policy p in all)
            {
                int sum = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                    sum += Policy.Run(TestWorld.Night(bid, seed), p).Total;
                sums[p.Name] = sum;
            }
            return sums;
        }
    }
}
