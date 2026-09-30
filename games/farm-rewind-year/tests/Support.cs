using NUnit.Framework;
using FarmRewindYear.Data;
using FarmRewindYear.Sim;

namespace FarmRewindYear.Tests
{
    /// 데이터와 기준 시뮬레이션을 한 번만 돌려 공유한다.
    public static class Support
    {
        static GameData _data;
        static RunResult _learner;
        static RunResult _stubborn;
        static RunResult _omniscient;
        static RunResult _ladder;
        static RunResult _stubbornLadder;
        static RunResult _learnerLadder;

        public static GameData Data => _data ?? (_data = DataLoader.Load());

        /// 되감을 때마다 기억이 쌓이는 수. 의도한 플레이.
        public static RunResult Learner => _learner ?? (_learner = Simulation.Run(Data, PolicyKind.Learner));

        /// 되감아도 배우지 않는 수. 되감기를 편의로만 쓰면 어떻게 되는지.
        public static RunResult Stubborn => _stubborn ?? (_stubborn = Simulation.Run(Data, PolicyKind.Stubborn));

        /// 첫 시도부터 한 해를 다 아는 수. 달성 가능한 상한.
        public static RunResult Omniscient => _omniscient ?? (_omniscient = Simulation.Run(Data, PolicyKind.Omniscient));

        /// 완전한 지식으로 되감기를 끝까지 밀어 본 것. 흔적의 값을 재는 사다리.
        public static RunResult Ladder => _ladder ?? (_ladder = Simulation.Run(Data, PolicyKind.Omniscient, true));

        /// 되감기를 끝까지 민 판들. 성공해도 멈추지 않으므로 사다리 전체를 볼 수 있다.
        public static RunResult StubbornLadder => _stubbornLadder ?? (_stubbornLadder = Simulation.Run(Data, PolicyKind.Stubborn, true));
        public static RunResult LearnerLadder => _learnerLadder ?? (_learnerLadder = Simulation.Run(Data, PolicyKind.Learner, true));

        /// 세 정책의 사다리 전체. "되감기가 이득이 아니다"를 정책마다 확인하는 데 쓴다.
        public static RunResult[] AllLadders => new[] { LearnerLadder, StubbornLadder, Ladder };

        public static void Print(string s) => TestContext.Out.Write(s);
        public static void Line(string s) => TestContext.Out.WriteLine(s);
    }
}
