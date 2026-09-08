using UnityEditor;
using UnityEngine;

// Asset authoring only. Runtime uses the saved native components on every platform.
public static class BuildReelMask
{
    public static void Save()
    {
        const string path = "Assets/Resources/RecoveredSymbols/Reel.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            var clip = root.transform.Find("Clip");
            var mask = clip.GetComponent<SpriteMask>();
            if (mask == null) mask = clip.gameObject.AddComponent<SpriteMask>();
            mask.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/RecoveredSymbols/Sprites/White1px.asset");
            mask.alphaCutoff = 0.5f;
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = 0; mask.frontSortingOrder = 2;
            mask.backSortingLayerID = 0; mask.backSortingOrder = -1;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
