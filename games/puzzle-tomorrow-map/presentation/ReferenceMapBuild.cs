using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class ReferenceMapBuild
{
    [MenuItem("Tomorrow Map/Build reference edition")]
    public static void Build()
    {
        foreach(string name in new[]{"city","desk","vehicle"})
        {
            string path="Assets/Resources/ReferenceCity/"+name+".png";
            if(!File.Exists(path)||!File.Exists("Assets/Resources/ReferenceCity/"+name+".bytes"))throw new System.Exception("Missing baked city asset: "+name);
            var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.maxTextureSize=name=="city"?8192:4096;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.sRGBTexture=true;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Trilinear;imp.SaveAndReimport();
        }
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
        foreach(string name in new[]{"Tomorrow/Baked","Tomorrow/Overlay","Tomorrow/Optics"})
        {var shader=Shader.Find(name);if(shader==null)throw new System.Exception("Missing shader "+name);bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}}
        graphics.ApplyModifiedProperties();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Tomorrow Map / reference edition").AddComponent<ReferenceMap>();
        Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/ReferenceMap.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/ReferenceMap.unity",true)};
        PlayerSettings.companyName="Orca Studio";PlayerSettings.productName="Tomorrow Map";PlayerSettings.colorSpace=ColorSpace.Gamma;PlayerSettings.defaultScreenWidth=1672;PlayerSettings.defaultScreenHeight=941;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/ReferenceMap.unity"},locationPathName="Build/TomorrowMap.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(result.summary.result!=BuildResult.Succeeded)throw new System.Exception("Reference build failed: "+result.summary.result);
        Debug.Log("REFERENCE_BUILD_SUCCEEDED");
    }
}
