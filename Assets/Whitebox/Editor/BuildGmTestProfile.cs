using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildGmTestProfile
{
    public static void Save()
    {
        const string profilePath="Assets/Whitebox/Settings/US_ConfigTest.asset";
        var profile=AssetDatabase.LoadAssetAtPath<LaunchProfile>(profilePath);
        if(profile==null){profile=ScriptableObject.CreateInstance<LaunchProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
        profile.profileId="US_ConfigTest";profile.countryCode="US";profile.languageType=0;profile.isA=false;
        profile.snapshotPath="RecoveredConfig/Remote/cp_test.json";
        profile.cashPresentationEnabled=true;profile.advertisementPresentationEnabled=true;
        profile.evidenceNote="Explicit local cp_test snapshot selection; not an assertion of server country or AB routing.";
        EditorUtility.SetDirty(profile);
        const string path="Assets/Resources/Whitebox/GameEntry.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var prior=root.transform.Find("SelectConfigTest");
            if(prior!=null)Object.DestroyImmediate(prior.gameObject);
            var template=root.transform.Find("SelectAlternative").GetComponent<Button>();
            var button=Object.Instantiate(template,root.transform,false);button.name="SelectConfigTest";
            var rect=(RectTransform)button.transform;
            rect.anchoredPosition+=new Vector2(0,-rect.rect.height-12);
            button.onClick=new Button.ButtonClickedEvent();
            button.GetComponentInChildren<Text>().text="US / cp_test (local)";
            var binding=button.gameObject.AddComponent<RecoveredProfileButton>();
            var serialized=new SerializedObject(binding);
            serialized.FindProperty("button").objectReferenceValue=button;
            serialized.FindProperty("entry").objectReferenceValue=root.GetComponent<GameEntry>();
            serialized.FindProperty("profile").objectReferenceValue=profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var panel=new SerializedObject(root.GetComponent<RecoveredGmPanel>());
            var buttons=panel.FindProperty("profileButtons");var groups=panel.FindProperty("groups");
            buttons.arraySize=3;groups.arraySize=3;
            buttons.GetArrayElementAtIndex(2).objectReferenceValue=button;
            groups.GetArrayElementAtIndex(2).objectReferenceValue=button.GetComponent<CanvasGroup>();
            panel.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
