using System.Collections.Generic;
using NUnit.Framework;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// ★ 검사기 8 — **SignalMatters. 이 PoC의 핵.**
    ///
    /// 남의 반응을 **끈 세계**와 **켠 세계**에서 같은 정책을 돌려 결과가 유의하게 달라야 한다.
    /// 같으면 신호가 장식이라는 뜻이고, 그러면 이 PoC는 할 말이 없다.
    ///
    /// 비교가 공정하려면 두 세계가 씨드를 공유해야 한다: 날씨 달력·기준가 잡음·도둑 주사위가
    /// 같은 줄기에서 같은 순서로 나온다(SignalState 가 계절마다 위험도와 무관하게 꼭 한 번 굴리는 이유).
    /// 그래서 두 세계의 차이는 **남이 보는가** 하나뿐이다.
    [TestFixture]
    public class SignalMattersTests
    {
        static int Pct(int on, int off) => off == 0 ? 0 : (on - off) * 100 / off;

        [Test]
        public void 두_세계의_수치를_전부_출력한다()
        {
            var lim = Support.Data.Economy.limits;
            Support.Line("정책             신호끔     신호켬     차이      도둑(회/칸)  평판 방문  최저지수");
            foreach (var kind in Support.MainPolicies)
            {
                var off = Support.Run(kind, false);
                var on = Support.Run(kind, true);
                Support.Line($"{kind,-16}{off.FinalMoneyCoin,8}{on.FinalMoneyCoin,10}" +
                             $"{Pct(on.FinalMoneyCoin, off.FinalMoneyCoin),8}%" +
                             $"{on.TheftCount,10}/{on.TheftPlotsLost,-5}{on.MaxRenown,6}{on.MaxVisitors,5}" +
                             $"{on.MinPriceIndexSeen,10}");
            }
            Support.Line("");
            Support.Line($"한계값: 신호를 쓰는 정책 >= +{lim.signalMattersMinMoneyDeltaPercent}% · " +
                         $"단작 <= -{lim.signalMattersMinMonoPenaltyPercent}% · " +
                         $"차이 폭 >= {lim.signalMattersMinDeltaSpreadPoints}p · " +
                         $"끈 세계 1위는 켠 세계에서 {lim.signalMattersOffBestMinRankOn}위 밖");
            Assert.Pass();
        }

        [Test]
        public void 모든_정책이_두_세계에서_다른_결과를_낸다()
        {
            foreach (var kind in Support.MainPolicies)
            {
                var off = Support.Run(kind, false);
                var on = Support.Run(kind, true);
                Assert.That(on.Fingerprint(), Is.Not.EqualTo(off.Fingerprint()),
                    $"{kind}: 신호를 켜도 결과가 똑같다. 남의 반응이 아무것도 하지 않고 있다.");
                Assert.That(on.FinalMoneyCoin, Is.Not.EqualTo(off.FinalMoneyCoin),
                    $"{kind}: 신호를 켜도 최종 현금이 같다.");
            }
        }

        [Test]
        public void 신호를_읽는_정책은_켠_세계에서_더_번다()
        {
            int need = Support.Data.Economy.limits.signalMattersMinMoneyDeltaPercent;
            foreach (var kind in new[] { PolicyKind.SignalAware, PolicyKind.Screen,
                                         PolicyKind.Renown, PolicyKind.Rotate })
            {
                var off = Support.Run(kind, false);
                var on = Support.Run(kind, true);
                int delta = Pct(on.FinalMoneyCoin, off.FinalMoneyCoin);
                Assert.That(delta, Is.GreaterThanOrEqualTo(need),
                    $"{kind}: 신호를 쓰는 정책인데 켠 세계에서 {delta}% 밖에 못 벌었다. " +
                    "통로(윤작·가림·평판)가 실제로 값을 만들지 못하고 있다.");
            }
        }

        [Test]
        public void 신호를_못_읽는_정책은_켠_세계에서_벌을_받는다()
        {
            int need = Support.Data.Economy.limits.signalMattersMinMonoPenaltyPercent;

            var monoOff = Support.Run(PolicyKind.MonoSeasonBest, false);
            var monoOn = Support.Run(PolicyKind.MonoSeasonBest, true);
            int monoDelta = Pct(monoOn.FinalMoneyCoin, monoOff.FinalMoneyCoin);
            Assert.That(monoDelta, Is.LessThanOrEqualTo(-need),
                $"단작이 켠 세계에서 {monoDelta}% — 벌이 {need}% 에 못 미친다. " +
                "밭을 한 작물로 덮는 것이 보여도 값이 안 떨어진다는 뜻이다.");

            var blindOff = Support.Run(PolicyKind.Blind, false);
            var blindOn = Support.Run(PolicyKind.Blind, true);
            Assert.That(blindOn.FinalMoneyCoin, Is.LessThan(blindOff.FinalMoneyCoin),
                "시세판을 아예 안 보는 정책이 켠 세계에서 더 벌었다. 신호가 벌이 아니라 상금이다.");

            // 판을 읽는 것만으로는 다 갚지 못한다 — 이 PoC가 만들려는 축이 바로 이 틈이다.
            var reactiveOff = Support.Run(PolicyKind.Reactive, false);
            var reactiveOn = Support.Run(PolicyKind.Reactive, true);
            Assert.That(reactiveOn.FinalMoneyCoin, Is.LessThan(reactiveOff.FinalMoneyCoin),
                "판을 읽기만 하는 정책이 켠 세계에서 이득을 봤다. " +
                "'판을 움직이는 것이 자기'라는 것을 알아야 이득이어야 한다.");
            Assert.That(Pct(reactiveOn.FinalMoneyCoin, reactiveOff.FinalMoneyCoin),
                Is.GreaterThan(Pct(blindOn.FinalMoneyCoin, blindOff.FinalMoneyCoin)),
                "판을 읽는 것이 안 읽는 것보다 낫지 않다. 시세판이 정보가 아니라는 뜻이다.");
        }

        [Test]
        public void 정책들의_차이_폭이_유의하게_벌어진다()
        {
            int need = Support.Data.Economy.limits.signalMattersMinDeltaSpreadPoints;
            int min = int.MaxValue, max = int.MinValue;
            string minAt = null, maxAt = null;
            foreach (var kind in Support.MainPolicies)
            {
                int delta = Pct(Support.Run(kind, true).FinalMoneyCoin,
                                Support.Run(kind, false).FinalMoneyCoin);
                if (delta < min) { min = delta; minAt = kind.ToString(); }
                if (delta > max) { max = delta; maxAt = kind.ToString(); }
            }
            int spread = max - min;
            Support.Line($"차이 폭 {spread}포인트 ({minAt} {min}% ~ {maxAt} +{max}%) · 한계 {need}p");
            Assert.That(spread, Is.GreaterThanOrEqualTo(need),
                $"신호를 켜도 정책 사이의 차이가 {spread}포인트 뿐이다. " +
                "남이 본다는 사실이 '무엇을 심을까'를 바꾸지 못하고 있다.");
        }

        [Test]
        public void 순위가_뒤집힌다()
        {
            int minRank = Support.Data.Economy.limits.signalMattersOffBestMinRankOn;
            var offRank = RankByMoney(false);
            var onRank = RankByMoney(true);

            Support.Line("끈 세계 순위: " + string.Join(" > ", offRank));
            Support.Line("켠 세계 순위: " + string.Join(" > ", onRank));

            Assert.That(onRank[0], Is.Not.EqualTo(offRank[0]),
                $"두 세계의 1위가 같다({offRank[0]}). 남이 본다는 사실이 최적 전략을 바꾸지 못했다 — " +
                "그러면 이 훅은 숫자만 흔드는 장식이다.");

            int fallen = onRank.IndexOf(offRank[0]) + 1;
            Support.Line($"끈 세계 1위 {offRank[0]} 는 켠 세계에서 {fallen}위 · 한계 {minRank}위 밖");
            Assert.That(fallen, Is.GreaterThanOrEqualTo(minRank),
                $"끈 세계의 1위 {offRank[0]} 가 켠 세계에서도 {fallen}위다. 벌이 약하다.");
        }

        static List<string> RankByMoney(bool signalOn)
        {
            var rows = new List<KeyValuePair<string, int>>();
            foreach (var kind in Support.MainPolicies)
                rows.Add(new KeyValuePair<string, int>(kind.ToString(), Support.Run(kind, signalOn).FinalMoneyCoin));
            rows.Sort((a, b) =>
            {
                int c = b.Value.CompareTo(a.Value);
                return c != 0 ? c : string.CompareOrdinal(a.Key, b.Key);
            });
            var names = new List<string>();
            foreach (var r in rows) names.Add(r.Key);
            return names;
        }

        [Test]
        public void 끈_세계에서는_시세가_움직이지_않는다()
        {
            // 두 세계의 차이가 "남이 본다" 하나임을 검사기가 확인한다.
            // 끈 세계에서 시세지수가 흔들리면 비교가 오염된다.
            foreach (var kind in Support.MainPolicies)
            {
                var off = Support.Run(kind, false);
                foreach (var s in off.SignalHistory)
                    for (int c = 0; c < s.PriceAfter.Length; c++)
                        Assert.That(s.PriceAfter[c], Is.EqualTo(100),
                            $"{kind}: 신호를 끈 세계인데 {s.SeasonNumber}계절에 " +
                            $"{Support.Data.Crops.crops[c].nameKo} 시세지수가 {s.PriceAfter[c]} 다.");
                Assert.That(off.TheftCount, Is.Zero, $"{kind}: 신호를 껐는데 도둑이 왔다.");
                Assert.That(off.MaxRenown, Is.Zero, $"{kind}: 신호를 껐는데 평판이 쌓였다.");
            }
        }

        [Test]
        public void 관측치는_두_세계에서_똑같이_세어진다()
        {
            // 관측은 끈 세계에서도 그대로 센다 — 그래야 "같은 밭을 같은 만큼 보였는데
            // 결과만 달랐다"고 말할 수 있다. 관측 자체가 달라지면 그 말을 못 한다.
            var off = Support.Run(PolicyKind.MonoSeasonBest, false);
            var on = Support.Run(PolicyKind.MonoSeasonBest, true);
            Assert.That(off.SignalHistory.Count, Is.EqualTo(on.SignalHistory.Count));
            // 첫 계절은 아직 남이 반응하기 전이라 두 세계의 밭이 같다 → 관측치도 같아야 한다.
            var a = off.SignalHistory[0];
            var b = on.SignalHistory[0];
            for (int c = 0; c < a.ObservedIndex.Length; c++)
                Assert.That(b.ObservedIndex[c], Is.EqualTo(a.ObservedIndex[c]),
                    $"첫 계절의 {Support.Data.Crops.crops[c].nameKo} 관측치가 두 세계에서 다르다. " +
                    "신호를 끄는 것이 관측까지 끄고 있다 — 그러면 비교가 성립하지 않는다.");
        }
    }
}
