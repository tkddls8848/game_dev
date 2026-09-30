using System.Collections.Generic;
using ConceptPoCs;
using static ConceptPoCs.V;
namespace TomorrowMap;
public class Rules : IGame
{
    public State Start(Scenario d)=>new();
    public static bool RouteOpen(State s)=>s.A==1 && s.B==1;
    public static bool TenantSafe(State s)=>s.C==1;
    public View Describe(State s, Scenario d)
    {
        if(s.Step==1) return End(RouteOpen(s)?(TenantSafe(s)?"모두의 주소가 남은 아침":"길은 열렸고 문은 사라졌다"):"구급차가 돌아선 아침",
            RouteOpen(s)?(TenantSafe(s)?"골목의 장애물을 옮기고 새 출입구를 냈다. 구급차가 병원에 도착하고, 지하 세입자는 자신의 문으로 나왔다.":"병원 길은 이어졌지만 확장된 도로가 지하집의 유일한 출입구를 지웠다. 지도에 없는 주민이 갇혔다."):"도로와 병원 담장의 입구가 연결되지 않았다. 도시가 지도를 정확하게 따랐지만, 환자는 병원에 도착하지 못했다.",
            "시는 당신이 승인한 도면대로 바뀌었습니다.",S("병원 통행",RouteOpen(s)?"연결":"단절"),S("주민 출입",TenantSafe(s)?"보존":"위험"));
        var choices=new List<Choice>{
            C("road","골목 폭 수정",s.A==0?"좁은 골목을 구급차 도로로 확장":"도로를 원래 폭으로 복구",s with{A=1-s.A}),
            C("gate","병원 담장 수정",s.B==0?"도로 끝에 병원 출입구 신설":"병원 출입구를 닫음",s with{B=1-s.B})};
        if((s.Flags&1)==0) choices.Add(C("petition","누락된 민원 읽기","지하 세입자의 출입 위치 확인",s with{Flags=1}));
        if(s.Flags==1) choices.Add(C("door","지하집 출입구 이전",s.C==0?"확장 도로 바깥으로 출입구 이동":"출입구를 원위치로 복구",s with{C=1-s.C}));
        choices.Add(C("approve","도면 승인 · 다음 날",RouteOpen(s)?"병원 경로 연결 확인. 주민 출입도 확인하세요.":"미리보기: 병원까지 통행할 수 없습니다",s with{Step=1}));
        return new("측량 구역 03 · 병원 골목","구급차 길을 연결하세요. 도로를 넓히면 기존 지하 출입구가 사라집니다. 누락된 민원을 확인하고 출입구를 옮길 수 있습니다.",s.Flags==0?"시청 도면: 지하층 미기재. 미처리 민원 1건.":"민원: 저는 3번 건물 지하에 삽니다. 도로를 넓히면 제 계단이 없어집니다.",
            new[]{S("병원 경로",RouteOpen(s)?"연결됨":"끊김"),S("민원",s.Flags==0?"미확인":"확인"),S("지하 출입",s.C==1?"이전 완료":"기존 위치")},choices.ToArray());
    }
}
