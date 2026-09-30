using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 검사기 일곱의 **전제**. 여기가 깨지면 아래 검사기들이 무엇을 재는지 알 수 없다.
    /// 참조 무결성 · 정수 규율 · 설계 원칙(UnityEngine 금지 · 부동소수 금지 · 씨드 고정)을 본다.
    /// </summary>
    [TestFixture]
    public sealed class DataIntegrityTests
    {
        [Test]
        public void 모든_참조가_풀린다()
        {
            GameData d = TestWorld.Data;

            foreach (MapDef map in d.Maps.maps)
            {
                ZoneGraph g = new ZoneGraph(map);   // 없는 구역을 참조하면 여기서 던진다
                Assert.That(g.HasZone(map.entryZone), "진입 구역이 없다: " + map.entryZone);
                Assert.That(g.HasZone(map.extractionZone), "탈출 구역이 없다: " + map.extractionZone);
                Assert.That(map.reconVantages.Length, Is.GreaterThan(0), map.id + " 에 정찰 벤티지가 없다");
                foreach (string v in map.reconVantages)
                    Assert.That(g.HasZone(v), "정찰 벤티지가 지도에 없다: " + v);
            }

            foreach (GuardDef guard in d.Patrols.guards)
            {
                ZoneGraph g = new ZoneGraph(d.Map(guard.mapId));
                Assert.That(guard.legs.Length, Is.GreaterThan(0), guard.id);
                foreach (PatrolLegDef leg in guard.legs)
                {
                    Assert.That(g.HasZone(leg.zone), guard.id + " 의 순찰 구역이 지도에 없다: " + leg.zone);
                    Assert.That(leg.dwellMs, Is.GreaterThan(0), guard.id + " 의 체류 시간이 0이다");
                }
                Assert.That(guard.phaseOptionsMs.Length, Is.GreaterThan(0), guard.id + " 에 위상이 없다");
            }

            foreach (ShiftChangeDef sc in d.Patrols.shiftChanges)
                Assert.DoesNotThrow(delegate { d.Guard(sc.guardId); }, "교대의 순찰병 참조가 깨졌다: " + sc.id);

            foreach (MissionDef m in d.AllMissions)
            {
                Assert.DoesNotThrow(delegate { d.Map(m.mapId); }, m.id);
                foreach (string id in m.guardIds) Assert.DoesNotThrow(delegate { d.Guard(id); }, m.id);
                foreach (string id in m.shiftChangeIds) Assert.DoesNotThrow(delegate { d.ShiftChange(id); }, m.id);
                foreach (string id in m.eliminateGuardIds) Assert.That(Array.IndexOf(m.guardIds, id), Is.GreaterThanOrEqualTo(0),
                    m.id + " 가 회차에 없는 순찰병을 지우라고 한다: " + id);
                foreach (string id in m.destroyTargetIds)
                    Assert.That(d.Target(id).mapId, Is.EqualTo(m.mapId), m.id + " 의 목표가 다른 지도에 있다");
                foreach (string id in m.seizeIntelIds)
                    Assert.That(d.Intel(id).mapId, Is.EqualTo(m.mapId), m.id + " 의 문서가 다른 지도에 있다");

                int objectives = m.eliminateGuardIds.Length + m.destroyTargetIds.Length + m.seizeIntelIds.Length;
                Assert.That(objectives, Is.GreaterThan(0), m.id + " 에 승리 조건이 없다");
            }

            foreach (SupplyDef s in d.Camp.supplies)
                Assert.That(s.ceiling, Is.GreaterThan(s.floor), s.id);
            foreach (ActionCostDef ac in d.Camp.actionCosts)
                Assert.DoesNotThrow(delegate { d.Supply(ac.supplyId); }, ac.actionKind);
            foreach (MissionRewardDef mr in d.Camp.missionRewards)
            {
                Assert.DoesNotThrow(delegate { d.Mission(mr.missionId); }, mr.missionId);
                Assert.DoesNotThrow(delegate { d.Supply(mr.supplyId); }, mr.supplyId);
            }
            foreach (RepairDef rp in d.Camp.repairs)
            {
                Assert.DoesNotThrow(delegate { d.Supply(rp.supplyId); }, rp.id);
                Assert.DoesNotThrow(delegate { d.Supply(rp.bonusSupplyId); }, rp.id);
            }
            Assert.DoesNotThrow(delegate { d.Supply(d.Camp.recovery.medicineSupplyId); });
            Assert.DoesNotThrow(delegate { d.Supply(d.Camp.recovery.trustSupplyId); });
        }

        [Test]
        public void 시각은_전부_틱의_배수다()
        {
            GameData d = TestWorld.Data;
            int tick = d.Balance.tickMs;
            Assert.That(tick, Is.GreaterThan(0));

            foreach (GuardDef g in d.Patrols.guards)
            {
                foreach (PatrolLegDef leg in g.legs)
                    Assert.That(leg.dwellMs % tick, Is.Zero, g.id + " 체류 " + leg.dwellMs);
                foreach (int phase in g.phaseOptionsMs)
                    Assert.That(phase % tick, Is.Zero, g.id + " 위상 " + phase);
            }
            foreach (ShiftChangeDef sc in d.Patrols.shiftChanges)
            {
                Assert.That(sc.atMs % tick, Is.Zero, sc.id);
                Assert.That(sc.newPhaseMs % tick, Is.Zero, sc.id);
            }
            foreach (MemberDef m in d.AllMembers)
            {
                Assert.That(m.moveMsPerZone % tick, Is.Zero, m.id);
                foreach (MemberActionDef a in m.actions)
                    Assert.That(a.durationMs % tick, Is.Zero, m.id + ":" + a.kind);
            }
            foreach (MissionDef m in d.AllMissions)
            {
                Assert.That(m.lengthMs % tick, Is.Zero, m.id);
                Assert.That(m.extractDeadlineMs % tick, Is.Zero, m.id);
                Assert.That(m.extractDeadlineMs, Is.LessThanOrEqualTo(m.lengthMs), m.id);
            }
        }

        [Test]
        public void 정찰_벤티지는_정찰병이_숨을_수_있는_자리다()
        {
            // 정찰 단계가 성립하려면 며칠 앉아 있을 수 있어야 한다. 노출되는 자리는 벤티지가 아니다.
            GameData d = TestWorld.Data;
            MemberDef scout = d.Member("scout");
            int floor = d.Balance.vision.concealFloorPercent;

            foreach (MapDef map in d.Maps.maps)
            {
                ZoneGraph g = new ZoneGraph(map);
                foreach (string v in map.reconVantages)
                {
                    int conceal = scout.stealthPercent + g.CoverPercent(v);
                    Assert.That(conceal, Is.GreaterThanOrEqualTo(floor),
                        map.id + " 의 벤티지 " + v + " 는 정찰병이 숨을 수 없다 (은폐 " + conceal + " < " + floor + ")");
                }
            }
        }

        [Test]
        public void 분대원의_수단이_겹치지_않는다()
        {
            // PLAN_TACTICS §4: 겹치면 한 명만 쓰는 최적해가 생긴다.
            Dictionary<string, string> owner = new Dictionary<string, string>();
            foreach (MemberDef m in TestWorld.Data.AllMembers)
            {
                Assert.That(m.actions, Is.Not.Null.And.Length.GreaterThan(0), m.id + " 에게 수단이 없다");
                foreach (MemberActionDef a in m.actions)
                {
                    Assert.That(owner.ContainsKey(a.kind), Is.False,
                        "수단 " + a.kind + " 를 " + owner.GetValueOrDefault(a.kind, "?") + " 와 " + m.id + " 가 나눠 갖는다");
                    owner[a.kind] = m.id;
                }
            }
            Assert.That(TestWorld.Data.AllMembers.Count, Is.EqualTo(3), "분대는 3인이다");
        }

        [Test]
        public void src_에_UnityEngine_과_부동소수가_없다()
        {
            // 뿌리 CLAUDE.md 설계 원칙 1·4·5. 문서상의 약속이 아니라 게이트여야 한다.
            string src = FindDir("src");
            string[] files = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories);
            Assert.That(files.Length, Is.GreaterThan(0), "src/ 에 파일이 없다: " + src);

            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                string name = Path.GetFileName(file);

                // 실제 using 지시문만 본다 — 주석의 "using UnityEngine 금지"에 걸리면 안 된다.
                Assert.That(Regex.IsMatch(text, @"(?m)^\s*using\s+UnityEngine"), Is.False,
                    name + " 가 UnityEngine 을 끌어들였다");
                Assert.That(Regex.IsMatch(text, @"(?m)^\s*using\s+UnityEditor"), Is.False,
                    name + " 가 UnityEditor 를 끌어들였다");

                // 부동소수: 선언·리터럴 모두 막는다. "9.999 피해"와 재현 불가가 여기서 시작한다.
                foreach (string bad in new[] { "float ", "double ", "decimal " })
                    Assert.That(text.Contains(bad), Is.False, name + " 에 " + bad.Trim() + " 가 있다");
                Assert.That(Regex.IsMatch(text, @"\b\d+\.\d+[fFdDmM]?\b"), Is.False,
                    name + " 에 소수 리터럴이 있다");

                // 난수는 반드시 씨드를 받는다. new Random() 은 재현을 깬다.
                Assert.That(Regex.IsMatch(text, @"new\s+Random\s*\(\s*\)"), Is.False,
                    name + " 에 씨드 없는 Random 이 있다");
            }
        }

        [Test]
        public void 회차_전개에는_난수가_없다()
        {
            // PLAN_TACTICS §8: 전술이 운으로 결판나면 정찰이 의미를 잃는다.
            // 난수를 쓰는 파일은 둘뿐이어야 한다 — 순찰 위상 선택(PatrolModel)과 거점 정산(CampLedger).
            string src = FindDir("src");
            List<string> usesRandom = new List<string>();
            foreach (string file in Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories))
                if (Regex.IsMatch(File.ReadAllText(file), @"new\s+Random\s*\(")) usesRandom.Add(Path.GetFileName(file));
            usesRandom.Sort(StringComparer.Ordinal);

            Assert.That(usesRandom, Is.EquivalentTo(new[] { "CampLedger.cs", "PatrolModel.cs", "PlanSearch.cs" }),
                "난수를 쓰는 파일이 바뀌었다: " + string.Join(", ", usesRandom)
                + " — MissionSim 에 난수가 들어가면 이 PoC의 근거가 사라진다");
        }

        internal static string FindDir(string name)
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, name);
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(name + " 를 찾지 못했다");
        }
    }
}
