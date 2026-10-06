using System;
using System.Collections.Generic;
using UnityEngine;
using FarmErosion.Sim;

// 공용 프리미티브. 모든 구역이 쓴다 — **여기를 고치면 전 구역에 영향이 간다.**
//
// Art.cs 한 파일에 바다·섬·소품이 모두 있어서 여러 사람이 동시에 손댈 수 없었다.
// partial class 라 파일만 갈라도 소유권이 갈린다. **구역 밖 메서드를 이 파일로 옮기지 말 것** —
// 옮기는 순간 다시 한 파일이 되고 병렬 작업이 충돌한다.
public sealed partial class LowpolyGame
{
    static Color C(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c); return c; }
    System.Random artRandom=new System.Random(729);
    float R(float a,float b)=>a+(b-a)*(float)artRandom.NextDouble();
    Color wood=>C("735039"); Color grass=>C("849451"); Color stone=>C("857e6d");
    Material facetMaterial,waterMaterial,mistMaterial;
    GameObject Cube(string n,float x,float y,float z,float w,float h,float d,Color c)=>Box(n,new Vector3(x,y,z),new Vector3(w,h,d),c);
    GameObject MeshObject(string n,List<Vector3> vertices,List<int> indices,List<Color> colors)
    {
        var o=new GameObject(n);o.transform.SetParent(world.transform);
        Vector3 center=Vector3.zero;foreach(var point in vertices)center+=point;center/=vertices.Count;
        for(int i=0;i<vertices.Count;i++)vertices[i]-=center;o.transform.position=center;
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();generatedMeshes.Add(mesh);
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        if(facetMaterial==null)facetMaterial=new Material(Shader.Find("PoC/Faceted"));
        o.AddComponent<MeshRenderer>().sharedMaterial=facetMaterial; return o;
    }
    void Tri(List<Vector3> v,List<int> t,List<Color> colors,Vector3 a,Vector3 b,Vector3 c,Color col)
    { int k=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(k);t.Add(k+1);t.Add(k+2);colors.Add(col);colors.Add(col);colors.Add(col); }
    GameObject Rock(Vector3 p,Vector3 scale,Color color,string name="Faceted rock")
    {
        var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
        const int count=7;var bottom=new Vector3[count];var middle=new Vector3[count];var top=new Vector3[count];
        for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;bottom[i]=p+Vector3.Scale(new Vector3(Mathf.Cos(a)*R(.65f,.95f),-.5f,Mathf.Sin(a)*R(.65f,.95f)),scale);middle[i]=p+Vector3.Scale(new Vector3(Mathf.Cos(a)*R(.8f,1.05f),R(-.1f,.15f),Mathf.Sin(a)*R(.8f,1.05f)),scale);top[i]=p+Vector3.Scale(new Vector3(Mathf.Cos(a)*R(.32f,.65f),R(.35f,.55f),Mathf.Sin(a)*R(.32f,.65f)),scale);}
        for(int i=0;i<count;i++){int j=(i+1)%count;Color col=color*R(.80f,1.12f);col.a=1;Tri(v,t,colors,bottom[i],middle[i],bottom[j],col);Tri(v,t,colors,bottom[j],middle[i],middle[j],col);Tri(v,t,colors,middle[i],top[i],middle[j],col);Tri(v,t,colors,middle[j],top[i],top[j],col);Tri(v,t,colors,top[i],p+new Vector3(0,scale.y*.48f,0),top[j],col);}
        return MeshObject(name,v,t,colors);
    }
    void Beam(Vector3 a,Vector3 b,float width,Color color,string name="Timber")
    { var o=Box(name,(a+b)/2,new Vector3(width,Vector3.Distance(a,b),width),color);o.transform.up=(b-a).normalized; }
    void Roof(float x,float y,float z,float w,float d,float h,Color color)
    {
        var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
        Vector3 a=new Vector3(x-w/2,y,z-d/2),b=new Vector3(x+w/2,y,z-d/2),c=new Vector3(x-w/2,y,z+d/2),e=new Vector3(x+w/2,y,z+d/2),f=new Vector3(x,y+h,z-d/2),g=new Vector3(x,y+h,z+d/2);
        Tri(v,t,colors,a,f,b,C("cdbb94"));Tri(v,t,colors,c,e,g,C("cdbb94"));Tri(v,t,colors,a,c,f,color);Tri(v,t,colors,c,g,f,color);Tri(v,t,colors,f,g,b,color*.87f);Tri(v,t,colors,g,e,b,color*.87f);MeshObject("Pitched tiled roof",v,t,colors);
        for(int i=0;i<9;i++){float zz=z-d/2+i*d/8;Beam(new Vector3(x-w/2,y+.025f,zz),new Vector3(x,y+h+.025f,zz),.035f,color*.72f);Beam(new Vector3(x,y+h+.025f,zz),new Vector3(x+w/2,y+.025f,zz),.035f,color*.72f);}
        for(int j=1;j<5;j++){float xx=j*w/10;float yy=y+h*(1-2*xx/w)+.02f;Beam(new Vector3(x-xx,yy,z-d/2),new Vector3(x-xx,yy,z+d/2),.022f,color*.75f);Beam(new Vector3(x+xx,yy,z-d/2),new Vector3(x+xx,yy,z+d/2),.022f,color*.75f);}
        Cube("Chimney",x+w*.28f,y+h*.66f,z+.25f,.20f,.64f,.25f,C("b5ab90"));Cube("Chimney cap",x+w*.28f,y+h*.66f+.33f,z+.25f,.26f,.09f,.30f,stone);
    }
    void Window(float x,float y,float z,bool lit,bool side=false)
    {
        var frame=Cube("Window frame",x,y,z,side?.055f:.36f,.48f,side?.36f:.055f,C("e2d6bc"));
        Cube("Window glass",x+(side?.033f:0),y,z+(side?0:-.033f),side?.02f:.27f,.37f,side?.27f:.02f,lit?C("efd18a"):C("35494b"));
        Cube("Mullion",x+(side?.05f:0),y,z+(side?0:-.05f),side?.02f:.025f,.39f,side?.025f:.02f,C("b9b399"));
        Cube("Sill",x,y-.25f,z,side?.15f:.45f,.055f,side?.45f:.15f,stone);
    }
    void Rail(Vector3 a,Vector3 b,float height)
    {
        int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.22f);
        for(int i=0;i<=count;i++)Beam(Vector3.Lerp(a,b,(float)i/count),Vector3.Lerp(a,b,(float)i/count)+Vector3.up*height,.032f,C("3e4b43"));
        Beam(a+Vector3.up*height,b+Vector3.up*height,.042f,C("3e4b43"));
    }
}
