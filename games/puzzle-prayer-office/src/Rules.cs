using System.Collections.Generic;
using ConceptPoCs;
using static ConceptPoCs.V;
namespace PrayerOffice;
public class Rules : IGame
{
    public State Start(Scenario d)=>new();
    public View Describe(State s, Scenario d)
    {
        bool harvest=s.A==1 || s.B==1;
        bool concert=s.B==0;
        bool flood=s.A==1 && s.C==0;
        if(s.Step==1) return End(harvest && concert && !flood ? "세 통의 감사 편지" : flood ? "하류에서 온 청구서" : "들어주지 못한 기도",
            harvest && concert && !flood?"오전에 밭을 적시고 오후에는 공연을 열었다. 먼저 수문을 열어 하류의 집도 지켰다. 서로 다른 세 사람이 같은 날을 기적으로 기억했다.":flood?"농부의 감사 편지 뒤로 하류 주민의 편지가 도착했다. 비는 곡식을 살렸지만 닫힌 수문 아래의 집을 잠기게 했다.":"누군가의 하루는 지켜졌지만 다른 기도는 이루어지지 않았다. 배정표의 빈칸에도 사람의 이름이 있었다.",
                "보관함의 임명장: ‘이 일을 대신 맡아 줄 사람이 오게 해 주세요.’ 전임자의 기도는 승인되어 있었습니다.",S("농사",harvest?"해결":"가뭄"),S("공연",concert?"진행":"취소"),S("하류",flood?"침수":"안전"));
        var choices=new List<Choice>{
            C("morning",s.A==0?"오전 · 비 배정":"오전 · 맑음 배정","밭에는 하루 중 한 번 비가 필요합니다",s with{A=1-s.A}),
            C("afternoon",s.B==0?"오후 · 비 배정":"오후 · 맑음 배정","야외 공연은 오후에 열립니다",s with{B=1-s.B})};
        if(s.Flags==0) choices.Add(C("read","하류 주민의 편지 열기","오전 강우의 숨겨진 대가 확인",s with{Flags=1}));
        if(s.Flags==1) choices.Add(C("gate",s.C==0?"비 오기 전 수문 열기":"수문 조정 취소","사전 조정으로 하류 침수 예방",s with{C=1-s.C}));
        choices.Add(C("dispatch","기적 배정표 발송","다음 날의 감사·항의 편지 확인",s with{Step=1}));
        return new("날씨 담당 · 접수함 03","세 사람의 하루를 조정하세요. 오전과 오후의 날씨를 나누어 배정할 수 있습니다. 편지를 읽으면 대가를 줄일 방법이 드러납니다.",
            s.Flags==0?"하류에서 온 미개봉 편지가 있습니다. 같은 비를 두고도 원하는 것은 다를 수 있습니다.":"하류 주민: 오전에 비가 오면 닫힌 수문 탓에 물이 넘칩니다. 비가 오기 전에 수문을 열어 주세요.",
            new[]{S("오전",s.A==1?"비":"맑음"),S("오후",s.B==1?"비":"맑음"),S("수문",s.C==1?"열림":"닫힘")},choices.ToArray());
    }
}
