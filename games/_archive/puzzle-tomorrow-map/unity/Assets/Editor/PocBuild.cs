using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build.Reporting;
public static class PocBuild
{
    public static void Build()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Resources/Models"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid); var imp=(ModelImporter)AssetImporter.GetAtPath(path);
            imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            // Blender 로 구운 붕괴는 **Legacy** 로 들인다. Generic(Mecanim) 이면 재생에
            // AnimatorController 에셋이 필요한데 그건 스크립트로 만들기 번거롭다.
            // Legacy 면 Animation 컴포넌트에 Play() 한 줄이면 된다.
            if(path.EndsWith("PlotCollapse.fbx"))
            {
                imp.animationType=ModelImporterAnimationType.Legacy; imp.importAnimation=true;
                imp.animationCompression=ModelImporterAnimationCompression.Off;
                // **1:1 로 들인다.** 기본값은 파일 단위를 따라 메시를 0.01 로 줄이고 루트를 100 배
                // 키우는데, 그러면 구운 위치 키프레임이 그 100 배로 곱해져 조각이 50 유닛 밖으로
                // 날아간다. 오류도 안 나고 화면에서 사라질 뿐이라 찾는 데 한참 걸렸다.
                imp.useFileScale=false; imp.globalScale=1f;
            }
            imp.SaveAndReimport();
        }
        var standard=Shader.Find("Standard");
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
        // 실행 중에만 Shader.Find 로 찾는 셰이더는 전부 여기 등록해야 한다.
        // 빠뜨리면 에디터에서는 멀쩡하고 **빌드에서만** ArgumentNullException(shader) 으로 죽는다.
        foreach(var shader in new[]{standard,Shader.Find("PoC/Faceted"),Shader.Find("PoC/Water"),
                                    Shader.Find("PoC/Atmosphere"),Shader.Find("PoC/Mist")})
        {
            if(shader==null)throw new System.Exception("Required presentation shader missing");
            bool present=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)present=true;
            if(!present){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;}
        }
        graphics.ApplyModifiedProperties();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("PoC controller").AddComponent<LowpolyGame>();
        Directory.CreateDirectory("Assets/Scenes"); EditorSceneManager.SaveScene(scene,"Assets/Scenes/Main.unity");
        PlayerSettings.companyName="Orca Studio"; PlayerSettings.productName=File.ReadAllText("Assets/Resources/mode.txt").Trim()=="farm"?"Eroding Fields":"Tomorrow Map";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{ scenes=new[]{"Assets/Scenes/Main.unity"},locationPathName="Build/"+(PlayerSettings.productName=="Eroding Fields"?"FarmErosion":"TomorrowMap")+".exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
        if(result.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+result.summary.result);
        Debug.Log("POC_BUILD_SUCCEEDED");
    }
}
