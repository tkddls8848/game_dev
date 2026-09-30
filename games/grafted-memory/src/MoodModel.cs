using Graft.Data;

namespace Graft.Sim
{
    /// <summary>
    /// 정서 거리. data/net.json 의 평평한 행렬을 그대로 읽는다.
    ///
    /// 거리를 코드에 적지 않는다 — 값이 데이터에 있어야 검사기가 실패했을 때
    /// 기준값을 내리는 대신 **데이터를 고쳐 다시 돌릴** 수 있다.
    /// 행렬은 대칭이고 대각이 0이어야 한다. DataValidator 가 그것을 지킨다.
    /// </summary>
    public sealed class MoodModel
    {
        private readonly GameData _data;
        private readonly int _n;

        public MoodModel(GameData data)
        {
            _data = data;
            _n = data.AllMoods.Count;
        }

        public int Count { get { return _n; } }

        public int Distance(string moodA, string moodB)
        {
            int a = _data.MoodIndex(moodA);
            int b = _data.MoodIndex(moodB);
            return _data.Net.moodDistance[a * _n + b];
        }

        public int DistanceAt(int a, int b) { return _data.Net.moodDistance[a * _n + b]; }
    }
}
