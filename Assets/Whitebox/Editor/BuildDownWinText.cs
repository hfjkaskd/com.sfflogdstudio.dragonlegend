using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
public static class BuildDownWinText
{
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredUI/DownWinText.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var driver=root.GetComponent<RecoveredDownWinText>();if(driver==null)driver=root.AddComponent<RecoveredDownWinText>();
            var data=new SerializedObject(driver);data.FindProperty("label").objectReferenceValue=root.GetComponent<TextMeshProUGUI>();
            data.FindProperty("delay").floatValue=.3f;data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        BuildSpinPlayfield.Save();
    }
}
