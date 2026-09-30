using System.Collections.Generic;
using ConceptPoCs;
using static ConceptPoCs.V;
namespace ScentLayers;
public class Rules : IGame
{
    public State Start(Scenario d) => new();
    public View Describe(State s, Scenario d)
    {
        if(s.Step == 1) return End(s.B == 1 ? "외투가 옮긴 냄새" : "향기는 사람의 이름이 아니다",
            s.B == 1 ? "온실의 흙은 관리인의 외투에 먼저 묻었다. 서기는 그 외투를 빌려 서재로 갔다. 서기를 온실 침입자로 지목한 증언은 성립하지 않는다." : "꽃향기만으로 서기를 지목했다. 그러나 온실의 흙과 향수는 서로 다른 시간에 묻었다. 이동한 것은 사람일 수도, 외투일 수도 있다.",
            "채취한 순서를 비교하면 냄새의 주인과 이동 경로를 구별할 수 있다.", S("기록한 흔적", Count(s.Flags) + " / 3"));
        var choices = new List<Choice>();
        for(int i=0;i<3;i++) if(i!=s.A) choices.Add(C("time"+i, d.Items[i].Title, "시간층 이동 · 채취 기록은 남습니다", s with { A=i }));
        if((s.Flags & (1<<s.A))==0) choices.Add(C("sample", "이 층의 냄새 채취", d.Items[s.A].Text, s with { Flags=s.Flags|(1<<s.A) }));
        if(s.Flags==7)
        {
            choices.Add(C("deduce-coat", "외투가 냄새를 옮겼다", "온실 → 외투 → 서재의 순서를 제출", s with { Step=1, B=1 }));
            choices.Add(C("deduce-clerk", "서기가 온실에 들어갔다", "꽃향기를 방문의 직접 증거로 제출", s with { Step=1, B=0 }));
        }
        return new("서재에서 발견된 외투", "세 시간층에서 흔적을 채취하세요. 냄새가 묻은 순서와 외투의 이동을 비교해 진술을 판정합니다.",
            d.Items[s.A].Detail, new[]{S("복원 시각", d.Items[s.A].Title), S("채취", Count(s.Flags)+" / 3")}, choices.ToArray());
    }
}
