using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildGmPanel
{
    public static void Save()
    {
        const string path="Assets/Resources/Whitebox/GameEntry.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var entry=new SerializedObject(root.GetComponent<GameEntry>());
            var primary=(Button)entry.FindProperty("selectDefault").objectReferenceValue;
            var alternative=(Button)entry.FindProperty("selectAlternative").objectReferenceValue;
            var prior=root.transform.Find("GmToggle");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            var toggle=Object.Instantiate(primary,root.transform,false);toggle.name="GmToggle";
            var rect=(RectTransform)toggle.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(8,-125);rect.sizeDelta=new Vector2(96,64);
            var inheritedGroup=toggle.GetComponent<CanvasGroup>();if(inheritedGroup!=null)Object.DestroyImmediate(inheritedGroup);
            toggle.GetComponent<Canvas>().sortingOrder=5001;toggle.onClick=new Button.ButtonClickedEvent();
            var label=toggle.GetComponentInChildren<Text>();label.text="GM";label.fontSize=24;
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.sizeDelta=Vector2.zero;
            var panel=root.GetComponent<RecoveredGmPanel>();if(panel==null)panel=root.AddComponent<RecoveredGmPanel>();
            var settings=new SerializedObject(panel);settings.FindProperty("toggle").objectReferenceValue=toggle;settings.FindProperty("toggleLabel").objectReferenceValue=label;
            settings.FindProperty("closedCaption").stringValue="GM";settings.FindProperty("openCaption").stringValue="Close";
            var buttons=settings.FindProperty("profileButtons");buttons.arraySize=2;var groups=settings.FindProperty("groups");groups.arraySize=2;
            var profiles=new[]{primary,alternative};
            for(int i=0;i<profiles.Length;i++) {
                buttons.GetArrayElementAtIndex(i).objectReferenceValue=profiles[i];
                var group=profiles[i].GetComponent<CanvasGroup>();if(group==null)group=profiles[i].gameObject.AddComponent<CanvasGroup>();
                group.alpha=0;group.interactable=false;group.blocksRaycasts=false;groups.GetArrayElementAtIndex(i).objectReferenceValue=group;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        BuildGmTestProfile.Save();
    }
}
