using System.Collections.Generic;
using ConceptPoCs;
using static ConceptPoCs.V;
namespace CurseInheritance;
public class Rules : IGame
{
    public State Start(Scenario d)=>new(A:d.Values[0],B:d.Values[1]);
    public View Describe(State s, Scenario d)
    {
        if(s.Step==3)
        {
            string title=s.C>=2?"다음 세대의 청구서":s.B<=0?"물이 멈춘 마을":(s.Flags&16)!=0?((s.Flags&4)==0?"처음 쓰는 공정한 장부":"늦게 고친 장부"):(s.Flags&4)!=0?"우물은 기억하지 않는다":"곡물로 갚은 계절";
            string result=s.C>=2?"아버지처럼 지급을 미뤘다. 우물의 상태와 별개로 미지급 채무는 후계자의 이름으로 넘어간다.":s.B<=0?"계약은 약해졌고 우물의 물이 끊겼다. 이미 치른 대가와 별개로 주민들의 생계 대안을 마련해야 한다.":(s.Flags&16)!=0?((s.Flags&4)==0?"중복 청구를 바로잡고 공동 노동으로 대가를 바꿨다. 물은 흐르고, 가족의 기억은 남았다.":"중복 청구를 바로잡았지만 앞선 결산에서 바친 기억은 돌아오지 않았다. 새 계약은 다음 희생을 줄일 것이다."):(s.Flags&4)!=0?"계약은 유지되었다. 누군가는 가족의 얼굴을 잊었다. 장부의 숫자는 맞지만 빈자리는 지워지지 않는다.":"기억 대신 곡물과 계약의 일부를 내어 주었다. 이번 계절은 넘겼지만 줄어든 곳간을 다시 채워야 한다.";
            return End(title, result,
                (s.Flags&20)==20?"계약은 고쳤지만 이미 바친 기억은 돌아오지 않습니다. 장부는 이전의 희생도 함께 기록합니다.":"계약 유지가 곧 좋은 결말은 아닙니다. 어떤 대가를 남겼는지 돌아보세요.",S("남은 곡물",s.A),S("계약 안정",s.B),S("미지급",s.C));
        }
        var choices=new List<Choice>();
        if((s.Flags&1)==0 && s.A>0) choices.Add(C("audit","선대 장부 조사","지난해 납부 영수증 확인 · 곡물 1",s with{A=s.A-1,Flags=s.Flags|1}));
        if((s.Flags&2)==0) choices.Add(C("talk","매월에게 사정 듣기","공동 노동 대가 협상 · 결산은 진행되지 않음",s with{Flags=s.Flags|2}));
        choices.Add(C("memory","기억을 대가로 계약 유지","안정 +1 · 주민의 가족 기억 하나 소실",s with{Step=s.Step+1,B=s.B+1,Flags=s.Flags|4}));
        choices.Add(C("defer","이번 대가 지급 연기","곡물 보존 · 미지급 +1 · 안정 -1",s with{Step=s.Step+1,B=System.Math.Max(0,s.B-1),C=s.C+1}));
        if(s.A>=d.Values[2]) choices.Add(C("grain","곡물로 대가 대체","곡물 -"+d.Values[2]+" · 안정 유지",s with{Step=s.Step+1,A=s.A-d.Values[2]}));
        if((s.Flags&3)==3 && (s.Flags&16)==0) choices.Add(C("renegotiate","중복 청구 취소 · 공동 노동 계약","영수증과 주민 동의 확보 · 안정 +1 · 새 계약",s with{Step=s.Step+1,B=s.B+1,Flags=s.Flags|16}));
        choices.Add(C("sever","우물 계약 일부 해지","안정 -2 · 대가 없음 · 물 공급 위험",s with{Step=s.Step+1,B=System.Math.Max(0,s.B-2)}));
        string feedback=(s.Flags&16)!=0?"매월: 기억 대신 우리 손으로 갚을 수 있으면 좋겠어요. 새 계약이 장부에 남았습니다.":(s.Flags&1)!=0?"발견한 영수증: 매월네는 지난해 이미 납부했다. 선대 장부에는 같은 대가가 다시 적혀 있다.":"할아버지는 물을 빌었고, 아버지는 대가를 미뤘다. 오늘 청구 대상은 매월네다.";
        return new("결산 "+(s.Step+1)+"일 · 마르지 않는 우물","세 차례의 결산을 마치세요. 장부를 조사하고 주민과 협상하면 새로운 계약 방식이 열립니다.",feedback,
            new[]{S("곡물",s.A),S("계약 안정",s.B),S("미지급",s.C),S("결산",(s.Step+1)+" / 3")},choices.ToArray());
    }
}
