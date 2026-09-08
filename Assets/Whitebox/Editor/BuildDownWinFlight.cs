using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
public static class BuildDownWinFlight
{
    public static void Save()
    {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/LampFlight.prefab"));
        try {
            root.name="DownWinFlight";root.transform.localPosition=Vector3.zero;root.SetActive(false);
            var data=new SerializedObject(root.GetComponent<RecoveredLampFlight>());data.FindProperty("curve").enumValueIndex=1;data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredSymbols/DownWinFlight.prefab");
        } finally {Object.DestroyImmediate(root);}
        BuildSpinPlayfield.Save();
    }
}
