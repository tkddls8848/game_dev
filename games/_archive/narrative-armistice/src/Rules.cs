using ConceptPoCs;
using static ConceptPoCs.V;
namespace Armistice;
public class Rules : IGame
{
    public State Start(Scenario d)=>new();
    public View Describe(State s, Scenario d)
    {
        if(s.Step==3) return End(s.A==0 ? "같은 문서, 같은 약속" : s.A==1 ? "해석이 남은 휴전" : "서명 뒤의 총성",
            s.A==0 ? "양국의 철수 시점과 책임이 일치한다. 회담은 거칠었지만, 국경의 병사들은 같은 명령을 받았다." : s.A==1 ? "서명은 끝났다. 그러나 한 조항의 해석이 달라 감시단의 중재가 필요하다." : "북국은 철수가 확정됐다고 믿고 진입했다. 남국은 검토를 약속했을 뿐이라며 발포했다.",
            "통역 기록의 차이가 현장의 명령이 되었다.", S("기록 불일치", s.A), S("회담 긴장", s.B));
        var item=d.Items[s.Step];
        return new("제 "+(s.Step+1)+" 의제 · "+item.Title, item.Text,
            s.Step==0 ? "북국: 즉시 철수 확약을 요구. 남국: 내부 승인 전 확약 불가." : s.C==0 ? "양측 서기가 같은 강도로 기록했습니다. 협상은 느려졌지만 약속은 일치합니다." : "북국 기록은 확약, 남국 원문은 조건부 약속입니다. 방 안의 긴장은 잠시 낮아졌습니다.",
            new[]{S("회담",(s.Step+1)+" / 3"),S("불일치 조항",s.A),S("긴장",s.B)},
            new[]{C("literal",item.Detail,"의미 보존 · 긴장 +1",s with{Step=s.Step+1,B=s.B+1,C=0}),
                  C("soften",d.Items[s.Step+3].Title,"확약으로 변경 · 불일치 +1",s with{Step=s.Step+1,A=s.A+1,C=1})});
    }
}
