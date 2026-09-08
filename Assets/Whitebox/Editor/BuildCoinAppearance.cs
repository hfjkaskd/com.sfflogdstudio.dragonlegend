using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Native region conversion for zcjb_chuxian and zcjb_idle.
// Slot 29 ringadd is a region; the separate mesh with the same name belongs to slot 45.
public static class BuildCoinAppearance
{
    [Serializable] public class Data { public Bone[] bones; public Slot[] slots; public Attachment[] attachments; public Clip[] animations; public Region[] regions; }
    [Serializable] public class Bone { public string name; public int parent; public float[] values; }
    [Serializable] public class Slot { public string name, attachment; public int bone, blend; public float[] color; }
    [Serializable] public class Attachment { public int slot; public string name, key; public float[] values, color; }
    [Serializable] public class Region { public string name; public int[] bounds, offsets; public int rotate; }
    [Serializable] public class Clip { public string name; public float duration; public Timeline[] timelines; }
    [Serializable] public class Timeline { public string domain; public int index, kind; public Frame[] frames; }
    [Serializable] public class Frame { public float time; public float[] values; public string attachment; public int curve; public Bezier[] bezier; }
    [Serializable] public class Bezier { public float[] values; }
    private static string Folder;
    private static readonly string[] Rgba = {"r","g","b","a"};
    public static void Save()
    {
        Build(false);
    }
    public static void SaveLampFlash()
    {
        Build(true);
        BuildSpinPlayfield.Save();
    }
    private static void Build(bool lamp)
    {
        Folder = "Assets/Resources/RecoveredUI/" + (lamp ? "LampFlash" : "CoinAppearance");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(lamp ? "Assets/Whitebox/Editor/RecoveredLampFlash.json" : "Assets/Whitebox/Editor/RecoveredCoinEffect.json"));
        var names = lamp ? new[]{"dianliang"} : new[]{"zcjb_idle","zcjb_chuxian"};
        var slots = lamp ? new[]{2,3,4,5,6,7} : new[]{25,26,27,29,31};
        foreach(var clipName in names) {
        var idle=Array.Find(data.animations,a=>a.name==clipName);
        if(idle==null)throw new InvalidOperationException("Missing original coin clip.");
        foreach(var timeline in idle.timelines) {
            if(timeline.domain=="deform")throw new InvalidOperationException("Idle now requires mesh deformation.");
            if(timeline.domain!="slot" || timeline.kind!=0)continue;
            foreach(var frame in timeline.frames)if(frame.attachment!=null) {
                if(Array.IndexOf(slots,timeline.index)<0)
                    throw new InvalidOperationException("Unconverted visible idle slot.");
                var attachment=Array.Find(data.attachments,a=>a.slot==timeline.index);
                if(attachment.key!=frame.attachment)throw new InvalidOperationException("Unconverted attachment switch.");
            }
        }
        }
        string texturePath = lamp ? "Assets/Resources/RecoveredArt/Res/Spine/sltongbi/ef_sltongbi.png" : "Assets/Resources/RecoveredArt/Res/Spine/棋子/jinbi/ef_jinbi.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        // Original atlas declares pma:true. Transparent RGB dilation corrupts PMA texels.
        if (importer.alphaIsTransparency || importer.textureCompression != TextureImporterCompression.Uncompressed) {
            importer.alphaIsTransparency = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var root = new GameObject(lamp ? "Ef_sltongbi" : "CoinIdle", typeof(RectTransform), typeof(Animation));
        Component driver = lamp ? (Component)root.AddComponent<RecoveredLampFlash>() : root.AddComponent<RecoveredCoinIdle>();
        try {
            root.layer=5; var rect=(RectTransform)root.transform; rect.sizeDelta=lamp ? new Vector2(50,50) : new Vector2(53,57);
            var visual=Rect("Visual",root.transform); visual.anchoredPosition=Vector2.zero;
            var bones=new RectTransform[data.bones.Length];
            var ordering=new Dictionary<Transform,int>();
            for(int i=0;i<bones.Length;i++) {
                var bone=data.bones[i]; var v=bone.values;
                bones[i]=Rect(bone.name,bone.parent<0?visual:bones[bone.parent]);
                bones[i].localPosition=new Vector3(v[1],v[2],0);bones[i].localEulerAngles=new Vector3(0,0,v[0]);
                bones[i].localScale=new Vector3(v[3],v[4],1);
            }
            var normal=Material("PmaNormal",10); var additive=Material("PmaAdditive",1);
            var images=new Image[data.slots.Length];
            foreach(var attachment in data.attachments) {
                if (Array.IndexOf(slots,attachment.slot)<0) continue;
                var slot=data.slots[attachment.slot];var region=Array.Find(data.regions,r=>r.name==attachment.name);
                var b=region.bounds;var offsets=region.offsets;bool rotated=region.rotate==90;
                int packedWidth=rotated?b[3]:b[2],packedHeight=rotated?b[2]:b[3];
                string spritePath=Folder+"/"+region.name+".asset";
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if(sprite==null) {
                    sprite=Sprite.Create(texture,new Rect(b[0],texture.height-b[1]-packedHeight,packedWidth,packedHeight),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                    sprite.name=region.name;AssetDatabase.CreateAsset(sprite,spritePath);
                }
                var node=Rect("Slot"+attachment.slot,bones[slot.bone]); var v=attachment.values;
                node.localPosition=new Vector3(v[1],v[2],0);node.localEulerAngles=new Vector3(0,0,v[0]);node.localScale=new Vector3(v[3],v[4],1);
                var imageRect=Rect("Image",node);float sx=v[5]/offsets[2],sy=v[6]/offsets[3];
                imageRect.anchoredPosition=new Vector2((offsets[0]+b[2]*.5f-offsets[2]*.5f)*sx,(offsets[1]+b[3]*.5f-offsets[3]*.5f)*sy);
                imageRect.sizeDelta=rotated?new Vector2(b[3]*sy,b[2]*sx):new Vector2(b[2]*sx,b[3]*sy);
                imageRect.localEulerAngles=new Vector3(0,0,rotated?-90:0);
                var image=imageRect.gameObject.AddComponent<Image>();image.sprite=sprite;image.material=slot.blend==1?additive:normal;
                image.color=ColorOf(slot.color)*ColorOf(attachment.color);image.raycastTarget=false;
                image.enabled=slot.attachment==attachment.key;images[attachment.slot]=image;ordering[node]=attachment.slot;
            }
            Order(visual,ordering);
            var baseline=new AnimationClip{name="setup",legacy=true,frameRate=30};
            for(int i=0;i<bones.Length;i++) {
                var v=data.bones[i].values;string path=AnimationUtility.CalculateTransformPath(bones[i],root.transform);
                Constant(baseline,path,typeof(Transform),"localEulerAnglesRaw.z",v[0]);
                Constant(baseline,path,typeof(Transform),"m_LocalPosition.x",v[1]);Constant(baseline,path,typeof(Transform),"m_LocalPosition.y",v[2]);
                Constant(baseline,path,typeof(Transform),"m_LocalScale.x",v[3]);Constant(baseline,path,typeof(Transform),"m_LocalScale.y",v[4]);
            }
            for(int i=0;i<images.Length;i++) if(images[i]!=null) {
                string path=AnimationUtility.CalculateTransformPath(images[i].transform,root.transform);
                Constant(baseline,path,typeof(Image),"m_Enabled",images[i].enabled?1:0);
                for(int c=0;c<4;c++)Constant(baseline,path,typeof(Image),"m_Color."+Rgba[c],images[i].color[c]);
            }
            baseline=SaveAsset(baseline,Folder+"/setup.anim");
            var player=root.GetComponent<Animation>();player.playAutomatically=false;
            foreach(var source in data.animations) {
                if (Array.IndexOf(names,source.name)<0) continue;
                var clip=new AnimationClip{name=source.name,legacy=true,frameRate=30,wrapMode=source.name=="zcjb_idle"?WrapMode.Loop:WrapMode.Once};
                foreach(var t in source.timelines) {
                    bool slot=t.domain=="slot";
                    if (slot && images[t.index] == null) continue;
                    string path=AnimationUtility.CalculateTransformPath(slot?images[t.index].transform:bones[t.index],root.transform);
                    if(slot&&t.kind==0) {
                        var keys=new Keyframe[t.frames.Length];
                        for(int i=0;i<keys.Length;i++)keys[i]=new Keyframe(t.frames[i].time,t.frames[i].attachment==null?0:1,float.PositiveInfinity,float.PositiveInfinity);
                        clip.SetCurve(path,typeof(Image),"m_Enabled",new AnimationCurve(keys));continue;
                    }
                    int dimensions=t.frames[0].values.Length;
                    for(int c=0;c<dimensions;c++) {
                        string property=slot?"m_Color."+Rgba[c]:t.kind==0?"localEulerAnglesRaw.z":(t.kind==4?"m_LocalScale.":"m_LocalPosition.")+(c==0?"x":"y");
                        float offset=slot||t.kind==4?0:data.bones[t.index].values[t.kind==0?0:c+1];
                        float factor=slot?AttachmentColor(data,t.index,c):t.kind==4?data.bones[t.index].values[c+3]:1;
                        clip.SetCurve(path,slot?typeof(Image):typeof(Transform),property,Curve(t.frames,c,offset,factor));
                    }
                }
                clip=SaveAsset(clip,Folder+"/"+source.name+".anim");player.AddClip(clip,source.name);
            }
            var serialized=new SerializedObject(driver);
            serialized.FindProperty("animationPlayer").objectReferenceValue=player;
            serialized.FindProperty("setup").objectReferenceValue=baseline;serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Folder+".prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
    }
    private static RectTransform Rect(string name,Transform parent) {
        var node=new GameObject(name,typeof(RectTransform));node.layer=5;node.transform.SetParent(parent,false);
        var rect=(RectTransform)node.transform;rect.sizeDelta=Vector2.zero;return rect;
    }
    private static Material Material(string name,int destination) {
        string path=Folder+"/"+name+".mat";var value=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(value==null){value=new Material(Shader.Find("DragonLegend/Recovered PMA UI")){name=name};AssetDatabase.CreateAsset(value,path);}
        value.SetFloat("_DestinationBlend",destination);return value;
    }
    private static Color ColorOf(float[] v)=>new Color(v[0],v[1],v[2],v[3]);
    private static float AttachmentColor(Data data,int slot,int c)=>Array.Find(data.attachments,a=>a.slot==slot).color[c];
    private static AnimationClip SaveAsset(AnimationClip clip,string path) {
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing==null){AssetDatabase.CreateAsset(clip,path);return clip;}
        EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);return existing;
    }
    private static void Constant(AnimationClip clip,string path,Type type,string property,float value)
        =>clip.SetCurve(path,type,property,AnimationCurve.Constant(0,0,value));
    private static int Order(Transform parent,Dictionary<Transform,int> slots) {
        int own=slots.TryGetValue(parent,out int index)?index:int.MaxValue;var children=new List<KeyValuePair<Transform,int>>();
        foreach(Transform child in parent){int value=Order(child,slots);children.Add(new KeyValuePair<Transform,int>(child,value));own=Math.Min(own,value);}
        children.Sort((a,b)=>a.Value.CompareTo(b.Value));for(int i=0;i<children.Count;i++)children[i].Key.SetSiblingIndex(i);return own;
    }
    public static AnimationCurve Curve(Frame[] frames,int channel,float offset,float factor) {
        var keys=new List<Keyframe>();
        for(int i=0;i<frames.Length;i++) {
            var f=frames[i];keys.Add(new Keyframe(f.time,offset+f.values[channel]*factor));
            if(i+1==frames.Length)break;
            if(f.curve==2) {
                var b=f.bezier[channel].values;var n=frames[i+1];
                // Spine 4.1 stores nine Bezier sample points; the runtime interpolates between them.
                for(int j=1;j<10;j++) {
                    float t=j*.1f,u=1-t;
                    float x=u*u*u*f.time+3*u*u*t*b[0]+3*u*t*t*b[2]+t*t*t*n.time;
                    float y=u*u*u*f.values[channel]+3*u*u*t*b[1]+3*u*t*t*b[3]+t*t*t*n.values[channel];
                    keys.Add(new Keyframe(x,offset+y*factor));
                }
            }
        }
        var curve=new AnimationCurve(keys.ToArray());
        for(int i=0;i<curve.length;i++) {
            AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
        }
        for(int f=0;f<frames.Length-1;f++) if(frames[f].curve==1) {
            for(int k=0;k<curve.length;k++)if(curve[k].time==frames[f].time)AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.Constant);
        }
        return curve;
    }
}
