using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DragonLegend.Whitebox;

public static class ValidateMainSkin
{
    [Serializable] private class Report
    {
        public int assets, prefabs, transforms, buttons;
        public bool runtimeReady;
        public string error;
    }

    [MenuItem("Dragon Legend/Main Skin/Validate and Capture %&k")]
    public static void Validate()
    {
        var report=new Report();
        try
        {
            foreach(var guid in AssetDatabase.FindAssets("",new[]{"Assets/Resources/MainSkin"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(AssetDatabase.IsValidFolder(path))continue;
                var asset=AssetDatabase.LoadMainAssetAtPath(path);
                if(asset==null)throw new InvalidOperationException("Asset failed to import: "+path);
                report.assets++;
                if(asset is Shader shader && ShaderUtil.ShaderHasError(shader))
                    throw new InvalidOperationException("Shader failed: "+path);
                if(!(asset is GameObject prefab))continue;
                report.prefabs++;
                report.transforms+=prefab.GetComponentsInChildren<Transform>(true).Length;
                foreach(var component in prefab.GetComponentsInChildren<Component>(true))
                    if(component==null)throw new InvalidOperationException("Missing script: "+path);
                foreach(var button in prefab.GetComponentsInChildren<Button>(true))
                {
                    report.buttons++;
                    if(button.onClick.GetPersistentEventCount()!=0)
                        throw new InvalidOperationException("Serialized UI event: "+path);
                }
            }
            var entry=UnityEngine.Object.FindObjectOfType<GameEntry>();
            report.runtimeReady=Application.isPlaying&&entry!=null&&entry.Playfield!=null&&entry.Playfield.Error==null;
            Directory.CreateDirectory("Artifacts/MainSkin");
            if(report.runtimeReady)ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts/MainSkin/main-screen.png"));
            Debug.Log("Main skin validation passed: "+report.assets+" assets; runtime ready="+report.runtimeReady);
        }
        catch(Exception error){report.error=error.ToString();Debug.LogException(error);}
        finally
        {
            Directory.CreateDirectory("Artifacts/MainSkin");
            File.WriteAllText("Artifacts/MainSkin/unity-validation.json",JsonUtility.ToJson(report,true));
        }
    }
}
