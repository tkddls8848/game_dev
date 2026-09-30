using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SilentBaton.Data;
using SilentBaton.Sim;

namespace SilentBaton.Tests
{
    /// <summary>
    /// ★ 핵 검사기 하나 — `VisualCuesSuffice`.
    ///
    /// **맞는 박이 보이는 단서만으로 도출되는가.**
    /// 소리를 들어야만 알 수 있는 정보가 하나라도 필수면 이 게임은 성립하지 않는다.
    ///
    /// 세 겹으로 본다:
    ///   ① **구조** — 보이는 것을 담는 형에 숨은 값이 없고, 게임의 지휘자는 진실을 받을 길이 없다
    ///   ② **성적** — 보이는 셋(+어제 신문)만으로 곡 셋 × 씨드 넷 전부에서 합격한다
    ///   ③ **등가류 전수** — 보이는 이력이 **완전히 같은** 숨은 상태 전부에서도 합격을 유지한다
    ///      (소리로만 알 수 있는 차이가 남아 있어도 합격선을 넘는다는 뜻이다)
    ///   ④ **음영 대조군** — 채널을 하나 가리면 떨어진다. 안 떨어지면 그 채널은 장식이다
    ///
    /// 신문은 **읽는 것이고 듣는 것이 아니다.** 그래서 ②가 신문을 쓰는 것은 이 검사기의 취지에 맞다 —
    /// 백지 상태의 성적은 `DelayedFeedbackMatters` 가 따로 재고, 이 파일 ②의 끝에 수치로 남긴다.
    /// </summary>
    [TestFixture]
    public sealed class VisualCuesSufficeTests
    {
        // ── ① 구조 ──────────────────────────────────────────────────────────
        [Test]
        public void 보이는_것을_담는_형에_숨은_값이_없다()
        {
            // 누가 VisibleFrame 에 숨은 값을 하나 끼워 넣으면 이 검사기가 먼저 깨진다.
            HashSet<string> allowed = new HashSet<string>
            {
                "Beat", "Bar", "RequiredDynamic",      // 악보는 지휘자도 읽는다 — 소리가 아니다
                "Breath", "Bow", "Face",               // 보이는 셋
                "Playing", "BowReadable",              // 누가 연주 중인가 · 활이 보이는가
                "BreathQuantMs", "BowQuantMs", "FaceQuantLevel"  // 그 무리의 눈금
            };
            FieldInfo[] fields = typeof(VisibleFrame).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo f in fields)
                Assert.That(allowed.Contains(f.Name), Is.True,
                    "VisibleFrame 에 허락하지 않은 칸이 있다: " + f.Name
                    + " — 소리로만 알 수 있는 것이 섞였는지 먼저 확인한다");
            Assert.That(fields.Length, Is.EqualTo(allowed.Count),
                "VisibleFrame 의 칸이 " + fields.Length + "개다 (기대 " + allowed.Count + ")");
            TestContext.WriteLine("VisibleFrame 의 공개 칸 " + fields.Length + "개 전부가 보이는 것뿐이다: "
                + string.Join(", ", Names(fields)));
        }

        [Test]
        public void 게임의_지휘자는_진실을_받을_길이_구조적으로_없다()
        {
            // Performance 는 IHearsEveryBeat / IHearsBarEnd 를 구현한 지휘자에게만 진실을 건넨다.
            Assert.That(typeof(IHearsEveryBeat).IsAssignableFrom(typeof(VisibleOnlyConductor)), Is.False,
                "게임의 지휘자가 매 박 진실을 듣는 귀를 달았다");
            Assert.That(typeof(IHearsBarEnd).IsAssignableFrom(typeof(VisibleOnlyConductor)), Is.False,
                "게임의 지휘자가 소절 끝 진실을 듣는 귀를 달았다");
            // 대조군은 반대로 반드시 귀가 있어야 한다 — 없으면 비교가 성립하지 않는다.
            Assert.That(typeof(IHearsEveryBeat).IsAssignableFrom(typeof(HearingConductor)), Is.True);
            Assert.That(typeof(IHearsBarEnd).IsAssignableFrom(typeof(ImmediateFeedbackConductor)), Is.True);

            // IConductor.Decide 의 서명에 숨은 상태가 들어올 자리가 없다.
            MethodInfo decide = typeof(IConductor).GetMethod("Decide");
            foreach (ParameterInfo p in decide.GetParameters())
                Assert.That(p.ParameterType, Is.Not.EqualTo(typeof(Players)),
                    "Decide 가 숨은 상태를 받는다: " + p.Name);
            TestContext.WriteLine("Decide" + "(" + string.Join(", ",
                ParamNames(decide.GetParameters())) + ") — 숨은 상태가 들어올 자리가 없다");
        }

        [Test]
        public void 지휘자의_소스에_숨은_상태를_가리키는_이름이_하나도_없다()
        {
            // 형식 검사만으로는 부족하다. 본문을 글자로 훑는다 —
            // 누가 GameData 를 타고 진실에 닿는 길을 만들면 여기서 걸린다.
            // 주석은 걷어 내고 **코드만** 본다 — 주석에는 이 규칙을 설명하는 이름들이 당연히 나온다.
            string[] shared = { "Players", "BiasMs", "TraitBiasMs", "IHearsEveryBeat", "IHearsBarEnd",
                                "PerformanceResult", "Review.Write", "HearBeat", "HearBarEnd" };
            string[] injectionPoints = { "Overwrite", "SetRateHint", "OffsetMs", "DeltaMs" };
            string srcRoot = SrcRoot();

            foreach (string name in new[] { "VisibleOnlyConductor.cs", "DeadReckoning.cs" })
            {
                string path = Path.Combine(srcRoot, name);
                Assert.That(File.Exists(path), name + " 이 없다 — 검사기가 볼 파일이 사라졌다");
                string code = CodeOnly(File.ReadAllText(path));
                foreach (string bad in shared)
                    Assert.That(code, Does.Not.Contain(bad), name + " 의 코드가 숨은 상태를 가리킨다: " + bad);
                Assert.That(code, Does.Contain("VisibleFrame"), name + " 이 보이는 프레임을 읽지 않는다");
                TestContext.WriteLine(name + " — 금지어 " + shared.Length + "개 전부 0건 (주석 제외)");
            }

            // 게임의 지휘자는 머릿속 지도에 값을 **주입하는 자리**도 쓰지 않는다.
            // 그 두 자리는 대조군 지휘자만 쓴다 — DeadReckoning 은 둘이 함께 쓰는 살림이다.
            string game = CodeOnly(File.ReadAllText(Path.Combine(srcRoot, "VisibleOnlyConductor.cs")));
            foreach (string bad in injectionPoints)
                Assert.That(game, Does.Not.Contain(bad),
                    "VisibleOnlyConductor 가 값을 주입받는 자리를 쓴다: " + bad);
            TestContext.WriteLine("VisibleOnlyConductor.cs — 주입 자리 "
                + string.Join(", ", injectionPoints) + " 전부 0건");
        }

        // ── ② 성적 ──────────────────────────────────────────────────────────
        [Test]
        public void 신문을_읽은_뒤_보이는_셋만으로_모든_곡_모든_씨드에서_합격한다()
        {
            GameData d = TestWorld.Data;
            CheckerBalance cb = d.Balance.checkers;
            int pass = d.Balance.review.passTotal;
            int warmPassed = 0, coldPassed = 0, pairs = 0, worstWarm = 999;
            foreach (string pieceId in TestWorld.PieceIds())
                foreach (int seed in TestWorld.Seeds)
                {
                    PressKnowledge press = TuningTests.Warmed(d, seed);
                    PerformanceResult warm = TestWorld.Visible(pieceId, seed, press, VisibleChannel.None, seed);
                    PerformanceResult cold = TestWorld.Visible(pieceId, seed, null, VisibleChannel.None, seed);
                    pairs++;
                    if (warm.Passed) warmPassed++;
                    if (cold.Passed) coldPassed++;
                    if (warm.Total < worstWarm) worstWarm = warm.Total;
                    Assert.That(warm.Passed,
                        pieceId + " 씨드 " + seed + " 에서 보이는 셋만으로 합격하지 못했다 ("
                        + warm.Total + " < " + pass + ") — 이 컨셉이 성립하지 않는다는 뜻이다");
                }
            Assert.That(warmPassed, Is.EqualTo(cb.minWarmPassRate));
            Assert.That(coldPassed, Is.LessThanOrEqualTo(cb.maxColdPassRate),
                "백지 상태에서 " + coldPassed + "/" + pairs + " 를 넘겼다 — 신문이 필요 없다는 뜻이 된다");
            TestContext.WriteLine("신문을 읽은 뒤 " + warmPassed + "/" + pairs + " 합격 (가장 낮은 총점 "
                + worstWarm + " · 합격선 " + pass + ")");
            TestContext.WriteLine("백지 상태로는 " + coldPassed + "/" + pairs
                + " — 뒤늦은 피드백이 장식이 아니라 유일한 학습 통로다 (DelayedFeedbackMatters 가 따로 잰다)");
        }

        // ── ③ 등가류 전수 ───────────────────────────────────────────────────
        [Test]
        public void 보이는_이력이_같은_숨은_상태_전부에서_합격을_유지한다()
        {
            // ★ 이 검사가 이 PoC의 생사다.
            // 성향 쏠림을 격자로 전수하고, **지휘자가 본 것이 완전히 같은** 회차들을 한 묶음으로 묶는다.
            // 한 묶음 안의 회차들은 소리로만 구별된다 — 지휘자는 같은 지휘를 내렸다.
            // 그 묶음의 **최악값**도 합격선을 넘으면 "소리가 필수가 아니다"가 수치로 증명된다.
            GameData d = TestWorld.Data;
            CheckerBalance cb = d.Balance.checkers;
            // 활이 보이는 무리는 씨드가 뽑은 값을 그대로 두고, **활이 없는 무리만** 1ms 눈금으로 전수한다.
            // 활 눈금이 3~4ms 이므로 활이 보이는 무리의 성향은 거의 그대로 화면에 드러난다 —
            // 소리로만 구별되는 여지는 활이 없는 무리에만 있다.
            Performance probe = new Performance(d, d.AllPieces[0], d.AllPieces[0].seed);
            int[] baseTrait = probe.Cast(TestWorld.Seeds[0]).TraitBiasMs;
            List<int[]> grid = BiasGrid.BowlessOnly(d, cb.biasGridStepMs, baseTrait);
            Assert.That(grid.Count, Is.GreaterThan(100), "격자가 " + grid.Count + "가지뿐이다");

            int totalGroups = 0, multiGroups = 0, worstSpread = 0, worstTotal = 999;
            string worstWhere = null;
            foreach (string pieceId in TestWorld.PieceIds())
            {
                int seed = TestWorld.Seeds[0];
                PressKnowledge press = TuningTests.Warmed(d, seed);
                Dictionary<string, List<PerformanceResult>> groups =
                    new Dictionary<string, List<PerformanceResult>>();

                foreach (int[] trait in grid)
                {
                    PerformanceResult r = TestWorld.Visible(pieceId, seed, press, VisibleChannel.None, seed, trait);
                    string key = r.SeenKey;
                    List<PerformanceResult> bucket;
                    if (!groups.TryGetValue(key, out bucket)) { bucket = new List<PerformanceResult>(); groups[key] = bucket; }
                    bucket.Add(r);
                }

                totalGroups += groups.Count;
                foreach (KeyValuePair<string, List<PerformanceResult>> kv in groups)
                {
                    List<PerformanceResult> bucket = kv.Value;
                    if (bucket.Count >= cb.minGroupMembers) multiGroups++;

                    // 같은 것을 보았으면 같은 지휘를 내렸어야 한다. 아니면 지휘자가 숨은 값을 보고 있다.
                    string baton = bucket[0].BatonKey;
                    foreach (PerformanceResult r in bucket)
                        Assert.That(r.BatonKey, Is.EqualTo(baton),
                            pieceId + ": 본 것이 같은데 지휘가 달랐다 — 지휘자가 보이지 않는 것을 읽고 있다");

                    int lo = int.MaxValue, hi = int.MinValue;
                    foreach (PerformanceResult r in bucket)
                    {
                        if (r.Total < lo) lo = r.Total;
                        if (r.Total > hi) hi = r.Total;
                        Assert.That(r.Passed,
                            pieceId + ": 보이는 이력이 같은 " + bucket.Count + "개 회차 중 하나가 떨어졌다 ("
                            + r.Total + ") — 소리를 들어야만 알 수 있는 정보가 필수라는 뜻이다");
                    }
                    if (hi - lo > worstSpread) worstSpread = hi - lo;
                    if (lo < worstTotal) { worstTotal = lo; worstWhere = pieceId + " (묶음 " + bucket.Count + "개)"; }
                }
                TestContext.WriteLine(pieceId + ": 격자 " + grid.Count + "가지 → 본 것이 다른 묶음 "
                    + groups.Count + "개");
            }
            Assert.That(multiGroups, Is.GreaterThanOrEqualTo(cb.minEquivalenceGroups),
                "회차가 둘 이상인 등가류가 " + multiGroups + "개뿐이다 — 이 검사기가 아무것도 재지 못한다");
            TestContext.WriteLine("회차가 둘 이상인 등가류 " + multiGroups + "개 · 한 묶음 안의 총점 폭 최대 "
                + worstSpread + "점 · 그 최악값도 " + worstTotal + "점으로 합격선 "
                + d.Balance.review.passTotal + " 를 넘었다 (" + worstWhere + ")");
            TestContext.WriteLine("→ **소리로만 구별되는 차이가 최대 " + worstSpread
                + "점 남아 있지만, 그 차이로 떨어지지는 않는다.**");
        }

        // ── ④ 음영 대조군 ───────────────────────────────────────────────────
        [Test]
        public void 호흡과_활을_가리면_떨어진다()
        {
            GameData d = TestWorld.Data;
            int min = d.Balance.checkers.minAblationDropPoints;
            VisibleChannel[] load = { VisibleChannel.Breath, VisibleChannel.Bow };
            foreach (VisibleChannel ch in load)
            {
                int sum = 0, n = 0, worst = 999;
                foreach (string pieceId in TestWorld.PieceIds())
                    foreach (int seed in TestWorld.Seeds)
                    {
                        PressKnowledge press = TuningTests.Warmed(d, seed);
                        int full = TestWorld.Visible(pieceId, seed, press, VisibleChannel.None, seed).Total;
                        int masked = TestWorld.Visible(pieceId, seed, press, ch, seed).Total;
                        sum += full - masked; n++;
                        if (full - masked < worst) worst = full - masked;
                    }
                int mean = sum / n;
                Assert.That(mean, Is.GreaterThanOrEqualTo(min),
                    VisibleChannels.Korean(ch) + " 를 가려도 평균 " + mean + "점만 떨어진다 — 그 채널은 장식이다");
                TestContext.WriteLine(VisibleChannels.Korean(ch) + " 가림: 평균 낙폭 " + mean
                    + "점 · 가장 작은 낙폭 " + worst + "점 (기준 " + min + ")");
            }
        }

        [Test]
        public void 표정을_가리면_세기_점수가_떨어진다()
        {
            // ★ 정직하게 적는다: 표정을 가려도 **총점**은 2점밖에 떨어지지 않는다.
            // 지휘봉이 하나라서 세기에 쓰지 않은 박이 템포로 가고, 그 이득이 세기 손실을 거의 메운다.
            // 그러므로 총점으로는 "표정이 짐을 진다"를 증명할 수 없다 — 대신 **세기 점수**로 잰다.
            GameData d = TestWorld.Data;
            int min = d.Balance.checkers.minDynAblationDropPoints;
            int dynSum = 0, totalSum = 0, n = 0, worstDyn = 999;
            foreach (string pieceId in TestWorld.PieceIds())
                foreach (int seed in TestWorld.Seeds)
                {
                    PressKnowledge press = TuningTests.Warmed(d, seed);
                    PerformanceResult full = TestWorld.Visible(pieceId, seed, press, VisibleChannel.None, seed);
                    PerformanceResult masked = TestWorld.Visible(pieceId, seed, press, VisibleChannel.Face, seed);
                    int dynDrop = full.Review.Dynamics - masked.Review.Dynamics;
                    dynSum += dynDrop; totalSum += full.Total - masked.Total; n++;
                    if (dynDrop < worstDyn) worstDyn = dynDrop;
                }
            Assert.That(dynSum / n, Is.GreaterThanOrEqualTo(min),
                "표정을 가려도 세기 점수가 평균 " + (dynSum / n) + "점만 떨어진다 — 표정은 장식이다");
            TestContext.WriteLine("표정 가림: **세기 점수** 평균 낙폭 " + (dynSum / n)
                + "점 (가장 작은 낙폭 " + worstDyn + " · 기준 " + min + ")");
            TestContext.WriteLine("그러나 **총점** 낙폭은 평균 " + (totalSum / n)
                + "점뿐이다 — 세기에 쓰지 않은 박이 템포로 가서 손실을 메운다.");
            TestContext.WriteLine("→ 기계가 판정하지 못한 것: **표정이 총점을 지탱하는지.** "
                + "지휘봉 하나로 셋을 다 볼 수 없다는 상충이 재미인지 결함인지는 사람이 본다 (README 판정 칸).");
        }

        [Test]
        public void 세_채널의_눈금이_무리마다_다르고_전부_쓰인다()
        {
            // 무리마다 읽히는 방식이 다르다는 것이 데이터에 실제로 들어 있는지 본다.
            GameData d = TestWorld.Data;
            HashSet<int> breathQuants = new HashSet<int>();
            int bowless = 0, coarseFace = 0;
            for (int i = 0; i < d.SectionCount; i++)
            {
                breathQuants.Add(VisibleFrame.BreathQuant(d, i));
                if (!d.AllSections[i].bowVisible) bowless++;
                if (VisibleFrame.FaceQuant(d, i) > d.Balance.visible.faceQuantLevel) coarseFace++;
                TestContext.WriteLine(d.AllSections[i].name + ": 호흡 " + VisibleFrame.BreathQuant(d, i)
                    + "ms · 활 " + (d.AllSections[i].bowVisible ? VisibleFrame.BowQuant(d, i) + "ms" : "없음")
                    + " · 표정 " + VisibleFrame.FaceQuant(d, i) + "칸");
            }
            Assert.That(breathQuants.Count, Is.GreaterThanOrEqualTo(3),
                "호흡 눈금이 무리마다 같다 — 무리를 구별해 볼 이유가 없어진다");
            Assert.That(bowless, Is.GreaterThanOrEqualTo(2),
                "활이 없는 무리가 " + bowless + "개뿐이다 — 신문이 알려 줄 것이 없어진다");
            Assert.That(coarseFace, Is.GreaterThanOrEqualTo(1), "표정이 잘 안 보이는 무리가 없다");
        }

        private static string[] Names(FieldInfo[] fields)
        {
            string[] names = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++) names[i] = fields[i].Name;
            return names;
        }

        private static string[] ParamNames(ParameterInfo[] ps)
        {
            string[] names = new string[ps.Length];
            for (int i = 0; i < ps.Length; i++) names[i] = ps[i].ParameterType.Name + " " + ps[i].Name;
            return names;
        }

        /// <summary>주석을 걷어 내고 코드만 남긴다. 금지어 검사가 설명글에 걸리지 않게 한다.</summary>
        private static string CodeOnly(string text)
        {
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string line in lines)
            {
                string t = line.TrimStart();
                if (t.StartsWith("///") || t.StartsWith("//")) continue;
                int slash = line.IndexOf("//", StringComparison.Ordinal);
                sb.Append(slash >= 0 ? line.Substring(0, slash) : line).Append('\n');
            }
            return sb.ToString();
        }

        public static string SrcRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "src");
                if (File.Exists(Path.Combine(candidate, "Performance.cs"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("src/Performance.cs 를 찾지 못했다");
        }
    }
}
