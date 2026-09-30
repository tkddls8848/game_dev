using System;
using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 검사기 6 — `NoDominantOrder`.
    /// **모든 임무를 같은 순서·같은 분대원으로 풀 수 없는가.**
    ///
    /// 풀 수 있으면 전술이 사라지고 외운 절차가 된다. 그러면 스토어에서 **퍼즐**로 읽히고,
    /// 퍼즐의 성공 확률은 0.34%다 (`docs/PLAN_GENRES.md §4` §2). 이 검사기가 그것을 기계로 막는다.
    ///
    /// ⚠️ 탐색은 완전하지 않다(빔 탐색). "없었다"는 **이 탐색 범위 안에서** 없었다는 뜻이다.
    /// 그래서 필수 판정은 손으로도 확인했고, 그 근거를 각 단정 옆에 적었다.
    /// </summary>
    [TestFixture]
    public sealed class NoDominantOrderTests
    {
        private const int Grid = 30000, Width = 12, Depth = 3;

        private static SearchResult SearchWithout(GameData d, string missionId, string excludedMemberId)
        {
            HashSet<string> allowed = new HashSet<string>();
            foreach (MemberDef m in d.AllMembers) if (m.id != excludedMemberId) allowed.Add(m.id);
            return PlanSearch.Beam(d, TestWorld.Sim(missionId), TestWorld.Corroborated(missionId),
                                   Grid, Width, Depth, allowed);
        }

        [Test]
        public void 모든_임무를_푸는_하나의_절차가_없다()
        {
            SortedSet<string> common = null;
            foreach (string missionId in TestWorld.MissionIds())
            {
                SortedSet<string> here = TestWorld.Search(missionId).ActionSignatures;
                if (common == null) { common = new SortedSet<string>(here, StringComparer.Ordinal); continue; }
                common.IntersectWith(here);
            }
            Assert.That(common, Is.Empty,
                "임무 전부를 푸는 절차가 있다: " + string.Join(" / ", common) + " — 외운 한 수가 된다");
            TestContext.WriteLine("임무 전부를 푸는 공통 절차 0개");
        }

        [Test]
        public void 모든_임무를_푸는_하나의_분대원_구성이_없다()
        {
            SortedSet<string> common = null;
            foreach (string missionId in TestWorld.MissionIds())
            {
                SortedSet<string> here = new SortedSet<string>(StringComparer.Ordinal);
                foreach (SquadPlan w in TestWorld.Search(missionId).Winners)
                    here.Add(string.Join("+", w.MembersUsed()));
                if (common == null) { common = here; continue; }
                common.IntersectWith(here);
                TestContext.WriteLine(missionId + " 구성: " + string.Join(" / ", here));
            }
            Assert.That(common, Is.Empty,
                "임무 전부를 같은 구성으로 푼다: " + string.Join(" / ", common));
        }

        [Test]
        public void 세_사람_각각이_어느_임무에서는_반드시_필요하다()
        {
            // 한 명을 빼고 다시 탐색해서, 그 사람이 없으면 못 푸는 임무가 있는지 본다.
            //
            // 손으로 확인한 근거:
            //   · 정찰병 — Infiltrate 를 가진 유일한 사람. 「징발 장부」는 문서를 빼내야 끝난다
            //   · 저격수 — 함정이 하나뿐이라(maxTraps 1) 순찰 둘을 함정으로만 지울 수 없다
            //   · 공병  — 폭약을 가진 유일한 사람. 「곳간의 징발 곡물」은 폭파가 승리 조건이다
            GameData d = TestWorld.Data;
            Dictionary<string, string> needed = new Dictionary<string, string>();

            foreach (MemberDef m in d.AllMembers)
                foreach (string missionId in TestWorld.MissionIds())
                {
                    SearchResult sr = SearchWithout(d, missionId, m.id);
                    if (sr.Winners.Count == 0 && !needed.ContainsKey(m.id)) needed[m.id] = missionId;
                }

            foreach (MemberDef m in d.AllMembers)
                Assert.That(needed.ContainsKey(m.id),
                    m.id + " 가 없어도 임무 전부가 풀린다 — 이 분대원은 장식이다");

            foreach (KeyValuePair<string, string> kv in needed)
                TestContext.WriteLine(kv.Key + " 가 없으면 못 푸는 임무: " + kv.Value);
        }

        [Test]
        public void 두_사람_조합으로_임무_전부를_풀_수_없다()
        {
            GameData d = TestWorld.Data;
            foreach (MemberDef excluded in d.AllMembers)
            {
                int solved = 0;
                List<string> unsolved = new List<string>();
                foreach (string missionId in TestWorld.MissionIds())
                {
                    if (SearchWithout(d, missionId, excluded.id).Winners.Count > 0) solved++;
                    else unsolved.Add(missionId);
                }
                Assert.That(solved, Is.LessThan(TestWorld.MissionIds().Count),
                    excluded.id + " 를 뺀 두 사람이 임무 전부를 풀었다 — 3인이 필요 없다");
                TestContext.WriteLine(excluded.id + " 를 뺀 두 사람: " + solved + "/"
                    + TestWorld.MissionIds().Count + " 해결 · 못 푼 임무 " + string.Join(" ", unsolved));
            }
        }

        [Test]
        public void 임무마다_해가_하나가_아니다()
        {
            GameData d = TestWorld.Data;
            foreach (string missionId in TestWorld.MissionIds())
            {
                SearchResult sr = TestWorld.Search(missionId);
                Assert.That(sr.Winners.Count, Is.GreaterThanOrEqualTo(2),
                    missionId + " 의 해가 하나뿐이다 — 외운 한 수가 된다");
                TestContext.WriteLine(missionId + ": 승리안 " + sr.Winners.Count + " · 절차 "
                    + sr.ActionSignatures.Count + " · 자리 " + sr.PlacementSignatures.Count);
                foreach (string sig in sr.ActionSignatures) TestContext.WriteLine("    " + sig);
            }
        }

        [Test]
        public void 눈먼_해도_임무마다_모양이_다르다()
        {
            // 눈먼 계획까지 같은 절차면, 정보를 안 사는 플레이어에게는 이 게임이 한 수짜리다.
            SortedSet<string> common = null;
            foreach (string missionId in TestWorld.MissionIds())
            {
                SortedSet<string> here = new SortedSet<string>(StringComparer.Ordinal);
                foreach (SquadPlan p in TestWorld.BlindWinners(missionId)) here.Add(p.ActionSignature());
                if (common == null) { common = here; continue; }
                common.IntersectWith(here);
                TestContext.WriteLine(missionId + " 눈먼 절차: " + string.Join(" / ", here));
            }
            Assert.That(common, Is.Empty, "눈먼 계획의 절차가 임무 전부에서 같다: " + string.Join(" / ", common));
        }
    }
}
