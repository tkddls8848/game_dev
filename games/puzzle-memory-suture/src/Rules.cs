using System.Collections.Generic;
using ConceptPoCs;
using static ConceptPoCs.V;
namespace MemorySuture;
public class Rules : IGame
{
    public State Start(Scenario d)=>new();
    public View Describe(State s, Scenario d)
    {
        bool coherent=s.A==1 && s.B==1;
        if(s.Step==1) return End(coherent ? (s.C==1 ? "봉합된 의심" : "남아 있는 온기") : "꿈의 거부 반응",
            coherent ? (s.C==1 ? "형이 자신을 지켜봤다는 기억이 감시로 정착했다. 의뢰는 성공했지만, 의뢰인은 이제 가족의 관심도 두려워한다." : "관찰과 열쇠의 기억은 연결됐다. 하지만 생일의 온기를 남겨 두어 의뢰인은 형을 의심하면서도 대화할 여지를 잃지 않았다.") : "열쇠를 쥔 사람과 문을 잠근 행동이 연결되지 않는다. 꿈은 모순을 찾아 새 기억을 밀어냈다.",
            "논리의 연결과 감정의 대가를 따로 살펴보세요.",S("모순",coherent?0:1),S("감정",s.C==1?"두려움":"온기"));
        var choices=new List<Choice>{
            C("person","인물 조각 교체",s.A==0?"나 → 형":"형 → 나",s with{A=1-s.A}),
            C("act","행동 조각 교체",s.B==0?"선물을 건넨다 → 문을 잠근다":"문을 잠근다 → 선물을 건넨다",s with{B=1-s.B}),
            C("emotion","감정 실 바꾸기",s.C==0?"온기 → 두려움":"두려움 → 온기",s with{C=1-s.C}),
            C("implant","이 기억을 봉합",coherent?"인물과 행동의 연결이 성립합니다":"모순 경고: 문을 잠글 수 있는 사람은 열쇠를 가진 형입니다",s with{Step=1})};
        return new("기억 07 · 열두 번째 생일","의뢰: 형의 보호를 감시로 의심하게 만들기. 확립된 사실은 바꿀 수 없습니다: 집 열쇠는 형만 가지고 있습니다.",
            coherent?"논리 연결 성립. 남길 감정에 따라 후유증이 달라집니다.":"작업대에서 인물과 행동을 바꿔 보세요. 확립된 사실과 충돌하면 거부 반응이 납니다.",
            new[]{S("인물",s.A==0?"나":"형"),S("행동",s.B==0?"선물을 건넨다":"문을 잠근다"),S("감정",s.C==0?"온기":"두려움")},choices.ToArray());
    }
}
