using System;
using System.IO;
using Newtonsoft.Json;

namespace FarmRewindYear.Data
{
    /// data/ 의 다섯 JSON을 읽는다. 테스트 산출물이 games/<슬러그>/TestBuild/... 아래에
    /// 떨어지므로 위로 올라가며 data/crops.json 을 가진 조상을 찾는다.
    public static class DataLoader
    {
        public static string FindPocRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "data", "crops.json"))) return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "data/crops.json 을 가진 PoC 뿌리를 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        static T Read<T>(string root, string file)
        {
            var path = Path.Combine(root, "data", file);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        public static GameData Load()
        {
            var root = FindPocRoot();
            var d = new GameData
            {
                Config  = Read<ConfigData>(root, "config.json"),
                Seasons = Read<SeasonsData>(root, "seasons.json"),
                Crops   = Read<CropsData>(root, "crops.json"),
                Plots   = Read<PlotsData>(root, "plots.json"),
                Economy = Read<EconomyData>(root, "economy.json"),
                Events  = Read<EventsData>(root, "events.json"),
            };
            d.Index();
            return d;
        }
    }
}
