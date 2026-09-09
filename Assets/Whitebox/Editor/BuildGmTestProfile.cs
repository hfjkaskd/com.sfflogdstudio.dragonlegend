using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildGmTestProfile
{
    public static void Save()
    {
        string[] ids={"US_ConfigTest","US_BundledDefault","US_BundledOrganic"};
        string[] names={"SelectConfigTest","SelectBundledDefault","SelectBundledOrganic"};
        string[] paths={"Remote/cp_test","Bundled/GoldenDragon_default","Bundled/GoldenDragon_organic"};
        string[] captions={"US / cp_test (local)","US / bundled default","US / bundled organic"};
        const string path="Assets/Resources/Whitebox/GameEntry.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var panel=new SerializedObject(root.GetComponent<RecoveredGmPanel>());
            var buttons=panel.FindProperty("profileButtons");var groups=panel.FindProperty("groups");
            buttons.arraySize=2+ids.Length;groups.arraySize=2+ids.Length;
            for(int i=0;i<ids.Length;i++) {
                string profilePath="Assets/Whitebox/Settings/"+ids[i]+".asset";
                var profile=AssetDatabase.LoadAssetAtPath<LaunchProfile>(profilePath);
                if(profile==null){profile=ScriptableObject.CreateInstance<LaunchProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
                profile.profileId=ids[i];profile.countryCode="US";profile.languageType=0;profile.isA=false;
                profile.snapshotPath="RecoveredConfig/"+paths[i]+".json";
                profile.cashPresentationEnabled=true;profile.advertisementPresentationEnabled=true;
                profile.evidenceNote="Explicit local snapshot selection; not an assertion of server country or AB routing.";
                EditorUtility.SetDirty(profile);
                var prior=root.transform.Find(names[i]);
                if(prior!=null)Object.DestroyImmediate(prior.gameObject);
                var template=root.transform.Find("SelectAlternative").GetComponent<Button>();
                var button=Object.Instantiate(template,root.transform,false);button.name=names[i];
                var rect=(RectTransform)button.transform;
                rect.anchoredPosition+=new Vector2(0,-(rect.rect.height+12)*(i+1));
                button.onClick=new Button.ButtonClickedEvent();
                button.GetComponentInChildren<Text>().text=captions[i];
                var binding=button.gameObject.AddComponent<RecoveredProfileButton>();
                var serialized=new SerializedObject(binding);
                serialized.FindProperty("button").objectReferenceValue=button;
                serialized.FindProperty("entry").objectReferenceValue=root.GetComponent<GameEntry>();
                serialized.FindProperty("profile").objectReferenceValue=profile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                buttons.GetArrayElementAtIndex(i+2).objectReferenceValue=button;
                groups.GetArrayElementAtIndex(i+2).objectReferenceValue=button.GetComponent<CanvasGroup>();
                }
            panel.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
