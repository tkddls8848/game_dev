using System.Collections.Generic;
using NUnit.Framework;
using FarmSignal.Data;
using FarmSignal.Report;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// 데이터와 기준 시뮬레이션을 한 번만 돌려 공유한다.
    /// 이 PoC는 **모든 정책을 두 세계(신호 켬/끔)에서** 돌리므로 표가 두 배다.
    /// 100년 시뮬레이션이라도 순수 C#이라 0.x초다 — 그게 이 장르에서 헤드리스의 값이다.
    public static class Support
    {
        static GameData _data;
        static readonly Dictionary<string, SimResult> _cache = new Dictionary<string, SimResult>();

        public static GameData Data => _data ?? (_data = DataLoader.Load());

        public static int SimYears => Data.Config.simYears;
        public static int EconomyYears => Data.Config.economySimYears;

        /// 정책 x 세계 x 년수 를 키로 캐시한다.
        public static SimResult Run(PolicyKind kind, bool signalOn, int years = 0, string forcedCropId = null)
        {
            if (years <= 0) years = SimYears;
            string key = kind + "|" + (signalOn ? "on" : "off") + "|" + years + "|" + (forcedCropId ?? "-");
            if (_cache.TryGetValue(key, out var r)) return r;
            r = Simulation.Run(Data, kind, years, signalOn, forcedCropId);
            _cache[key] = r;
            return r;
        }

        public static SimResult Blind => Run(PolicyKind.Blind, true);
        public static SimResult BlindOff => Run(PolicyKind.Blind, false);
        public static SimResult Reactive => Run(PolicyKind.Reactive, true);
        public static SimResult ReactiveOff => Run(PolicyKind.Reactive, false);
        public static SimResult Aware => Run(PolicyKind.SignalAware, true);
        public static SimResult AwareOff => Run(PolicyKind.SignalAware, false);
        public static SimResult Screen => Run(PolicyKind.Screen, true);
        public static SimResult ScreenOff => Run(PolicyKind.Screen, false);
        public static SimResult RenownRun => Run(PolicyKind.Renown, true);
        public static SimResult RenownRunOff => Run(PolicyKind.Renown, false);
        public static SimResult Rotate => Run(PolicyKind.Rotate, true);
        public static SimResult RotateOff => Run(PolicyKind.Rotate, false);
        public static SimResult Mono => Run(PolicyKind.MonoSeasonBest, true);
        public static SimResult MonoOff => Run(PolicyKind.MonoSeasonBest, false);

        public static PolicyKind[] MainPolicies => new[]
        {
            PolicyKind.Blind, PolicyKind.Reactive, PolicyKind.Rotate, PolicyKind.Screen,
            PolicyKind.Renown, PolicyKind.SignalAware, PolicyKind.MonoSeasonBest
        };

        /// 작물마다 그 작물만 심는 정책. NoSingleCropWins 가 쓴다.
        public static IEnumerable<SimResult> MonoCropRuns(bool signalOn)
        {
            foreach (var c in Data.Crops.crops)
                yield return Run(PolicyKind.MonoCrop, signalOn, 0, c.id);
        }

        public static SignalBoard Board(string planId, bool signalOn)
        {
            foreach (var p in Data.Showcase.plans)
                if (p.id == planId) return SignalBoard.Compute(Data, p, signalOn, SimYears);
            Assert.Fail("showcase.json 에 계획 " + planId + " 이 없다.");
            return null;
        }

        public static void Print(string s) => TestContext.Out.Write(s);
        public static void Line(string s) => TestContext.Out.WriteLine(s);
    }
}
