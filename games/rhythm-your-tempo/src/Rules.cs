using System.Linq;
using ConceptPoCs;
using static ConceptPoCs.V;
namespace YourTempo;
public class Rules : IGame
{
    public State Start(Scenario d)=>new();
    public View Describe(State s, Scenario d)
    {
        if(s.Step==4) return End(s.A>=3?"네 사람의 한 호흡":s.A>=1?"흔들렸지만 끝난 연주":"서로를 놓친 사중주",
            s.A>=3?"신문은 네 사람이 한 사람처럼 숨 쉬었다고 썼다. 당신은 그 소리를 듣지 못했지만, 마지막 활이 멈춘 순간을 기억한다.":s.A>=1?"몇 번의 진입이 어긋났다. 그래도 단원들은 마지막까지 서로의 움직임을 기다렸다.":"첫 진입부터 시선이 어긋났다. 다음 연습에서는 음표보다 들숨과 준비 동작을 먼저 살펴야 한다.",
            "공연 평론은 결과의 해석입니다. 연주 중 피드백은 단원의 움직임으로 확인합니다.",S("맞춘 진입",s.A+" / 4"));
        var member=d.Items[s.Step];
        return new("제 "+(s.Step+1)+" 진입 · "+member.Title,"움직이는 박자가 밝은 진입 구간에 들어올 때 이 단원에게 큐를 주세요. 소리는 재생되지 않습니다. 타이밍 대신 연습 모드의 박자 이동도 사용할 수 있습니다.",
            s.Step==0?member.Text:s.B==1?"앞선 단원이 정확히 진입했습니다. 다음 단원의 호흡을 보세요.":"앞선 단원의 활이 흔들렸습니다. 다음 진입으로 호흡을 다시 맞출 수 있습니다.",
            new[]{S("정확한 큐",s.A+" / "+s.Step),S("준비 중",member.Title)},
            Enumerable.Range(0,d.Values[1]).Select(beat=>
            {
                bool hit=beat>=d.Values[2] && beat<=d.Values[3];
                return C("cue"+beat,"지금 큐 보내기",hit?"진입 구간 안 · 호흡 일치":"진입 구간 밖 · 진입 어긋남",
                    s with{Step=s.Step+1,A=s.A+(hit?1:0),B=hit?1:0});
            }).ToArray());
    }
}
