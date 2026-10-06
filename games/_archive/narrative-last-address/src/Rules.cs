using System.Collections.Generic;
using ConceptPoCs;
using static ConceptPoCs.V;
namespace LastAddress;
public class Rules : IGame
{
    public State Start(Scenario d)=>new(A:d.Values[0]);
    public View Describe(State s, Scenario d)
    {
        if(s.Step==1) return End(s.B==2 && (s.Flags&4)!=0 ? "조용한 집의 다음 주소" : s.C>=2?"함께 떠나는 복도":"마지막 차에 실은 것",
            s.B==2 && (s.Flags&4)!=0?"아무 소리도 없던 303호는 짐을 쌀 상자조차 없었다. 당신의 차로 이웃은 새 거처에 도착했다.":s.C>=2?"먼저 도왔던 이웃들이 남은 가구의 짐을 나눠 옮겼다. 당신의 차에는 한 가구만 탔지만 복도에는 아무도 혼자 남지 않았다.":d.Items[s.B].Title+"의 짐을 마지막 차에 실었다. 다른 이웃에게 건넨 작은 도움도 그날의 기억으로 남았다.",
                "도움의 크기보다 무엇이 필요한지 알아차렸는지가 중요했습니다.",S("작은 도움",s.C),S("남은 시간",s.A+"시간"));
        var choices=new List<Choice>();
        for(int i=0;i<3;i++)
        {
            int bit=1<<i, helped=1<<(i+3);
            if((s.Flags&bit)==0 && s.A>1) choices.Add(C("listen"+i,d.Items[i].Title+" · 귀 기울이기","사정 확인 · 1시간",s with{A=s.A-1,Flags=s.Flags|bit,B=i}));
            if((s.Flags&bit)!=0 && (s.Flags&helped)==0 && s.A>2) choices.Add(C("help"+i,d.Items[i].Title+" · 작은 도움",d.Items[i].Detail+" · 2시간",s with{A=s.A-2,C=s.C+1,Flags=s.Flags|helped,B=i}));
            if((s.Flags&bit)!=0) choices.Add(C("move"+i,d.Items[i].Title+" · 이삿차 배정","이 가구와 함께 떠납니다 · 최종 결정",s with{Step=1,B=i}));
        }
        string feedback=s.Flags==0?"마지막 이삿차는 저녁에 떠납니다. 한 가구의 짐을 더 실을 수 있습니다.":d.Items[s.B].Text;
        return new("3층 복도 · 마지막 이삿날","문에 귀를 기울여 이웃의 사정을 알아보세요. 작은 도움은 여러 번 줄 수 있지만, 마지막 차에는 한 가구만 함께 탈 수 있습니다.",feedback,
            new[]{S("출발까지",s.A+"시간"),S("도운 이웃",s.C),S("차량 여유","한 가구")},choices.ToArray());
    }
}
