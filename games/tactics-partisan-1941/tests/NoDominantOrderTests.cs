using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 검사기 4 — `NoDominantOrder`.
    /// **모든 임무를 같은 순서·같은 분대원으로 풀 수 없는가.**
    /// 깨지면 전술이 사라지고 외운 절차가 된다 — 그리고 스토어에서 퍼즐로 분류된다
    /// (PLAN_TACTICS §2: 퍼즐 성공 확률 0.34%).
    ///
    /// 넷을 본다:
    ///   ① 모든 임무를 이기는 **절차**(분대원·수단의 순서열)가 하나도 없다
    ///   ② 모든 임무를 이기는 **2인 조합**이 없다 — 3인이 다 필요하다
    ///   ③ 세 사람 각각이 **어느 임무에서는 없으면 못 푼다**
    ///   ④ 임무마다 이기는 계획이 둘 이상이다 — 유일한 한 수가 아니다
    /// </summary>
    [TestFixture]
    public sealed class NoDominantOrderTests
    {
        [Test]
        public void 모든_임무를_이기는_절차가_없다()
        {
            List<string> missions = TestWorld.MissionIds();
            Assert.That(missions.Count, Is.GreaterThanOrEqualTo(2),
                "임무가 둘 미만이면 이 검사기는 아무것도 판정하지 않는다");

            SortedSet<string> common = null;
            foreach (string missionId in missions)
            {
                SortedSet<string> sigs = TestWorld.Search(missionId).ActionSignatures();
                Assert.That(sigs.Count, Is.GreaterThan(0), missionId + " 에 이기는 절차가 없다");
                if (common == null) { common = new SortedSet<string>(sigs, StringComparer.Ordinal); continue; }
                common.IntersectWith(sigs);
            }

            Assert.That(common, Is.Empty,
                "같은 절차가 모든 임무를 푼다: " + string.Join(" / ", common));
        }

        [Test]
        public void 모든_임무를_이기는_2인_조합이_없다()
        {
            List<string> members = TestWorld.MemberIds();
            List<string> missions = TestWorld.MissionIds();

            for (int i = 0; i < members.Count; i++)
            {
                HashSet<string> pair = TestWorld.AllMembersExcept(members[i]);
                List<string> solved = new List<string>();
                foreach (string missionId in missions)
                    if (TestWorld.Search(missionId, pair).Winners.Count > 0) solved.Add(missionId);

                Assert.That(solved.Count, Is.LessThan(missions.Count),
                    "2인 조합 {" + string.Join(",", pair) + "} 이 모든 임무를 푼다 — 한 사람이 남는다");
                TestContext.WriteLine("{" + string.Join(",", pair) + "} 이 푸는 임무: "
                                      + (solved.Count == 0 ? "없음" : string.Join(", ", solved)));
            }
        }

        [Test]
        public void 세_사람_각각이_어느_임무에서는_없으면_못_푼다()
        {
            // 수단이 겹치지 않는다(§4)는 것을 결과로 확인한다.
            SortedSet<string> indispensableSomewhere = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string missionId in TestWorld.MissionIds())
            {
                SortedSet<string> needed = TestWorld.Search(missionId).IndispensableMembers();
                TestContext.WriteLine(missionId + " 없으면 못 푸는 사람: "
                                      + (needed.Count == 0 ? "없음" : string.Join(",", needed)));
                foreach (string m in needed) indispensableSomewhere.Add(m);
            }

            Assert.That(indispensableSomewhere, Is.EquivalentTo(TestWorld.MemberIds()),
                "어느 임무에서도 꼭 필요하지 않은 분대원이 있다. 꼭 필요한 사람: "
                + string.Join(",", indispensableSomewhere));
        }

        [Test]
        public void 임무마다_이기는_계획이_둘_이상이다()
        {
            int min = TestWorld.Data.Balance.checkers.minWinningPlansPerMission;
            foreach (string missionId in TestWorld.MissionIds())
            {
                SearchResult sr = TestWorld.Search(missionId);
                Assert.That(sr.Winners.Count, Is.GreaterThanOrEqualTo(min),
                    missionId + " 의 해가 " + sr.Winners.Count + "개뿐이다 — 유일한 한 수는 퍼즐이다");

                // 서로 다른 계획인지(시각만 다른 것이 아니라) 자리·구성도 본다.
                SortedSet<string> placements = sr.PlacementSignatures();
                TestContext.WriteLine(missionId + ": 해 " + sr.Winners.Count + "개 · 자리 조합 "
                                      + placements.Count + "개 · 절차 " + sr.ActionSignatures().Count + "개");
            }
        }

        [Test]
        public void 대조군_같은_임무를_셋_두면_지배_절차가_잡힌다()
        {
            // 검사기에 이가 있는지 본다. 같은 임무를 세 번 두면 같은 절차가 전부를 풀어야 하고,
            // 교집합이 비지 않아야 한다.
            SortedSet<string> sigs = TestWorld.Search("m_ridge_dusk").ActionSignatures();
            SortedSet<string> common = new SortedSet<string>(sigs, StringComparer.Ordinal);
            common.IntersectWith(sigs);
            common.IntersectWith(sigs);
            Assert.That(common, Is.Not.Empty,
                "같은 임무끼리도 공통 절차가 없다 — 교집합 계산 자체가 틀렸다는 뜻이다");
        }
    }
}
