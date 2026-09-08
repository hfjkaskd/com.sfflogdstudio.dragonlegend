using System;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildCoinStopEffect
{
    private const string Folder="Assets/Resources/RecoveredSymbols/CoinStopEffect";
    public static void Save()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/CoinAppearance.prefab");
        var root=new GameObject("Jinbi",typeof(RecoveredCoinStopEffect),typeof(SortingGroup));root.layer=5;
        try {
            root.transform.localScale=Vector3.one*.7f;
            var normal=Material("PmaNormal",10);var additive=Material("PmaAdditive",1);
            var visual=Copy(source.transform,root.transform,normal,additive);
            var appearance=visual.gameObject.AddComponent<RecoveredCoinIdle>();
            var player=visual.gameObject.AddComponent<Animation>();player.playAutomatically=false;
            var setup=ConvertClip(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/RecoveredUI/CoinAppearance/setup.anim"));
            foreach(var name in new[]{"zcjb_chuxian","zcjb_idle"}) {
                var clip=ConvertClip(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/RecoveredUI/CoinAppearance/"+name+".anim"));
                player.AddClip(clip,name);
            }
            var data=new SerializedObject(appearance);data.FindProperty("animationPlayer").objectReferenceValue=player;
            data.FindProperty("setup").objectReferenceValue=setup;data.ApplyModifiedPropertiesWithoutUndo();
            data=new SerializedObject(root.GetComponent<RecoveredCoinStopEffect>());
            data.FindProperty("appearance").objectReferenceValue=appearance;
            var reveal=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/CoinReveal.prefab"),root.transform)).GetComponent<RecoveredCoinReveal>();
            var glow=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/CoinGlow.prefab"),root.transform)).GetComponent<RecoveredCoinGlow>();
            reveal.gameObject.SetActive(false);glow.gameObject.SetActive(false);
            glow.GetComponent<SortingGroup>().sortingOrder=1;
            data.FindProperty("reveal").objectReferenceValue=reveal;
            data.FindProperty("glow").objectReferenceValue=glow;
            var rewardText=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/CoinRewardText.prefab"),root.transform)).GetComponent<RecoveredCoinRewardText>();
            data.FindProperty("rewardText").objectReferenceValue=rewardText;
            data.FindProperty("scaleDuration").floatValue=.2f;data.FindProperty("peakScale").floatValue=1;
            data.FindProperty("restingScale").floatValue=.7f;data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/RecoveredSymbols/CoinStopEffect.prefab");
        } finally {Object.DestroyImmediate(root);}
        BuildSpinPlayfield.Save();AssetDatabase.SaveAssets();
    }
    private static Transform Copy(Transform source,Transform parent,Material normal,Material additive)
    {
        var root=new GameObject(source.name);root.layer=5;var transform=root.transform;transform.SetParent(parent,false);
        transform.localPosition=source.localPosition*.01f;transform.localRotation=source.localRotation;transform.localScale=source.localScale;
        var image=source.GetComponent<Image>();
        if(image!=null) {
            var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=image.sprite;renderer.color=image.color;
            renderer.enabled=image.enabled;renderer.sharedMaterial=image.material.GetFloat("_DestinationBlend")==1?additive:normal;
            renderer.sortingOrder=int.Parse(source.parent.name.Substring(4),System.Globalization.CultureInfo.InvariantCulture);
            var size=((RectTransform)source).sizeDelta;var rect=image.sprite.rect;
            transform.localScale=Vector3.Scale(transform.localScale,new Vector3(size.x/rect.width,size.y/rect.height,1));
        }
        foreach(Transform child in source)Copy(child,transform,normal,additive);
        return transform;
    }
    private static Material Material(string name,int destination)
    {
        var path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("DragonLegend/Recovered PMA Sprite")){name=name};AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_DestinationBlend",destination);return material;
    }
    private static AnimationClip ConvertClip(AnimationClip source)
    {
        var clip=new AnimationClip{name=source.name,legacy=true,frameRate=source.frameRate,wrapMode=source.wrapMode};
        foreach(var binding in AnimationUtility.GetCurveBindings(source)) {
            var curve=AnimationUtility.GetEditorCurve(source,binding);
            if(binding.type==typeof(Transform)&&binding.propertyName.StartsWith("m_LocalPosition.",StringComparison.Ordinal)) {
                var keys=curve.keys;
                for(int i=0;i<keys.Length;i++){var key=keys[i];key.value*=.01f;key.inTangent*=.01f;key.outTangent*=.01f;keys[i]=key;}
                curve.keys=keys;
            }
            clip.SetCurve(binding.path,binding.type==typeof(Image)?typeof(SpriteRenderer):binding.type,binding.propertyName,curve);
        }
        string path=Folder+"/"+source.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing==null){AssetDatabase.CreateAsset(clip,path);return clip;}
        EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);return existing;
    }
}
