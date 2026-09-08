using System;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildBonusCollection
{
    [Serializable] public sealed class Layout { public Node[] nodes; }
    [Serializable] public sealed class Node {
        public string name; public int parent; public bool image,idle;
        public float[] position,size,scale,anchorMin,anchorMax,pivot;
    }
    public static void Save()
    {
        BuildBonusCoinIdle.Save();
        var data=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Whitebox/Editor/RecoveredBonusCollection.json"));
        var transforms=new RectTransform[data.nodes.Length];
        try {
            for(int i=0;i<data.nodes.Length;i++) {
                var n=data.nodes[i];GameObject node;
                if(n.idle)node=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/BonusCoinIdle.prefab"));
                else node=new GameObject(n.name,typeof(RectTransform));
                node.name=n.name;node.layer=5;var rect=transforms[i]=(RectTransform)node.transform;
                if(n.parent>=0)rect.SetParent(transforms[n.parent],false);
                rect.anchorMin=V(n.anchorMin);rect.anchorMax=V(n.anchorMax);rect.pivot=V(n.pivot);
                rect.anchoredPosition=V(n.position);rect.sizeDelta=V(n.size);rect.localScale=new Vector3(n.scale[0],n.scale[1],n.scale[2]);
                if(n.image){var graphic=node.AddComponent<Image>();graphic.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/RecoveredUI/zjm_icon_wanfa01.asset");graphic.raycastTarget=false;}
            }
            var collection=transforms[0].gameObject.AddComponent<RecoveredBonusCollection>();
            var serialized=new SerializedObject(collection);var columns=serialized.FindProperty("columns");columns.arraySize=5;
            for(int i=0;i<5;i++) {
                var parent=transforms[0].GetChild(i);var items=columns.GetArrayElementAtIndex(i).FindPropertyRelative("items");items.arraySize=2;
                for(int j=0;j<2;j++)items.GetArrayElementAtIndex(j).objectReferenceValue=parent.GetChild(j);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(transforms[0].gameObject,"Assets/Resources/RecoveredUI/BonusCollection.prefab");
        } finally {if(transforms[0]!=null)Object.DestroyImmediate(transforms[0].gameObject);}
        BuildSpinPlayfield.Save();AssetDatabase.SaveAssets();
    }
    private static Vector2 V(float[] v)=>new Vector2(v[0],v[1]);
}
