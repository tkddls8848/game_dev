using NUnit.Framework;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    /// <summary>
    /// 침묵의 게이트가 실제로 어떻게 움직이는지 한 칸씩 확인한다.
    /// 핵 검사기가 재는 수치의 바닥에 이 규칙이 있으므로 따로 못 박아 둔다.
    /// </summary>
    [TestFixture]
    public class KnowledgeGateTests
    {
        private const string Bid = "b_ga";
        private const string Quiet = "h_ga_quiet";
        private const string Child = "h_ga_child";

        private static Night Tonight(int seed) { return TestWorld.Night(Bid, seed); }

        [Test]
        public void 침묵한_문에_귀를_대면_침묵이_확인된다()
        {
            Night n = Tonight(0);
            Knowledge k = new Knowledge(n);
            int q = n.IndexOf(Quiet);
            Assert.That(k.Of(q).SilenceConfirmed, Is.False);
            Assert.That(k.Of(q).Level, Is.EqualTo(0));
            k.Listen(q, 0);
            Assert.That(k.Of(q).SilenceConfirmed, Is.True);
            Assert.That(k.Of(q).Level, Is.EqualTo(1), "침묵을 확인한 것만으로는 「뭔가 있다」까지다");
            Assert.That(k.Of(q).NeedId, Is.Null);
        }

        [Test]
        public void 이웃의_말만_들으면_소문에_그친다()
        {
            // 옆집이 「약을 못 받았다」고 말해도, 그 문 앞에 서 보지 않으면 확신이 서지 않는다.
            for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
            {
                Night n = Tonight(seed);
                int c = n.IndexOf(Child), q = n.IndexOf(Quiet);
                for (int slot = 0; slot < n.SlotCount; slot++)
                {
                    Knowledge k = new Knowledge(n);
                    k.Listen(c, slot);
                    Belief b = k.Of(q);
                    Assert.That(b.Level, Is.LessThanOrEqualTo(1),
                        "씨드" + seed + " 시간대" + slot + ": 문 앞에 서 보지 않았는데 확신이 섰다");
                }
            }
        }

        [Test]
        public void 확인한_뒤에_들은_말은_확신이_되고_순서를_따지지_않는다()
        {
            // 소문을 먼저 듣고 나중에 문 앞에 서도 같은 결론이어야 한다 —
            // 사람은 그렇게 확신이 서고, 순서를 따지면 계획 탐색이 배로 커진다.
            int confirmed = 0;
            for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
            {
                Night n = Tonight(seed);
                int c = n.IndexOf(Child), q = n.IndexOf(Quiet);
                for (int slot = 0; slot < n.SlotCount; slot++)
                {
                    Knowledge a = new Knowledge(n);
                    a.Listen(q, (slot + 1) % n.SlotCount);
                    a.Listen(c, slot);
                    Knowledge b = new Knowledge(n);
                    b.Listen(c, slot);
                    b.Listen(q, (slot + 1) % n.SlotCount);
                    Assert.That(b.Of(q).Level, Is.EqualTo(a.Of(q).Level),
                        "씨드" + seed + " 시간대" + slot + ": 듣는 순서가 결론을 바꿨다");
                    Assert.That(b.Of(q).NeedId, Is.EqualTo(a.Of(q).NeedId));
                    if (a.Of(q).Level >= 2 && a.Of(q).NeedId == "need_medicine") confirmed++;
                }
            }
            Assert.That(confirmed, Is.GreaterThan(0),
                "침묵을 확인하고 옆집 말을 들어도 확신에 이르는 경우가 한 번도 없다");
        }

        [Test]
        public void 되돌리기가_정확하다()
        {
            // 전수 탐색이 같은 상태를 수만 번 지나가므로 Listen/Unlisten 이 정확히 역이어야 한다.
            Night n = Tonight(3);
            Knowledge k = new Knowledge(n);
            string[] before = new string[n.Households.Count];
            for (int i = 0; i < n.Households.Count; i++) before[i] = Dump(k, i);
            for (int d = 0; d < n.Households.Count; d++)
                for (int s = 0; s < n.SlotCount; s++)
                {
                    k.Listen(d, s);
                    k.Unlisten(d, s);
                }
            Assert.That(k.Listens, Is.EqualTo(0));
            Assert.That(k.SilentDoorListens, Is.EqualTo(0));
            for (int i = 0; i < n.Households.Count; i++)
                Assert.That(Dump(k, i), Is.EqualTo(before[i]),
                    n.Households[i].id + " 의 상태가 되돌아오지 않았다");
        }

        private static string Dump(Knowledge k, int i)
        {
            Belief b = k.Of(i);
            return b.Level + "/" + (b.NeedId ?? "-") + "/" + b.Misled + "/" + b.SilenceConfirmed;
        }
    }
}
