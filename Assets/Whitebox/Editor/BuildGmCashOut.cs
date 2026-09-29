using System.Collections.Generic;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildGmCashOut
{
    private static Font font;
    private static readonly List<Text> labels=new List<Text>();
    private const string FontPath="RecoveredArt/Res/Font/msyhbd";
    public static void Save()
    {
        const string path="Assets/Resources/Whitebox/GameEntry.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            labels.Clear();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var gm=root.GetComponent<RecoveredGmPanel>();var gmData=new SerializedObject(gm);
            var old=root.transform.Find("GmCashOutPanel");if(old!=null)Object.DestroyImmediate(old.gameObject);
            old=root.transform.Find("GmCashOutOpen");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var groups=new List<CanvasGroup>();var existing=gmData.FindProperty("groups");
            for(int i=0;i<existing.arraySize;i++){var group=existing.GetArrayElementAtIndex(i).objectReferenceValue as CanvasGroup;if(group!=null)groups.Add(group);}
            gmData.FindProperty("closedCaption").stringValue="GM";gmData.FindProperty("openCaption").stringValue="关闭";
            gmData.FindProperty("fontPath").stringValue=FontPath;
            var toggleText=(Text)gmData.FindProperty("toggleLabel").objectReferenceValue;toggleText.text="GM";
            var profiles=gmData.FindProperty("profileButtons");
            string[] names={"默认配置（非自然量）","备用配置","美国：非自然量测试配置","美国：原包非自然量配置","美国：原包自然量配置","巴西：现金配置"};
            for(int i=0;i<profiles.arraySize;i++){
                var button=(Button)profiles.GetArrayElementAtIndex(i).objectReferenceValue;
                button.GetComponentInChildren<Text>(true).text=names[i];
            }
            var open=Button(root.transform,"GmCashOutOpen","提现任务验证",new Vector2(540,-800),new Vector2(920,72));
            var openCanvas=open.gameObject.AddComponent<Canvas>();openCanvas.overrideSorting=true;openCanvas.sortingOrder=5001;
            open.gameObject.AddComponent<GraphicRaycaster>();
            var menuGroup=open.gameObject.AddComponent<CanvasGroup>();menuGroup.alpha=0;menuGroup.interactable=false;menuGroup.blocksRaycasts=false;groups.Add(menuGroup);
            existing.arraySize=groups.Count;for(int i=0;i<groups.Count;i++)existing.GetArrayElementAtIndex(i).objectReferenceValue=groups[i];gmData.ApplyModifiedPropertiesWithoutUndo();
            var panel=new GameObject("GmCashOutPanel",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(Image));panel.layer=5;panel.transform.SetParent(root.transform,false);
            var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var canvas=panel.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=5002;
            panel.GetComponent<Image>().color=new Color(.035f,.05f,.085f,.985f);
            var status=Label(panel.transform,"Status","提现任务验证",new Vector2(40,-120),new Vector2(1000,860),28,TextAnchor.UpperLeft);
            var close=Button(panel.transform,"Close","关闭验证",new Vector2(900,-52),new Vector2(260,72));
            string[] captions={"上一档位","下一档位","上一任务","下一任务","切换模拟分支","创建测试状态","进度差 1 次","补 1 次进度","补满当前进度","等待时间到期","重新开始等待","恢复原存档","打开原版提现页","刷新验证结果"};
            var actions=new List<Button>{close};
            for(int i=0;i<captions.Length;i++)actions.Add(Button(panel.transform,"Action"+(i+1),captions[i],new Vector2(i%2==0?280:800,-1060-(i/2)*96),new Vector2(490,80)));
            Label(panel.transform,"Help","测试会修改本地存档，首次修改自动备份。\n打开提现页后，请亲自点击原提现按钮检查提示和下一阶段。",new Vector2(40,-1760),new Vector2(1000,120),27,TextAnchor.UpperLeft);
            var tools=root.GetComponent<RecoveredGmCashOutPanel>();if(tools==null)tools=root.AddComponent<RecoveredGmCashOutPanel>();
            var data=new SerializedObject(tools);data.FindProperty("entry").objectReferenceValue=root.GetComponent<GameEntry>();data.FindProperty("gm").objectReferenceValue=gm;
            data.FindProperty("openButton").objectReferenceValue=open;data.FindProperty("panel").objectReferenceValue=panel;data.FindProperty("status").objectReferenceValue=status;data.FindProperty("fontPath").stringValue=FontPath;
            var list=data.FindProperty("actions");list.arraySize=actions.Count;for(int i=0;i<actions.Count;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=actions[i];
            list=data.FindProperty("labels");list.arraySize=labels.Count;for(int i=0;i<labels.Count;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=labels[i];
            string[] tasks={"旋转次数","观看广告","领取奖池奖励","领取大奖","领取宝物","进入免费游戏","收集宝物"};
            list=data.FindProperty("taskNames");list.arraySize=tasks.Length;for(int i=0;i<tasks.Length;i++)list.GetArrayElementAtIndex(i).stringValue=tasks[i];
            data.ApplyModifiedPropertiesWithoutUndo();panel.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(root);labels.Clear();}
    }
    private static Text Label(Transform parent,string name,string text,Vector2 pos,Vector2 size,int fontSize,TextAnchor alignment)
    {
        var node=new GameObject(name,typeof(RectTransform),typeof(Text));node.layer=5;node.transform.SetParent(parent,false);
        var rect=(RectTransform)node.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;
        var label=node.GetComponent<Text>();label.font=font;label.text=text;label.fontSize=fontSize;label.color=Color.white;label.alignment=alignment;label.raycastTarget=false;
        label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;labels.Add(label);return label;
    }
    private static Button Button(Transform parent,string name,string caption,Vector2 pos,Vector2 size)
    {
        var node=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));node.layer=5;node.transform.SetParent(parent,false);
        var rect=(RectTransform)node.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;
        var image=node.GetComponent<Image>();image.color=new Color(.14f,.22f,.34f,1);var button=node.GetComponent<Button>();button.targetGraphic=image;
        var label=Label(node.transform,"Label",caption,Vector2.zero,size,30,TextAnchor.MiddleCenter);
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
        return button;
    }
}
