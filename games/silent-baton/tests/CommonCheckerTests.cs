using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SilentBaton.Data;
using SilentBaton.Sim;

namespace SilentBaton.Tests
{
    /// <summary>
    /// 공통 검사기 — `SeedDeterminism`. **나머지 전부의 전제다.**
    /// 같은 씨드로 두 번 돌려 같은 결과가 나오지 않으면 핵 검사기들이 재는 것이 아무것도 아니다.
    /// </summary>
    [TestFixture]
    public sealed class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드_같은_지휘자는_박_하나까지_같은_연주를_남긴다()
        {
            GameData d = TestWorld.Data;
            foreach (string pieceId in TestWorld.PieceIds())
                foreach (int seed in TestWorld.Seeds)
                {
                    string a = Transcript(TestWorld.Visible(pieceId, seed, null, VisibleChannel.None, seed));
                    string b = Transcript(TestWorld.Visible(pieceId, seed, null, VisibleChannel.None, seed));
                    Assert.That(b, Is.EqualTo(a), pieceId + " 씨드 " + seed + " 에서 두 연주가 갈라졌다");
                }
            TestContext.WriteLine("곡 " + d.AllPieces.Length + " × 씨드 " + TestWorld.Seeds.Length
                + " 전부 두 번 돌려 같았다");
        }

        [Test]
        public void 배역을_두_번_써도_상태가_더러워지지_않는다()
        {
            // Performance.Run 이 넘겨받은 배역을 그대로 쓰면 두 번째 회차가 첫 번째에 오염된다.
            GameData d = TestWorld.Data;
            PieceDef piece = d.AllPieces[0];
            Performance perf = new Performance(d, piece, piece.seed);
            Players cast = perf.Cast(TestWorld.SeasonSeed);
            int[] before = (int[])cast.OffsetMs.Clone();
            int[] beforeDyn = (int[])cast.Dyn.Clone();

            PerformanceResult a = perf.Run(new VisibleOnlyConductor(), cast);
            PerformanceResult b = perf.Run(new VisibleOnlyConductor(), cast);
            Assert.That(b.Total, Is.EqualTo(a.Total), "같은 배역으로 두 번 돌렸는데 성적이 달라졌다");
            Assert.That(b.SeenKey, Is.EqualTo(a.SeenKey));
            Assert.That(cast.OffsetMs, Is.EqualTo(before), "연주가 배역의 어긋남을 건드렸다");
            Assert.That(cast.Dyn, Is.EqualTo(beforeDyn), "연주가 배역의 세기를 건드렸다");
            TestContext.WriteLine("배역을 두 번 써도 " + a.Total + "점으로 같고 원본이 그대로다");
        }

        [Test]
        public void 씨드를_바꾸면_성향과_시작_어긋남이_달라진다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> traits = new HashSet<string>(), starts = new HashSet<string>();
            foreach (int seed in TestWorld.Seeds)
            {
                Performance perf = new Performance(d, d.AllPieces[0], d.AllPieces[0].seed + seed);
                Players p = perf.Cast(seed);
                Assert.That(traits.Add(string.Join(",", p.TraitBiasMs)),
                    "씨드 " + seed + " 의 성향이 다른 씨드와 같다");
                starts.Add(string.Join(",", p.OffsetMs));
                TestContext.WriteLine("씨드 " + seed + " 성향 " + string.Join(", ", p.TraitBiasMs)
                    + " · 시작 어긋남 " + string.Join(", ", p.OffsetMs) + "ms");
            }
            Assert.That(traits.Count, Is.EqualTo(TestWorld.Seeds.Length));
            Assert.That(starts.Count, Is.EqualTo(TestWorld.Seeds.Length));
        }

        [Test]
        public void 난수는_System_Random_뿐이고_씨드는_데이터에_적혀_있다()
        {
            GameData d = TestWorld.Data;
            Assert.That(d.Score.seasonSeed, Is.GreaterThan(0), "score.json 에 시즌 씨드가 없다");
            foreach (PieceDef p in d.AllPieces)
                Assert.That(p.seed, Is.GreaterThan(0), p.id + " 에 씨드가 없다");
            Assert.That(d.Balance.checkers.seeds, Is.Not.Null.And.Not.Empty);
            Assert.That(d.Balance.checkers.seeds, Contains.Item(d.Score.seasonSeed));
            TestContext.WriteLine("시즌 씨드 " + d.Score.seasonSeed + " · 곡 씨드 "
                + string.Join(", ", PieceSeeds(d)) + " · 검사 씨드 "
                + string.Join(", ", d.Balance.checkers.seeds));
        }

        private static List<int> PieceSeeds(GameData d)
        {
            List<int> s = new List<int>();
            foreach (PieceDef p in d.AllPieces) s.Add(p.seed);
            return s;
        }

        private static string Transcript(PerformanceResult r)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(r.Total).Append('|').Append(r.Review.Accuracy).Append('|').Append(r.Review.Ensemble)
              .Append('|').Append(r.Review.Dynamics).Append('|').Append(r.Review.GradeId).Append('\n');
            foreach (BeatRecord b in r.Log)
            {
                sb.Append(b.Bar).Append('-').Append(b.Beat).Append(':').Append(b.Given).Append(':')
                  .Append(b.SpreadMs).Append(',').Append(b.AbsOffsetSum).Append(',').Append(b.AbsDynSum)
                  .Append(':').Append(string.Join("/", b.OffsetAfter)).Append('\n');
                b.Seen.AppendKey(sb);
                sb.Append('\n');
            }
            foreach (PressNote n in r.Review.Notes) sb.Append('#').Append(n.Kind).Append(n.Text).Append('\n');
            return sb.ToString();
        }
    }

    /// <summary>
    /// 공통 검사기 — `DataValidator`. 참조 무결성 · 정수 규율 · 설계 원칙 게이트.
    /// 이 묶음이 통과하지 않으면 위의 검사기 전부가 무의미해진다.
    /// </summary>
    [TestFixture]
    public sealed class DataValidatorTests
    {
        private static string RootOf(string marker, string file)
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, marker);
                if (File.Exists(Path.Combine(candidate, file))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(marker + "/" + file + " 을 찾지 못했다");
        }

        [Test]
        public void 모든_참조가_이어진다()
        {
            GameData d = TestWorld.Data;
            Assert.That(d.SectionCount, Is.EqualTo(4), "단원 무리는 넷이다");
            HashSet<string> ids = new HashSet<string>();
            foreach (SectionDef s in d.AllSections)
            {
                Assert.That(ids.Add(s.id), "무리 id 가 겹친다: " + s.id);
                Assert.That(s.name, Is.Not.Null.And.Not.Empty);
                Assert.That(s.instrument, Is.Not.Null.And.Not.Empty, s.name + " 에 악기가 없다");
                Assert.That(s.note, Is.Not.Null.And.Not.Empty, s.name + " 에 설명이 없다");
                Assert.That(s.responsivenessPercent, Is.InRange(1, 100));
                Assert.That(s.biasRangeMs, Is.InRange(1, 60), s.name + " 의 쏠림 폭이 " + s.biasRangeMs);
                Assert.That(s.dynHabit, Is.InRange(0, 100));
                Assert.That(s.breathVisible, Is.True, s.name + " 의 호흡이 보이지 않는다 — 읽을 길이 없어진다");
            }

            Assert.That(d.AllPieces.Length, Is.EqualTo(3), "곡은 셋이다");
            foreach (PieceDef p in d.AllPieces)
            {
                Assert.That(p.bars, Is.Not.Null.And.Not.Empty);
                HashSet<string> entered = new HashSet<string>();
                int lastIndex = 0;
                foreach (BarDef bar in p.bars)
                {
                    Assert.That(bar.index, Is.EqualTo(lastIndex + 1), p.id + " 의 소절 번호가 이어지지 않는다");
                    lastIndex = bar.index;
                    Assert.That(bar.beats, Is.InRange(1, 12), p.id + " " + bar.index + "소절의 박이 " + bar.beats);
                    Assert.That(bar.beatMs, Is.InRange(100, 3000));
                    Assert.That(bar.requiredDynamic, Is.InRange(0, 100));
                    Assert.That(bar.note, Is.Not.Null.And.Not.Empty, p.id + " " + bar.index + "소절에 설명이 없다");
                    foreach (string sid in bar.enteringSectionIds)
                    {
                        d.Section(sid);
                        Assert.That(entered.Add(sid), p.id + " 에서 " + sid + " 가 두 번 들어온다");
                    }
                }
                Assert.That(entered.Count, Is.GreaterThanOrEqualTo(3),
                    p.id + " 에 무리가 " + entered.Count + "개만 들어온다");
                Assert.That(p.bars[0].enteringSectionIds.Length, Is.GreaterThan(0),
                    p.id + " 의 첫 소절에 들어오는 무리가 없다");
                Assert.That(p.TotalBeats, Is.GreaterThanOrEqualTo(12), p.id + " 가 너무 짧다");
            }

            Assert.That(d.AllCues.Length, Is.EqualTo(9), "지휘 동작은 아홉이다");
            int tempo = 0, dyn = 0, hold = 0;
            HashSet<int> nudges = new HashSet<int>();
            foreach (CueDef c in d.AllCues)
            {
                Assert.That(c.name, Is.Not.Null.And.Not.Empty);
                Assert.That(c.gesture, Is.Not.Null.And.Not.Empty, c.id + " 에 동작 설명이 없다 — 화면에 그릴 것이 없다");
                if (c.IsTempo) { tempo++; Assert.That(nudges.Add(c.tempoNudgeMs), "같은 세기의 동작이 둘이다: " + c.id); }
                if (c.IsDynamic) dyn++;
                if (!c.IsTempo && !c.IsDynamic) hold++;
            }
            Assert.That(tempo, Is.EqualTo(6), "템포 동작은 여섯이다 (당김 셋 · 늘림 셋)");
            Assert.That(dyn, Is.EqualTo(2), "세기 동작은 둘이다 (크게·작게)");
            Assert.That(hold, Is.EqualTo(1), "박만 주는 동작이 하나여야 한다");

            // 신문
            int lastMin = 101;
            foreach (GradeDef g in d.Press.grades)
            {
                Assert.That(g.minTotal, Is.LessThan(lastMin), "등급이 내림차순이 아니다: " + g.id);
                lastMin = g.minTotal;
                Assert.That(g.headline, Is.Not.Null.And.Not.Empty, g.id + " 에 표제가 없다");
                Assert.That(g.body, Is.Not.Null.And.Not.Empty, g.id + " 에 본문이 없다");
            }
            foreach (string kind in new[] { NoteKinds.Lag, NoteKinds.Rush, NoteKinds.Loud,
                                            NoteKinds.Soft, NoteKinds.Split })
            {
                NoteDef n = d.NoteOfKind(kind);
                Assert.That(n.text, Does.Contain("{0}"), n.id + " 가 무리 이름을 받지 않는다");
                // 조사(이/가·과/와)를 붙이면 「타」와 「목관」에서 갈라진다. 자리표시자 바로 뒤는 조사가 아니어야 한다.
                int at = n.text.IndexOf("{0}", StringComparison.Ordinal) + 3;
                if (at < n.text.Length)
                    Assert.That("이가은는과와을를".IndexOf(n.text[at]), Is.LessThan(0),
                        n.id + " 가 무리 이름 뒤에 조사를 붙인다: " + n.text);
            }
            Assert.That(d.NoteOfKind(NoteKinds.Split).text, Does.Contain("{1}"),
                "갈라짐 지적이 두 무리를 받지 않는다");

            TestContext.WriteLine("무리 " + d.SectionCount + " · 곡 " + d.AllPieces.Length
                + " (박 " + PieceBeats(d) + ") · 동작 " + d.AllCues.Length + " · 등급 "
                + d.Press.grades.Length + " · 지적 " + d.Press.notes.Length + " — 참조가 전부 이어진다");
        }

        [Test]
        public void 합격선이_등급표에서_온_값이다()
        {
            // 합격선을 결과에 맞춰 고르면 검사기가 죽는다. 등급표의 선을 그대로 쓴다.
            GameData d = TestWorld.Data;
            int pass = d.Balance.review.passTotal;
            bool matched = false;
            foreach (GradeDef g in d.Press.grades) if (g.minTotal == pass) matched = true;
            Assert.That(matched,
                "합격선 " + pass + " 이 등급표의 어느 선과도 같지 않다 — 결과를 보고 맞춘 값일 수 있다");
            GradeDef atPass = d.GradeOf(pass);
            TestContext.WriteLine("합격선 " + pass + " = 등급 「" + atPass.name + "」 의 선 그대로다");
        }

        [Test]
        public void 수치가_전부_정수이고_부동소수가_한_줄도_없다()
        {
            string dataRoot = RootOf("data", "score.json");
            Regex decimals = new Regex(@":\s*-?\d+\.\d");
            int files = 0;
            foreach (string path in Directory.GetFiles(dataRoot, "*.json"))
            {
                if (Path.GetFileName(path) == "browser-casts.json") continue;   // 테스트가 내보낸 산출물
                string text = File.ReadAllText(path);
                files++;
                Match m = decimals.Match(text);
                Assert.That(m.Success, Is.False,
                    Path.GetFileName(path) + " 에 부동소수가 있다: " + (m.Success ? m.Value : ""));
            }
            Assert.That(files, Is.EqualTo(5));
            TestContext.WriteLine("data/*.json " + files + "개에 부동소수 0건");
        }

        [Test]
        public void src_에_UnityEngine_의존이_없고_부동소수_연산이_없다()
        {
            string srcRoot = RootOf("src", "Performance.cs");
            int files = 0;
            foreach (string path in Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(path);
                files++;
                string name = Path.GetFileName(path);
                Assert.That(text, Does.Not.Contain("UnityEngine"), name);
                Assert.That(text, Does.Not.Contain("UnityEditor"), name);
                foreach (Match m in Regex.Matches(text, @"\bRandom\b"))
                {
                    string before = text.Substring(Math.Max(0, m.Index - 14), Math.Min(14, m.Index));
                    Assert.That(before, Does.Not.Contain("UnityEngine."), name);
                }
                // 설계 원칙 4: 부동소수 금지. float/double 이 한 번이라도 들어오면 재현이 흔들린다.
                Assert.That(Regex.IsMatch(text, @"\b(float|double|decimal)\b"), Is.False,
                    name + " 에 부동소수 형이 있다 — 정수와 나머지 누적만 쓴다");
                Assert.That(Regex.IsMatch(text, @"static\s+readonly\s+string\s+\w+\s*=\s*Localization"), Is.False,
                    name + " 이 번역 문자열을 static 필드에 굳혔다");
            }
            Assert.That(files, Is.GreaterThanOrEqualTo(8));
            TestContext.WriteLine("src/*.cs " + files + "개 — UnityEngine 0건 · 부동소수 형 0건");
        }

        [Test]
        public void 연출_파일이_데이터의_id_를_그대로_가리킨다()
        {
            string poc = Directory.GetParent(RootOf("data", "score.json")).FullName;
            string palettePath = Path.Combine(poc, "presentation", "palette.json");
            string htmlPath = Path.Combine(poc, "presentation", "index.html");
            Assert.That(File.Exists(palettePath), "presentation/palette.json 이 없다");
            Assert.That(File.Exists(htmlPath), "presentation/index.html 이 없다");

            // 목업은 손으로 옮겨 적은 수치가 아니라 data/ 를 읽어 그려야 한다.
            string all = File.ReadAllText(htmlPath);
            foreach (string js in Directory.GetFiles(Path.Combine(poc, "presentation"), "*.js"))
                all += File.ReadAllText(js);
            Assert.That(all, Does.Contain("../data/"), "목업이 data/ 를 읽지 않는다");
            foreach (string file in new[] { "sections", "score", "cues", "balance", "press" })
                Assert.That(all, Does.Contain("\"" + file + "\""),
                    "목업이 data/" + file + ".json 을 읽지 않는다");
            Assert.That(all, Does.Contain("palette.json"), "목업이 palette.json 을 읽지 않는다");
            // 규칙까지 화면에서 다시 계산한다 — 그 계산이 C# 과 같은지는 tests/browser-parity.js 가 대조한다.
            Assert.That(all, Does.Contain("balance.visible"), "목업이 눈금을 데이터에서 읽지 않는다");
            Assert.That(all, Does.Contain("balance.review"), "목업이 평점 기준을 데이터에서 읽지 않는다");

            string palette = File.ReadAllText(palettePath);
            Assert.That(palette, Does.Not.Contain("<연출 이름>"), "palette.json 에 자리표시자가 남아 있다");
            Assert.That(palette, Does.Not.Contain("<폰트 이름"), "palette.json 에 자리표시자가 남아 있다");
            TestContext.WriteLine("목업이 data/ 다섯과 palette.json 을 읽는다");
        }

        private static string PieceBeats(GameData d)
        {
            List<string> s = new List<string>();
            foreach (PieceDef p in d.AllPieces) s.Add(p.TotalBeats.ToString());
            return string.Join("+", s);
        }
    }

    /// <summary>
    /// 공통 검사기 — `NoDominantStrategy`.
    /// **한 무리에게 한 동작만 되풀이하는 것이 최적이 아닌가.**
    /// 최적이면 이 게임은 지휘가 아니라 한 번의 결심이다.
    /// </summary>
    [TestFixture]
    public sealed class NoDominantStrategyTests
    {
        [Test]
        public void 한_동작_되풀이는_어디서도_보이는_지휘자를_못_따라간다()
        {
            GameData d = TestWorld.Data;
            CheckerBalance cb = d.Balance.checkers;
            int worstGap = 9999, passing = 0, monoSum = 0, n = 0;
            string worstWhere = null, bestMonoLabel = null;
            int bestMono = -1;

            foreach (string pieceId in TestWorld.PieceIds())
                foreach (int seed in TestWorld.Seeds)
                {
                    PressKnowledge press = TuningTests.Warmed(d, seed);
                    int visible = TestWorld.Visible(pieceId, seed, press, VisibleChannel.None, seed).Total;
                    int best = -1; string label = null;
                    for (int s = 0; s < d.SectionCount; s++)
                        foreach (CueDef c in d.AllCues)
                        {
                            string name = d.AllSections[s].name + "/" + c.name;
                            int t = TestWorld.Play(pieceId, seed, new MonoConductor(s, c.id, name), seed).Total;
                            if (t > best) { best = t; label = name; }
                        }
                    if (best >= d.Balance.review.passTotal) passing++;
                    if (best > bestMono) { bestMono = best; bestMonoLabel = pieceId + " " + label; }
                    monoSum += best; n++;
                    int gap = visible - best;
                    if (gap < worstGap) { worstGap = gap; worstWhere = pieceId + " 씨드 " + seed + " (" + label + ")"; }
                }

            Assert.That(worstGap, Is.GreaterThanOrEqualTo(cb.monoDominanceMarginPoints),
                "가장 좁은 자리에서 한 동작 되풀이가 보이는 지휘자에 " + worstGap + "점까지 붙었다 ("
                + worstWhere + ")");
            Assert.That(monoSum / n, Is.LessThan(d.Balance.review.passTotal),
                "한 동작 되풀이의 평균이 " + (monoSum / n) + "점으로 합격선을 넘는다");
            Assert.That(passing, Is.LessThanOrEqualTo(cb.maxMonoPassing),
                "한 동작 되풀이가 " + passing + "/" + n + " 에서 합격한다");

            TestContext.WriteLine("한 동작 되풀이: 평균 " + (monoSum / n) + "점 · 최고 " + bestMono
                + "점 (" + bestMonoLabel + ") · 합격 " + passing + "/" + n
                + " (허용 " + cb.maxMonoPassing + ")");
            TestContext.WriteLine("보이는 지휘자와의 가장 좁은 차 " + worstGap + "점 (" + worstWhere
                + ", 기준 " + cb.monoDominanceMarginPoints + ")");
            if (passing > 0)
                TestContext.WriteLine("→ 기계가 남긴 것: 합격선에 턱걸이하는 한 동작이 " + passing
                    + "곳 있다. **0으로 만들지 못했고 기준을 그 수로 적어 남겼다** (README 판정 칸).");
        }

        [Test]
        public void 쓸모없는_동작이_없다()
        {
            // 보이는 지휘자가 실제로 쓰는 동작이 몇 가지인가. 한 가지만 쓰면 나머지는 장식이다.
            GameData d = TestWorld.Data;
            Dictionary<string, int> used = new Dictionary<string, int>();
            foreach (CueDef c in d.AllCues) used[c.id] = 0;
            int[] sectionUse = new int[d.SectionCount];

            foreach (string pieceId in TestWorld.PieceIds())
                foreach (int seed in TestWorld.Seeds)
                {
                    PressKnowledge press = TuningTests.Warmed(d, seed);
                    PerformanceResult r = TestWorld.Visible(pieceId, seed, press, VisibleChannel.None, seed);
                    foreach (BeatRecord b in r.Log)
                    {
                        used[b.Given.CueId] = used[b.Given.CueId] + 1;
                        sectionUse[b.Given.SectionIndex]++;
                    }
                }

            List<string> never = new List<string>();
            foreach (KeyValuePair<string, int> kv in used)
            {
                TestContext.WriteLine(d.Cue(kv.Key).name.PadRight(16) + kv.Value + "번");
                if (kv.Value == 0) never.Add(d.Cue(kv.Key).name);
            }
            for (int i = 0; i < d.SectionCount; i++)
            {
                Assert.That(sectionUse[i], Is.GreaterThan(0),
                    d.AllSections[i].name + " 에게 지휘봉이 한 번도 가지 않았다");
                TestContext.WriteLine(d.AllSections[i].name + " 에게 " + sectionUse[i] + "번");
            }
            Assert.That(never.Count, Is.LessThanOrEqualTo(2),
                "한 번도 쓰이지 않은 동작이 " + never.Count + "개다: " + string.Join(", ", never));
            if (never.Count > 0)
                TestContext.WriteLine("→ 쓰이지 않은 동작: " + string.Join(", ", never)
                    + " — 자동 지휘자가 안 쓴다는 뜻이고, 사람이 쓸지는 기계가 모른다");
        }

        [Test]
        public void 지휘봉은_한_박에_한_무리만_간다()
        {
            // 이 제약이 이 게임의 긴장 전부다. 깨지면 셋을 동시에 고칠 수 있게 되고 난이도가 사라진다.
            GameData d = TestWorld.Data;
            PerformanceResult r = TestWorld.Visible(TestWorld.PieceIds()[2], TestWorld.Seeds[0],
                                                   null, VisibleChannel.None, TestWorld.Seeds[0]);
            foreach (BeatRecord b in r.Log)
            {
                Assert.That(b.Given.SectionIndex, Is.InRange(0, d.SectionCount - 1));
                d.Cue(b.Given.CueId);
            }
            Assert.That(r.Log.Count, Is.EqualTo(d.Piece(TestWorld.PieceIds()[2]).TotalBeats),
                "박 수와 기록 수가 다르다");
            TestContext.WriteLine("박 " + r.Log.Count + "개 · 각 박에 지휘봉 하나");
        }
    }

    /// <summary>설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.</summary>
    [TestFixture]
    public sealed class LocalizationTests
    {
        [TearDown]
        public void 되돌린다() { Localization.Language = "ko"; }

        [Test]
        public void 표에_없는_키는_한국어_원문을_그대로_낸다()
        {
            Localization.Language = "en";
            Assert.That(Localization.Text("없는.키", "소리 없는 지휘대"), Is.EqualTo("소리 없는 지휘대"));
        }

        [Test]
        public void 언어를_바꾸면_채널_이름이_그_자리에서_따라온다()
        {
            Assert.That(VisibleChannels.Korean(VisibleChannel.Bow), Is.EqualTo("활 각도"));
            Localization.Language = "en";
            Assert.That(VisibleChannels.Korean(VisibleChannel.Bow), Is.EqualTo("bow angle"));
            Localization.Language = "ko";
            Assert.That(VisibleChannels.Korean(VisibleChannel.Bow), Is.EqualTo("활 각도"));
        }

        [Test]
        public void 모든_채널과_지휘자에_이름이_있다()
        {
            foreach (VisibleChannel c in VisibleChannels.All)
            {
                Assert.That(VisibleChannels.Korean(c), Is.Not.Null.And.Not.Empty);
                Localization.Language = "en";
                string en = VisibleChannels.Korean(c);
                Localization.Language = "ko";
                Assert.That(en, Is.Not.Null.And.Not.Empty, c + " 의 영어가 없다");
                TestContext.WriteLine(c + " → " + VisibleChannels.Korean(c) + " / " + en);
            }
            IConductor[] all = { new VisibleOnlyConductor(), new HearingConductor(),
                                 new ImmediateFeedbackConductor() };
            foreach (IConductor c in all)
            {
                Assert.That(c.Name, Is.Not.Null.And.Not.Empty);
                Localization.Language = "en";
                Assert.That(c.Name, Is.Not.Null.And.Not.Empty);
                Localization.Language = "ko";
            }
        }

        [Test]
        public void 평론은_조각을_이어_붙이지_않고_자리표시자를_쓴다()
        {
            GameData d = TestWorld.Data;
            NoteDef split = d.NoteOfKind(NoteKinds.Split);
            Localization.Language = "en";
            string s = Localization.Text("press.split", split.text, "현", "금관");
            Assert.That(s, Is.EqualTo("현, 금관 — two different beats"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("press.split", split.text, "현", "금관"),
                Is.EqualTo("현, 금관 — 둘이 서로 다른 박을 세고 있었다"));
        }

        [Test]
        public void 평론_문구가_실제_회차에서_한국어로_나온다()
        {
            PerformanceResult r = TestWorld.Visible(TestWorld.PieceIds()[2], TestWorld.Seeds[0],
                                                   null, VisibleChannel.None, TestWorld.Seeds[0]);
            Assert.That(r.Review.Notes, Is.Not.Empty, "평론에 지적이 하나도 없다");
            foreach (PressNote n in r.Review.Notes)
            {
                Assert.That(n.Text, Is.Not.Null.And.Not.Empty);
                Assert.That(n.Text, Does.Not.Contain("{0}"), "자리표시자가 채워지지 않았다: " + n.Text);
            }
            TestContext.WriteLine(r.Review.GradeName + " — " + r.Review.Headline);
            foreach (PressNote n in r.Review.Notes) TestContext.WriteLine("   " + n.Text);
        }
    }
}
