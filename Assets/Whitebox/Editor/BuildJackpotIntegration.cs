using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildJackpotIntegration
{
    public static void Save()
    {
        BuildJackpotPopup.Save();BuildSpinPlayfield.Save();
        const string path="Assets/Resources/Whitebox/GameEntry.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var prior=root.transform.Find("GM SDK");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            var host=new GameObject("GM SDK",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(RecoveredAdSimulationControls));
            host.layer=5;host.transform.SetParent(root.transform,false);
            var rect=(RectTransform)host.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var canvas=host.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=5000;
            var panel=new GameObject("Ad result",typeof(RectTransform),typeof(Image));panel.layer=5;panel.transform.SetParent(host.transform,false);
            rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-30);rect.sizeDelta=new Vector2(800,110);
            panel.GetComponent<Image>().color=new Color(0,0,0,.85f);
            var entry=new SerializedObject(root.GetComponent<GameEntry>());
            var source=(Button)entry.FindProperty("selectDefault").objectReferenceValue;
            var reward=CopyButton(source,panel.transform,"Reward ad", "GM: Ad reward",-190);
            var fail=CopyButton(source,panel.transform,"Fail ad", "GM: Ad failed",190);
            var settings=new SerializedObject(host.GetComponent<RecoveredAdSimulationControls>());
            settings.FindProperty("panel").objectReferenceValue=panel;
            settings.FindProperty("reward").objectReferenceValue=reward;settings.FindProperty("fail").objectReferenceValue=fail;settings.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            entry.FindProperty("adControls").objectReferenceValue=host.GetComponent<RecoveredAdSimulationControls>();entry.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static Button CopyButton(Button source,Transform parent,string name,string text,float x)
    {
        var button=Object.Instantiate(source,parent,false);button.name=name;
        var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,0);rect.sizeDelta=new Vector2(360,80);
        button.GetComponentInChildren<Text>(true).text=text;return button;
    }
}
