using System.Collections.Generic;
using NUnit.Framework;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// 검사기 9 — NoSingleCropWins. 한 작물만 계속 심는 것이 최적이 아닌가.
    /// 신호가 있으면 단작이 **벌을 받아야** 한다.
    ///
    /// 두 갈래로 본다:
    ///   1. 작물별 단작 여덟 — 문자 그대로 "한 작물만". 심을 수 없는 계절에는 밭을 비운다
    ///   2. 계절별 단작(MonoSeasonBest) — 계절마다 기준마진 1위 하나로 밭을 덮는다.
    ///      이쪽이 훨씬 강한 상대이고, **신호를 끄면 실제로 이긴다.** 그 대비가 이 검사기의 값이다
    [TestFixture]
    public class NoSingleCropWinsTests
    {
        [Test]
        public void 작물별_단작은_어느_섞는_정책도_못_이긴다()
        {
            int margin = Support.Data.Economy.limits.noSingleCropWinsMarginPercent;
            int mixedBest = 0;
            string mixedName = null;
            foreach (var kind in new[] { PolicyKind.SignalAware, PolicyKind.Rotate,
                                         PolicyKind.Screen, PolicyKind.Renown, PolicyKind.Reactive })
            {
                var r = Support.Run(kind, true);
                if (r.FinalMoneyCoin > mixedBest) { mixedBest = r.FinalMoneyCoin; mixedName = kind.ToString(); }
            }
            Support.Line($"섞는 정책 최고 {mixedName} {mixedBest}");
            Support.Line("작물    단작(켬)   단작(끔)   파산");
            foreach (var c in Support.Data.Crops.crops)
            {
                var on = Support.Run(PolicyKind.MonoCrop, true, 0, c.id);
                var off = Support.Run(PolicyKind.MonoCrop, false, 0, c.id);
                Support.Line($"{c.nameKo,-8}{on.FinalMoneyCoin,9}{off.FinalMoneyCoin,10}   " +
                             $"{(on.BankruptYear > 0 ? on.BankruptYear + "년" : "-")}");
                Assert.That(on.FinalMoneyCoin * 100, Is.LessThan(mixedBest * (100 - margin)),
                    $"{c.nameKo} 단작이 섞는 정책({mixedName})의 {100 - margin}% 를 넘겼다 — " +
                    "한 작물만 심는 것이 최적이 되면 이 훅은 결정을 만들지 못한다.");
            }
        }

        [Test]
        public void 계절별_단작은_신호를_끄면_이기고_켜면_진다()
        {
            // **이 검사기의 핵.** 단작이 늘 나쁜 것이 아니다 — 남이 보지 않는 세계에서는 옳다.
            // 벌을 만드는 것은 규칙(남이 본다)이고, 그것을 이 한 쌍이 증명한다.
            var monoOff = Support.Run(PolicyKind.MonoSeasonBest, false);
            var monoOn = Support.Run(PolicyKind.MonoSeasonBest, true);

            int rotateOff = Support.Run(PolicyKind.Rotate, false).FinalMoneyCoin;
            int rotateOn = Support.Run(PolicyKind.Rotate, true).FinalMoneyCoin;

            Support.Line($"신호 끔: 단작 {monoOff.FinalMoneyCoin} vs 윤작 {rotateOff}");
            Support.Line($"신호 켬: 단작 {monoOn.FinalMoneyCoin} vs 윤작 {rotateOn}");

            Assert.That(monoOff.FinalMoneyCoin, Is.GreaterThan(rotateOff),
                "남이 보지 않는 세계에서도 단작이 윤작을 못 이긴다. " +
                "그러면 윤작이 이기는 이유가 신호가 아니라 다른 것(물·토질)이라는 뜻이고, " +
                "이 검사기가 무엇을 봤는지 말할 수 없다.");
            Assert.That(monoOn.FinalMoneyCoin, Is.LessThan(rotateOn),
                "남이 보는 세계에서 단작이 아직 윤작을 이긴다 — 벌이 모자라다.");
        }

        [Test]
        public void 단작은_시세를_스스로_무너뜨린다()
        {
            var d = Support.Data;
            var mono = Support.Run(PolicyKind.MonoSeasonBest, true);
            var rotate = Support.Run(PolicyKind.Rotate, true);
            Support.Line($"단작 최저 시세지수 {mono.MinPriceIndexSeen} · 윤작 {rotate.MinPriceIndexSeen}");
            Assert.That(mono.MinPriceIndexSeen, Is.LessThanOrEqualTo(d.Signal.copycat.collapsePercent),
                $"단작인데 시세지수가 붕괴선({d.Signal.copycat.collapsePercent}) 아래로 안 내려갔다.");
            Assert.That(rotate.MinPriceIndexSeen, Is.GreaterThan(mono.MinPriceIndexSeen),
                "윤작이 단작보다 시세를 더 무너뜨렸다 — 관측치를 흩는 것이 듣지 않는다는 뜻이다.");
        }

        [Test]
        public void 한_작물의_관측치가_몰리면_그_작물만_값이_떨어진다()
        {
            // 벌이 밭 전체에 번지면 "무엇을 심을까"가 아니라 "얼마나 심을까"만 남는다.
            // 값이 떨어지는 것은 **본 작물**이어야 한다.
            var d = Support.Data;
            var mono = Support.Run(PolicyKind.MonoSeasonBest, true);
            var last = mono.SignalHistory[mono.SignalHistory.Count - 1];

            int hit = 0, held = 0;
            for (int c = 0; c < d.CropCount; c++)
            {
                bool observed = false;
                foreach (var h in mono.SignalHistory) if (h.ObservedIndex[c] > 50) { observed = true; break; }
                if (observed) { if (last.PriceAfter[c] < 100) hit++; }
                else { if (last.PriceAfter[c] >= 100) held++; }
                Support.Line($"{d.Crops.crops[c].nameKo,-8} 마지막 지수 {last.PriceAfter[c],4} · " +
                             $"{(observed ? "많이 보였다" : "거의 안 보였다")}");
            }
            Assert.That(hit, Is.GreaterThan(0), "많이 보인 작물인데 값이 안 떨어진 것이 하나도 없다.");
            Assert.That(held, Is.GreaterThan(0),
                "거의 안 보인 작물도 값이 떨어졌다 — 벌이 작물별이 아니라 밭 전체에 걸리고 있다.");
        }

        [Test]
        public void 묶어_둔_작물은_값이_오른다()
        {
            // 윤작이 이기는 진짜 이유. 안 심은 작물이 귀해지지 않으면 윤작은 손해를 나누는 것뿐이다.
            var d = Support.Data;
            var r = Support.Run(PolicyKind.Screen, true);
            var risen = new List<string>();
            var last = r.SignalHistory[r.SignalHistory.Count - 1];
            for (int c = 0; c < d.CropCount; c++)
                if (last.PriceAfter[c] > 100) risen.Add($"{d.Crops.crops[c].nameKo} {last.PriceAfter[c]}");
            Support.Line("3년 뒤 기준가를 넘긴 작물: " + (risen.Count == 0 ? "없음" : string.Join(" · ", risen)));
            Assert.That(risen.Count, Is.GreaterThan(0),
                "한 작물도 기준가를 넘기지 못했다 — 묶어 두는 것에 보상이 없으면 윤작이 이길 이유가 없다.");
            Assert.That(r.MaxPriceIndexSeen, Is.EqualTo(d.Signal.copycat.ceilPercent),
                $"천장({d.Signal.copycat.ceilPercent}%)에 닿은 작물이 없다 — 천장이 죽은 값이다.");
        }
    }
}
