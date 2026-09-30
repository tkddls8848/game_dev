using System;
using System.Collections.Generic;
using UnityEngine;
using FarmErosion.Sim;

// 다른 게임(내일의 지도) 전용. **farm-erosion 작업에서 건드리지 말 것.**
//
// Art.cs 한 파일에 바다·섬·소품이 모두 있어서 여러 사람이 동시에 손댈 수 없었다.
// partial class 라 파일만 갈라도 소유권이 갈린다. **구역 밖 메서드를 이 파일로 옮기지 말 것** —
// 옮기는 순간 다시 한 파일이 되고 병렬 작업이 충돌한다.
public sealed partial class LowpolyGame
{
    void Townhouse(float x,float z,float w,float d,int floors,int variant)
    {
        float baseY=.53f,h=floors*.76f;
        Color wall=variant%3==0?C("cabda5"):variant%3==1?C("d7cdb6"):C("afa995");
        Cube("Raised pavement",x,.40f,z,w+.45f,.18f,d+.45f,C("a59f8b"));
        Cube("Plaster facade",x,baseY+h/2,z,w,h,d,wall);
        Cube("Stone foundation",x,.62f,z,w+.06f,.19f,d+.06f,stone);
        for(int floor=0;floor<floors;floor++)
        {
            float yy=baseY+.46f+floor*.76f;
            for(int j=0;j<2;j++){Window(x-w*.26f+j*w*.52f,yy,z-d/2-.025f,(floor+j+variant)%3==0);Window(x+w/2+.025f,yy,z-d*.26f+j*d*.52f,(floor+j+variant)%4==0,true);}
            if(floor>0)Cube("Floor cornice",x,yy-.37f,z,w+.06f,.055f,d+.06f,C("d6ccaf"));
        }
        Roof(x,baseY+h,z,w+.22f,d+.22f,.43f,C(variant%2==0?"985a3c":"8b5c43"));
        Cube("Front door",x,.88f,z-d/2-.045f,.37f,.68f,.065f,C("476258"));
        var awning=Cube("Shop awning",x,1.29f,z-d/2-.22f,w*.82f,.065f,.44f,C("4a695e"));awning.transform.rotation=Quaternion.Euler(12,0,0);
        for(int j=0;j<4;j++)Cube("Awning stripe",x-w*.30f+j*w*.2f,1.33f,z-d/2-.22f,.07f,.025f,.42f,C("b5b5a0"));
        if(variant%2==0){Cube("Balcony",x,1.88f,z-d/2-.22f,w*.85f,.09f,.40f,stone);Rail(new Vector3(x-w*.43f,1.94f,z-d/2-.43f),new Vector3(x+w*.43f,1.94f,z-d/2-.43f),.30f);}
        for(int j=0;j<2;j++) {float xx=x-w*.5f+j*w;Cube("Planter",xx,.64f,z-d/2-.33f,.27f,.25f,.28f,wood);Rock(new Vector3(xx,.88f,z-d/2-.33f),new Vector3(.23f,.35f,.22f),C("5c7550"),"Potted shrub");}
    }
    void Person(float x,float z,Color coat)
    {
        Cube("Resident coat",x,.96f,z,.15f,.30f,.12f,coat);
        Rock(new Vector3(x,1.18f,z),new Vector3(.075f,.16f,.073f),C("c3a080"),"Resident head");
        for(int i=-1;i<=1;i+=2){Beam(new Vector3(x+i*.05f,.81f,z),new Vector3(x+i*.06f,.55f,z+i*.045f),.047f,C("3c443f"));Beam(new Vector3(x+i*.095f,1.07f,z),new Vector3(x+i*.13f,.86f,z),.043f,coat);}
    }
    void StreetLamp(float x,float z)
    {
        Beam(new Vector3(x,.51f,z),new Vector3(x,1.95f,z),.045f,C("48514a"));Beam(new Vector3(x,1.95f,z),new Vector3(x+.23f,1.95f,z),.045f,C("48514a"));
        Cube("Lantern",x+.23f,1.92f,z,.17f,.16f,.16f,C("edcf82"));Cube("Lantern lid",x+.23f,2.02f,z,.23f,.04f,.23f,C("48514a"));
    }
    void MapScene()
    {
        // The city is a physical planning model on a drafting desk.
        Cube("Walnut desk",0,-.42f,0,70,.5f,70,C("6e503c"));
        for(int j=-35;j<36;j++){float z=j*.5f;Cube("Wood grain",0,-.158f,z,70,.003f,R(.006f,.022f),C(j%3==0?"826045":"644733"));}
        Cube("Drawing sheet",0,-.11f,0,15,.035f,13,C("c2b89c"));
        for(int i=-13;i<=13;i++){Cube("Drafting grid",i*.5f,-.083f,0,.008f,.004f,13,C("a9a48f"));Cube("Drafting grid",0,-.082f,i*.5f,15,.004f,.008f,C("a9a48f"));}
        Cube("City plinth",0,.11f,0,11,.42f,10.4f,C("b2ab93"));
        for(int i=-10;i<=10;i++)for(int j=-9;j<=9;j++)Cube("Paving slab",i*.51f,.34f,j*.53f,.495f,.07f,.512f,C((i+j)%3==0?"b4af9d":"c0bba8"));
        Cube("Road main",.32f,.395f,0,1.55f,.06f,9.95f,C("646b66"));
        Cube("Road cross",0,.399f,.45f,10.75f,.06f,1.25f,C("646b66"));
        for(int i=-8;i<=8;i++)Cube("Lane markings",.31f,.439f,i*.52f,.035f,.007f,.25f,C("d0cbb6"));
        for(int i=-9;i<=9;i++)Cube("Cross markings",i*.52f,.441f,.46f,.23f,.007f,.032f,C("d0cbb6"));
        for(int i=-1;i<=1;i+=2){Cube("Street curb",.32f+i*.83f,.46f,0,.11f,.14f,10,C("aaa794"));Cube("Street curb",0,.46f,.45f+i*.7f,10.8f,.14f,.09f,C("aaa794"));}
        Townhouse(-3.7f,3.10f,1.50f,1.5f,3,0);Townhouse(-1.68f,3.16f,1.52f,1.6f,2,1);
        Townhouse(-3.80f,.10f,1.4f,1.75f,4,2);Townhouse(-1.76f,-1.65f,1.4f,1.6f,2,0);
        Townhouse(-3.80f,-3.45f,1.4f,1.48f,2,1);Townhouse(2.32f,-2.90f,1.63f,1.65f,2,0);Townhouse(4.23f,-2.85f,1.35f,1.75f,2,1);
        // Hospital: stepped roof, glazing, sign, entrance and perimeter fence.
        Cube("Hospital foundation",2.90f,.47f,3.10f,3.4f,.17f,2.65f,C("aca991"));
        Cube("Hospital facade",2.90f,1.54f,3.14f,2.9f,2.02f,2.25f,C("d5d1bc"));
        Cube("Hospital cornice",2.9f,2.57f,3.14f,3.02f,.12f,2.38f,C("e0dac3"));
        Cube("Roof machinery",3.28f,2.93f,3.3f,.70f,.61f,.85f,C("c5c2af"));Cube("Roof machinery",2.05f,2.79f,3.5f,.71f,.32f,.75f,C("b4b8ad"));
        for(int j=0;j<4;j++){Window(1.8f+j*.7f,2.04f,1.995f,false);Window(4.38f,1.95f,2.50f+j*.38f,false,true);}
        Cube("Glass entrance",2.9f,1.02f,1.99f,1.08f,.98f,.045f,C("ddc792"));for(int j=0;j<3;j++)Cube("Entrance frame",2.4f+j*.5f,1.02f,1.95f,.035f,1.05f,.06f,C("778178"));
        Cube("Hospital cross",2.9f,2.32f,1.94f,.49f,.15f,.06f,C("477565"));Cube("Hospital cross",2.9f,2.32f,1.91f,.15f,.48f,.06f,C("477565"));
        if(node.state.b==0)Cube("Hospital wall",1.42f,.77f,1.59f,1.75f,.61f,.16f,C("949989"));
        else for(int j=0;j<5;j++)Cube("Pedestrian crossing",.32f,.446f,1.18f+j*.14f,1.20f,.01f,.08f,C("e0d8bd"));
        // Revision overlay remains tied to the canonical road state.
        Color route=node.state.a==1?C("73dad3"):C("d9b55c");
        float width=node.state.a==1?1.30f:.46f;
        for(int side=-1;side<=1;side+=2)Beam(new Vector3(.32f+side*width/2,.49f,-2.45f),new Vector3(.32f+side*width/2,.49f,1.8f),.038f,route,"Survey line");
        if(node.state.a==1)for(int j=0;j<9;j++)Cube("Proposed road glow",.32f,.449f,-2.3f+j*.46f,1.20f,.012f,.30f,C("4caaa2"));
        float dx=node.state.c==1?-2.60f:-.76f;
        Cube("Basement stairwell",dx,.47f,-2.98f,.69f,.10f,.87f,C("514d3e"));
        for(int j=0;j<5;j++)Cube("Basement steps",dx,.50f+j*.042f,-3.25f+j*.14f,.57f,.10f,.15f,C("aaa489"));
        Rail(new Vector3(dx-.36f,.55f,-3.42f),new Vector3(dx-.36f,.55f,-2.55f),.40f);
        Cube("Tenant door light",dx,.69f,-2.50f,.37f,.30f,.04f,C("eed38d"));
        for(int i=0;i<7;i++){float x=i<3?-4.8f:4.9f,z=-4+i*1.4f;if(z<4.8f)Tree(x,.46f,z,.64f);}
        for(int i=0;i<5;i++)StreetLamp(i%2==0?-0.66f:1.27f,-3.6f+i*1.65f);
        for(int i=0;i<12;i++){float x=i<6?-.91f:1.39f;float z=-3.9f+(i%6)*1.47f;Person(x,z,C(i%3==0?"4d6f64":i%3==1?"aa8b59":"d0c5a4"));}
        float carZ=node.terminal&&node.state.a==1&&node.state.b==1?1.02f:-3.55f;
        int carStart=world.transform.childCount;
        Cube("Ambulance body",.3f,.77f,carZ,.63f,.53f,1.10f,C("e3e0d0"));Cube("Ambulance windshield",.3f,.89f,carZ-.56f,.48f,.21f,.025f,C("485b5b"));
        Cube("Red ambulance stripe",.63f,.75f,carZ,.025f,.08f,.90f,C("b65a45"));Cube("Emergency light",.3f,1.08f,carZ+.16f,.32f,.11f,.16f,C("b35542"));
        for(int side=-1;side<=1;side+=2)for(int wheel=-1;wheel<=1;wheel+=2)Rock(new Vector3(.3f+side*.32f,.58f,carZ+wheel*.32f),new Vector3(.06f,.23f,.13f),C("333c39"),"Wheel");
        if(node.terminal&&node.state.a==1&&node.state.b==1)
        {
            int partCount=world.transform.childCount-carStart;var vehicle=new GameObject("Ambulance arrival");vehicle.transform.SetParent(world.transform);
            for(int i=0;i<partCount;i++)world.transform.GetChild(carStart).SetParent(vehicle.transform,true);
            vehicle.AddComponent<ArrivalMotion>();
        }
        // Desk props frame the diorama rather than filling the playable city.
        var paper=Cube("Planning notes",-6.2f,-.05f,-.4f,2,.03f,3.3f,C("d6cdb1"));paper.transform.rotation=Quaternion.Euler(0,-14,0);
        for(int j=0;j<8;j++)Cube("Handwritten rules",-6.1f,-.02f,-1.5f+j*.27f,1.05f,.005f,.025f,C("807b66"));
        Beam(new Vector3(-4,-.02f,-6.05f),new Vector3(1.4f,-.02f,-6.75f),.12f,C("363f39"),"Drafting pencil");
        Cube("Notebook",5.9f,.02f,-4.8f,2,.36f,3.3f,C("3e4640"));Cube("Notebook paper",5.85f,-.01f,-4.8f,1.9f,.21f,3.2f,C("bdb49b"));
        Tree(-6.4f,-.05f,5.3f,1.5f);Cube("Desk planter",-6.4f,.19f,5.3f,1.0f,.65f,.95f,C("676956"));
    }
    Vector3 PlotPosition(PlotState p)=>new Vector3((p.X-2.5f)*1.47f,.80f,(p.Y-1.5f)*1.47f);
}
