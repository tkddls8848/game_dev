namespace RedPen.Tests
{
    /// <summary>테스트가 함께 쓰는 세계. data/ 를 한 번만 읽는다.</summary>
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
    }
}
