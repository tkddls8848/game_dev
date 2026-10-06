using ConceptPoCs;
using static ConceptPoCs.V;
namespace LockedPhone;
public class Rules : IGame
{
    public State Start(Scenario d)=>new(A:d.Values[0]);
    public View Describe(State s, Scenario d)
    {
        if(s.Step==4) return End(s.C==1 ? "마지막 수신인" : "지워지지 않은 부재중 전화",
            s.C==1 ? "119에 잠금 화면의 긴급 통화로 연락했다. 지하 기록실에 갇힌 사람이 구조되고, 고인이 지키려던 원본도 발견됐다." : "충분한 위치 정보를 모으지 못했거나, 기다리는 사람에게 연락하지 않았다. 화면은 다시 잠겼고 사건은 미해결로 남았다.",
            "잠금은 한 번도 풀리지 않았다. 주인이 남긴 관계가 마지막 하루를 설명했다.", S("확인한 단서",Count(s.Flags)),S("배터리",s.A+"%"));
        if(s.Step==0) return new("윤서 · 수신 전화", "휴대폰 주인은 사망했습니다. 윤서는 아직 그 사실을 모릅니다. 전화에 어떻게 반응할까요?", "19:42 · 잠금 해제 불가",new[]{S("배터리",s.A+"%")},new[]{
            C("decline","전화 거절","윤서가 문자로 용건을 남기게 합니다",s with{Step=1,A=s.A-2,Flags=1}),
            C("answer","전화 받기","잠금 화면에서 통화만 연결합니다",s with{Step=1,A=s.A-4,Flags=2})});
        if(s.Step==1) return new("새 메시지 1개",(s.Flags&1)!=0 ? "윤서: 왜 끊어? 어제 말한 구청 지하 기록실 앞이야. 철문이 잠겼어." : "윤서: 말이 없네… 살아 있는 거지? 약속한 곳에서 기다릴게.",
            "통화에 대한 반응이 뒤따르는 알림을 바꿨습니다.",new[]{S("배터리",s.A+"%")},new[]{
            C("expand","알림 길게 펼치기","숨겨진 뒷부분까지 확인 · 배터리 -2",s with{Step=2,A=s.A-2,Flags=s.Flags|4}),
            C("charge","충전기 연결","배터리 +20 · 위치 정보는 아직 불충분",s with{Step=2,A=s.A+20})});
        if(s.Step==2) return new("일정 · 어제 17:00",(s.Flags&4)!=0 ? "알림 뒷부분: 구청 지하 B2 / 환풍기가 멈췄어. 17시 기록 반출 건 때문에 온 거 맞지?" : "캘린더: 어제 17:00 · 원본 인계 / 구청. 위치 세부 정보는 알림을 펼쳐야 보입니다.",
            "사진 앱이나 메시지 앱은 열 수 없습니다. 잠금 화면의 알림만 확인합니다.",new[]{S("배터리",s.A+"%"),S("위치 확인",(s.Flags&5)!=0?"단서 확보":"미확인")},new[]{
            C("location","일정 알림 펼치기","구청 B2 기록실 위치 확인",s with{Step=3,Flags=s.Flags|8}),
            C("wait","화면 끄고 기다리기","위치 단서를 추가하지 않고 시간을 보냅니다",s with{Step=3})});
        bool located=(s.Flags&1)!=0 || (s.Flags&4)!=0 || (s.Flags&8)!=0;
        return new("윤서 · 마지막 알림","안쪽 문까지 잠겼어. 여긴 통화가 잘 안 돼. 누가 밖에서 연락해 줘야 해.","잠금 화면의 긴급 통화로 위치를 전달할 수 있습니다.",new[]{S("위치",located?"구청 B2 기록실":"구청까지만 확인"),S("배터리",s.A+"%")},new[]{
            C("rescue","긴급 통화 · 구조 요청",located?"확인한 B2 기록실 위치 전달":"위치 불충분 · 수색 지연",s with{Step=4,C=located?1:0}),
            C("leave","휴대폰을 증거로 보관","알림에 더 반응하지 않습니다",s with{Step=4,C=0})});
    }
}
