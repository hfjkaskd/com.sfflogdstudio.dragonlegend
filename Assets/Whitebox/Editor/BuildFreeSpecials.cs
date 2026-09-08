using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;

public static class BuildFreeSpecials
{
    public static void Save()
    {
        const string path="Assets/Resources/RecoveredSymbols/FreeReels.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {Attach(root);PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();}
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Attach(GameObject root)
    {
        var existing=root.transform.Find("Specials");
        var node=existing==null?new GameObject("Specials",typeof(RecoveredFreeSpecials)):existing.gameObject;
        node.transform.SetParent(root.transform,false);
        var storage=node.transform.Find("Inactive");
        if(storage==null){storage=new GameObject("Inactive").transform;storage.SetParent(node.transform,false);}
        storage.gameObject.SetActive(false);
        var specials=node.GetComponent<RecoveredFreeSpecials>();var data=new SerializedObject(specials);
        data.FindProperty("coinPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredFreeCoin>("Assets/Resources/RecoveredSymbols/FreeCoin.prefab");
        data.FindProperty("ballPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredFreeBall>("Assets/Resources/RecoveredSymbols/FreeBall.prefab");
        data.FindProperty("storage").objectReferenceValue=storage;
        var lamps=node.GetComponent<RecoveredFreeLampFlights>();if(lamps==null)lamps=node.AddComponent<RecoveredFreeLampFlights>();
        var lampSettings=new SerializedObject(lamps);
        lampSettings.FindProperty("flightPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredLampFlight>("Assets/Resources/RecoveredSymbols/LampFlight.prefab");
        lampSettings.FindProperty("flashPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredLampFlash>("Assets/Resources/RecoveredUI/LampFlash.prefab");
        lampSettings.FindProperty("storage").objectReferenceValue=storage;lampSettings.FindProperty("arrivalSound").stringValue="exp";
        lampSettings.ApplyModifiedPropertiesWithoutUndo();data.FindProperty("lampFlights").objectReferenceValue=lamps;
        data.FindProperty("slotCenter").vector3Value=new Vector3(0,.86f,0);
        data.FindProperty("coinScale").floatValue=.7f;data.FindProperty("ballScale").floatValue=.8f;
        data.FindProperty("coinStopSound").stringValue="coinshow";
        data.FindProperty("ballStopSound").stringValue="scatterShow";
        data.FindProperty("stopVibrationMilliseconds").intValue=200;
        data.ApplyModifiedPropertiesWithoutUndo();
        data=new SerializedObject(root.GetComponent<RecoveredFreeReels>());data.FindProperty("specials").objectReferenceValue=specials;data.ApplyModifiedPropertiesWithoutUndo();
    }
}
