using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using CurseLedger.Data;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// 공통 검사기 — `DataValidator`. 참조 무결성 · 정수 규율 · 설계 원칙 게이트.
    /// 이 묶음이 통과하지 않으면 위의 검사기 전부가 무의미해진다 — 데이터가 거짓말을 하고 있다는 뜻이므로.
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

            // 족보는 대의 수보다 길어야 한다 — 8대를 넘겨도 다음 대의 이름이 있어야 장부가 이어진다.
            Assert.That(d.AllHeirs.Count, Is.GreaterThan(TestWorld.Generations),
                "족보가 대의 수보다 짧다 — 마지막 대에 종손 이름이 없다");
            HashSet<int> gens = new HashSet<int>();
            foreach (HeirDef h in d.AllHeirs)
            {
                Assert.That(h.generationIndex, Is.GreaterThan(0), h.id + " 의 대 번호가 없다");
                Assert.That(gens.Add(h.generationIndex), "같은 대에 종손이 둘이다: " + h.generationIndex);
                Assert.That(h.name, Is.Not.Null.And.Not.Empty);
                Assert.That(h.note, Is.Not.Null.And.Not.Empty, h.name + " 에 족보 주석이 없다");
                Assert.That(h.bornYear, Is.InRange(1500, 2100), h.name + " 의 생년이 " + h.bornYear);
            }
            for (int g = 1; g <= TestWorld.Generations; g++) d.HeirOfGeneration(g);

            // 물려받은 청구서의 출처가 족보에 있어야 한다.
            foreach (PendingDef p in d.Curse.inheritedPending)
            {
                d.Heir(p.from);
                Assert.That(p.atGeneration, Is.InRange(1, TestWorld.Generations),
                    "물려받은 청구서가 " + p.atGeneration + "대에 도착한다 — 대의 범위 밖이다");
                Assert.That(p.delayGenerations, Is.EqualTo(-1),
                    "물려받은 청구서는 도착 대를 직접 적는다. 값 없는 int 는 -1 이어야 한다 (설계 원칙 3)");
                Assert.That(p.note, Is.Not.Null.And.Not.Empty);
            }

            // 앞선 대의 장부가 실제 제례 id 를 가리켜야 한다 — 목업이 이것으로 위쪽 칸을 그린다.
            int prior = 0;
            foreach (PriorLine l in d.Curse.priorLedger)
            {
                if (string.IsNullOrEmpty(l.riteId)) continue;   // 주석 줄
                prior++;
                d.Rite(l.riteId);
                d.Heir(l.heirId);
                Assert.That(l.generation, Is.GreaterThan(0));
                RiteDef r = d.Rite(l.riteId);
                int names = l.victimNames == null ? 0 : l.victimNames.Length;
                if (r.TakesVictim) Assert.That(names, Is.GreaterThan(0),
                    l.heirId + " 는 사람을 바쳤다고 적혔는데 이름이 없다");
                else Assert.That(names, Is.Zero, l.heirId + " 는 사람을 바치지 않았는데 이름이 적혀 있다");
            }
            Assert.That(prior, Is.GreaterThanOrEqualTo(3), "앞선 대의 장부가 셋보다 적다 — '대대로'가 되지 않는다");

            // 제례
            Assert.That(d.AllRites.Count, Is.EqualTo(6), "제례는 여섯이다");
            HashSet<string> riteIds = new HashSet<string>();
            foreach (RiteDef r in d.AllRites)
            {
                Assert.That(riteIds.Add(r.id), "제례 id 가 겹친다: " + r.id);
                Assert.That(r.name, Is.Not.Null.And.Not.Empty);
                Assert.That(r.note, Is.Not.Null.And.Not.Empty, r.id + " 에 설명이 없다");
                Assert.That(r.payPercent, Is.InRange(0, 200), r.id + " 의 갚는 비율이 " + r.payPercent);
                Assert.That(r.immediate, Is.Not.Null, r.id + " 에 즉시 효과 칸이 없다");
                if (r.TakesVictim)
                    Assert.That(r.requiresVictimTier, Is.AnyOf(Tiers.Elder, Tiers.Young),
                        r.id + " 가 없는 계층을 요구한다: " + r.requiresVictimTier);
                foreach (PendingDef p in r.deferred)
                    Assert.That(p.atGeneration, Is.EqualTo(-1),
                        r.id + " 의 지연 청구서가 절대 대를 적었다. 상대 지연만 쓴다 (값 없는 int 는 -1)");
            }
            foreach (string need in new[] { "offer_elder", "offer_young", "house_pays",
                                            "rite_reinforce", "rite_release", "close_ledger" })
                d.Rite(need);

            TestContext.WriteLine("족보 " + d.AllHeirs.Count + "대 · 명단 " + d.AllVillagers.Count
                + "명 · 제례 " + d.AllRites.Count + " · 앞선 장부 " + prior + "줄 · 물려받은 청구서 "
                + d.Curse.inheritedPending.Length + "장 — 참조가 전부 이어진다");
        }

        [Test]
        public void 명단이_여덟_대를_버틸_만큼_있다()
        {
            GameData d = TestWorld.Data;
            int elders = d.TierRoster(Tiers.Elder).Count;
            int young = d.TierRoster(Tiers.Young).Count;
            Assert.That(elders, Is.GreaterThanOrEqualTo(TestWorld.Generations - 1),
                "늙은 이가 " + elders + "명뿐이다 — 명단이 바닥나는 것이 사실상의 규칙이 된다");
            Assert.That(young, Is.GreaterThanOrEqualTo(TestWorld.Generations),
                "젊은 이가 " + young + "명뿐이다");
            TestContext.WriteLine("늙은 이 " + elders + " · 젊은 이 " + young + " · 대 " + TestWorld.Generations);
        }

        [Test]
        public void 수치가_전부_정수이고_부동소수가_한_줄도_없다()
        {
            // 설계 원칙 4. 데이터에 소수점이 하나 들어오면 "0.999대 자란 저주"가 생긴다.
            string dataRoot = RootOf("data", "curse.json");
            Regex decimals = new Regex(@":\s*-?\d+\.\d");
            foreach (string path in Directory.GetFiles(dataRoot, "*.json"))
            {
                string text = File.ReadAllText(path);
                Match m = decimals.Match(text);
                Assert.That(m.Success, Is.False,
                    Path.GetFileName(path) + " 에 부동소수가 있다: " + (m.Success ? m.Value : ""));
            }
            TestContext.WriteLine("data/*.json 에 부동소수 0건");
        }

        [Test]
        public void src_에_UnityEngine_의존이_없다()
        {
            // 설계 원칙 1. 이 게이트가 깨지면 이 PoC는 헤드리스로 돌지 않는다.
            string srcRoot = RootOf("src", "CurseSim.cs");
            int files = 0;
            foreach (string path in Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(path);
                files++;
                Assert.That(text, Does.Not.Contain("UnityEngine"), Path.GetFileName(path));
                Assert.That(text, Does.Not.Contain("UnityEditor"), Path.GetFileName(path));
                // 설계 원칙 5: UnityEngine.Random 금지. System.Random 만 쓴다.
                foreach (Match m in Regex.Matches(text, @"\bRandom\b"))
                {
                    int at = m.Index;
                    string before = text.Substring(Math.Max(0, at - 14), Math.Min(14, at));
                    Assert.That(before, Does.Not.Contain("UnityEngine."), Path.GetFileName(path));
                }
                // 설계 원칙 6: 로컬라이제이션 문자열을 static 필드에 굳히지 않는다.
                Assert.That(Regex.IsMatch(text, @"static\s+readonly\s+string\s+\w+\s*=\s*Localization"), Is.False,
                    Path.GetFileName(path) + " 가 번역 문자열을 static 필드에 굳혔다");
            }
            Assert.That(files, Is.GreaterThanOrEqualTo(6));
            TestContext.WriteLine("src/*.cs " + files + "개 — UnityEngine 참조 0건");
        }

        [Test]
        public void 시작_상태와_한계값이_서로_어긋나지_않는다()
        {
            GameData d = TestWorld.Data;
            StartState s = d.Curse.start;
            LimitBalance l = d.Balance.limits;
            EndingBalance e = d.Balance.ending;

            Assert.That(s.prosperity, Is.InRange(1, l.prosperityMax));
            Assert.That(s.binding, Is.InRange(1, l.bindingMax));
            Assert.That(s.wrath, Is.InRange(0, l.wrathMax));
            Assert.That(s.houseVitality, Is.InRange(1, l.houseVitalityMax));
            Assert.That(s.resentment, Is.InRange(0, e.uprisingResentment - 1),
                "시작부터 원한이 봉기선에 닿아 있다");
            Assert.That(s.releaseProgress, Is.LessThan(e.releaseComplete));
            Assert.That(s.prosperity, Is.GreaterThan(e.villageAliveFloor),
                "시작부터 마을이 죽은 것으로 센다");
            Assert.That(e.releaseFamineDrop, Is.GreaterThan(0),
                "기근 낙차가 0이면 '저주를 풀면 마을이 몰락한다'가 거짓이 된다");
            Assert.That(d.Curse.generations, Is.EqualTo(d.Balance.checkers.exhaustiveGenerations),
                "게임의 대 수와 전수 훑기의 대 수가 다르다 — 검사기가 다른 게임을 보고 있다");

            // 저주가 주는 풍요와 노여움이 주는 재앙이 서로를 지우지 않아야 한다.
            FlowBalance f = d.Balance.flow;
            Assert.That(f.blessingPerBindingPercent, Is.GreaterThan(0));
            Assert.That(f.blightPerWrathPercent, Is.GreaterThan(f.blessingPerBindingPercent),
                "노여움이 속박보다 약하면 미납이 벌이 되지 않는다");
            TestContext.WriteLine("속박 " + s.binding + " → 대마다 번영 +"
                + (s.binding * f.blessingPerBindingPercent / 100 - f.prosperityDecayPerGeneration)
                + " (감소 " + f.prosperityDecayPerGeneration + " 포함) · 노여움 1당 -"
                + f.blightPerWrathPercent + "%");
        }

        [Test]
        public void 연출_파일이_데이터의_id_를_그대로_가리킨다()
        {
            // palette.json 과 index.html 이 손으로 옮겨 적은 수치로 그리면 다른 PoC와 견줄 수 없다.
            string poc = Directory.GetParent(RootOf("data", "curse.json")).FullName;
            string palettePath = Path.Combine(poc, "presentation", "palette.json");
            string htmlPath = Path.Combine(poc, "presentation", "index.html");
            Assert.That(File.Exists(palettePath), "presentation/palette.json 이 없다");
            Assert.That(File.Exists(htmlPath), "presentation/index.html 이 없다");

            string html = File.ReadAllText(htmlPath);
            foreach (string file in new[] { "kin", "curse", "rites", "balance" })
                Assert.That(html, Does.Contain("../data/" + file + ".json").IgnoreCase.Or.Contain("\"" + file + "\""),
                    "목업이 data/" + file + ".json 을 읽지 않는다");
            Assert.That(html, Does.Contain("palette.json"), "목업이 palette.json 을 읽지 않는다");

            string palette = File.ReadAllText(palettePath);
            Assert.That(palette, Does.Not.Contain("<연출 이름>"), "palette.json 에 템플릿 자리표시자가 남아 있다");
            Assert.That(palette, Does.Not.Contain("<폰트 이름"), "palette.json 에 템플릿 자리표시자가 남아 있다");
            TestContext.WriteLine("목업이 data/ 넷과 palette.json 을 읽는다");
        }
    }
}
