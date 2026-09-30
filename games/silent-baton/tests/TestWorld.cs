using System.Collections.Generic;
using SilentBaton.Data;
using SilentBaton.Sim;

namespace SilentBaton.Tests
{
    /// <summary>테스트가 공유하는 세계. data/ 를 한 번만 읽는다.</summary>
    public static class TestWorld
    {
        private static GameData _data;

        public static GameData Data
        {
            get
            {
                if (_data == null) _data = GameData.Load();
                return _data;
            }
        }

        public static int[] Seeds { get { return Data.Balance.checkers.seeds; } }
        public static int SeasonSeed { get { return Data.Score.seasonSeed; } }
        public static List<string> PieceIds() { return Data.PieceIds(); }

        /// <summary>한 곡 한 씨드의 연주 한 번. 지휘자만 갈아 끼우면 두 세계 비교가 된다.</summary>
        public static PerformanceResult Play(string pieceId, int seed, IConductor conductor,
                                             int seasonSeed = 0, int[] trait = null)
        {
            GameData d = Data;
            PieceDef piece = d.Piece(pieceId);
            Performance perf = new Performance(d, piece, seed);
            Players players = perf.Cast(seasonSeed == 0 ? SeasonSeed : seasonSeed, trait);
            return perf.Run(conductor, players);
        }

        public static PerformanceResult Visible(string pieceId, int seed, PressKnowledge press = null,
                                                VisibleChannel masked = VisibleChannel.None,
                                                int seasonSeed = 0, int[] trait = null)
        {
            return Play(pieceId, seed, new VisibleOnlyConductor(press, masked), seasonSeed, trait);
        }

        public static PerformanceResult Hearing(string pieceId, int seed, int seasonSeed = 0, int[] trait = null)
        {
            return Play(pieceId, seed, new HearingConductor(), seasonSeed, trait);
        }

        public static PerformanceResult Immediate(string pieceId, int seed, int seasonSeed = 0, int[] trait = null)
        {
            return Play(pieceId, seed, new ImmediateFeedbackConductor(), seasonSeed, trait);
        }
    }
}
