using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class BuildWildWorld
{
    [Serializable] private class Source : BuildCoinAppearance.Data { public new Attachment[] attachments; public new Clip[] animations; }
    [Serializable] private class Attachment : BuildCoinAppearance.Attachment {
        public int kind, endSlot; public bool weighted; public string path;
        public float[] vertices, uvs; public int[] counts, triangles; public Influence[] influences;
    }
    [Serializable] private class Influence { public int bone; public float x, y, weight; }
    [Serializable] private class Clip { public string name; public float duration; public Timeline[] timelines; }
    [Serializable] private class Timeline : BuildCoinAppearance.Timeline { public string attachment; }
    const string Folder = "Assets/Resources/RecoveredSymbols/Wild3";

    public static void Save()
    {
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var root = new GameObject("Wild3", typeof(SortingGroup)); root.layer = 5;
        try {
            Create(root.transform, "Wild", "ef_wild3", "wild3");
            var win = new GameObject("Win3"); win.layer = 5; win.transform.SetParent(root.transform, false);
            Create(win.transform, "Glow", "ef_slwin3", "slwin3");
            PrefabUtility.SaveAsPrefabAsset(root, Folder + ".prefab"); AssetDatabase.SaveAssets();
        } finally { Object.DestroyImmediate(root); }
    }
    public static void SaveLight()
    {
        const string folder="Assets/Resources/RecoveredSymbols/Wild3Light";
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var root=Create(null,"Wild3Light","ef_wild1_3","1_3",folder,false);
        try {
            var driver=new SerializedObject(root.AddComponent<RecoveredWildLight>());
            driver.FindProperty("player").objectReferenceValue=root.GetComponent<Animation>();
            driver.FindProperty("rig").objectReferenceValue=root.GetComponent<RecoveredWorldRig>();
            driver.FindProperty("clipName").stringValue="wild3";driver.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,folder+".prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
    }
    static GameObject Create(Transform parent, string name, string file, string directory,string folder=Folder,bool loop=true)
    {
        var source = JsonUtility.FromJson<Source>(File.ReadAllText("Artifacts/WildAuthoring/" + file + ".json"));
        var data = ScriptableObject.CreateInstance<RecoveredWorldRigData>(); data.name = file;
        data.atlasPath = "RecoveredArt/Res/Spine/棋子/" + directory + "/" + file; data.pixelsPerUnit = 100;
        string texturePath = "Assets/Resources/" + data.atlasPath + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.alphaIsTransparency = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        data.bones = new RecoveredRegionRig.Bone[source.bones.Length];
        for (int i = 0; i < data.bones.Length; i++) {
            var b = source.bones[i]; var v = b.values;
            if (b.mode == 2 || b.mode < 0 || b.mode > 4 || v[5] != 0 || v[6] != 0) throw new InvalidDataException("Unconverted bone");
            data.bones[i] = new RecoveredRegionRig.Bone { name=b.name, parent=b.parent, mode=b.mode, rotation=v[0], x=v[1], y=v[2], scaleX=v[3], scaleY=v[4] };
        }
        var ids = new Dictionary<string, int>(); data.attachments = new RecoveredWorldRigData.Attachment[source.attachments.Length];
        for (int i = 0; i < data.attachments.Length; i++) {
            var a = source.attachments[i]; ids.Add(a.slot + "/" + a.key, i);
            var region = Array.Find(source.regions, r => r.name == (string.IsNullOrEmpty(a.path) ? a.name : a.path));
            var result = new RecoveredWorldRigData.Attachment { name=a.key, slot=a.slot, tint=ColorOf(a.color), counts=Array.Empty<int>(), boneIndices=Array.Empty<int>(), weights=Array.Empty<float>() };
            if (a.kind == 0) {
                var b = region.bounds; var o = region.offsets; var v = a.values;
                float left=o[0]*v[5]/o[2]-v[5]/2, bottom=o[1]*v[6]/o[3]-v[6]/2;
                float right=left+b[2]*v[5]/o[2], top=bottom+b[3]*v[6]/o[3];
                result.positions = new[] { new Vector2(left,bottom),new Vector2(left,top),new Vector2(right,top),new Vector2(right,bottom) };
                var transform = Matrix4x4.TRS(new Vector3(v[1],v[2],0),Quaternion.Euler(0,0,v[0]),new Vector3(v[3],v[4],1));
                for (int p=0;p<4;p++) result.positions[p]=transform.MultiplyPoint3x4(result.positions[p]);
                bool rotated=region.rotate==90; if (!rotated && region.rotate!=0) throw new InvalidDataException("Atlas rotation");
                float u0=(float)b[0]/texture.width,u1=(float)(b[0]+(rotated?b[3]:b[2]))/texture.width;
                float y1=1-(float)b[1]/texture.height,y0=1-(float)(b[1]+(rotated?b[2]:b[3]))/texture.height;
                result.uv = rotated ? new[] {new Vector2(u1,y0),new Vector2(u0,y0),new Vector2(u0,y1),new Vector2(u1,y1)}
                    : new[] {new Vector2(u0,y0),new Vector2(u0,y1),new Vector2(u1,y1),new Vector2(u1,y0)};
                result.triangles = new[] {0,1,2,2,3,0};
            } else if (a.kind == 2) {
                result.triangles = a.triangles;
                result.uv = new Vector2[a.uvs.Length/2];
                for (int p=0;p<result.uv.Length;p++) result.uv[p]=MeshUv(region,new Vector2(a.uvs[p*2],a.uvs[p*2+1]),texture.width,texture.height);
                if (a.weighted) {
                    result.counts=a.counts; result.positions=new Vector2[a.influences.Length];
                    result.boneIndices=new int[a.influences.Length]; result.weights=new float[a.influences.Length];
                    for (int p=0;p<a.influences.Length;p++) {var w=a.influences[p];result.positions[p]=new Vector2(w.x,w.y);result.boneIndices[p]=w.bone;result.weights[p]=w.weight;}
                } else {
                    result.positions=new Vector2[a.vertices.Length/2];
                    for (int p=0;p<result.positions.Length;p++) result.positions[p]=new Vector2(a.vertices[p*2],a.vertices[p*2+1]);
                }
            } else if(a.kind==6) {
                if(a.weighted||a.vertices.Length!=8)throw new InvalidDataException("This light requires an unweighted convex quad");
                result.clipping=true;result.endSlot=a.endSlot;
                result.uv=Array.Empty<Vector2>();result.triangles=Array.Empty<int>();result.positions=new Vector2[4];
                for(int p=0;p<4;p++)result.positions[p]=new Vector2(a.vertices[p*2],a.vertices[p*2+1]);
                ValidateClip(a.vertices);
            } else throw new InvalidDataException("Attachment needs an explicit native conversion");
            data.attachments[i]=result;
        }
        data.slots=new RecoveredWorldRigData.Slot[source.slots.Length];
        for (int i=0;i<data.slots.Length;i++) {
            var s=source.slots[i];if(s.blend!=0&&s.blend!=1)throw new InvalidDataException("Unsupported slot blending");
            data.slots[i]=new RecoveredWorldRigData.Slot {bone=s.bone,attachment=string.IsNullOrEmpty(s.attachment)?-1:ids[i+"/"+s.attachment],tint=ColorOf(s.color),additive=s.blend==1};
        }
        var animation=source.animations[0]; data.duration=animation.duration;
        var channels=new List<RecoveredRigAnimation.Channel>();
        foreach (var t in animation.timelines) {
            if (t.domain=="deform") {
                var attachment=data.attachments[ids[t.index+"/"+t.attachment]];
                attachment.deform=new RecoveredWorldRigData.DeformFrame[t.frames.Length];
                for(int f=0;f<t.frames.Length;f++) {
                    var frame=t.frames[f];if(attachment.clipping)ValidateClip(frame.values);
                    var converted=new RecoveredWorldRigData.DeformFrame{time=frame.time,values=frame.values};
                    if(f+1<t.frames.Length)converted.progress=BuildCoinAppearance.Curve(new[] {
                        new BuildCoinAppearance.Frame{time=frame.time,values=new[]{0f},curve=frame.curve,bezier=frame.bezier},
                        new BuildCoinAppearance.Frame{time=t.frames[f+1].time,values=new[]{1f}}},0,0,1);
                    attachment.deform[f]=converted;
                }
                continue;
            }
            bool slot=t.domain=="slot";if(!slot&&t.domain!="bone")throw new InvalidDataException("Unknown timeline");
            if(slot&&t.kind==0) {
                var keys=new List<Keyframe>();if(t.frames[0].time>0)keys.Add(new Keyframe(0,data.slots[t.index].attachment,float.PositiveInfinity,float.PositiveInfinity));
                foreach(var frame in t.frames)keys.Add(new Keyframe(frame.time,string.IsNullOrEmpty(frame.attachment)?-1:ids[t.index+"/"+frame.attachment],float.PositiveInfinity,float.PositiveInfinity));
                channels.Add(new RecoveredRigAnimation.Channel{slot=true,index=t.index,component=-1,curve=new AnimationCurve(keys.ToArray())});continue;
            }
            if (slot ? t.kind!=1 : t.kind!=0&&t.kind!=1&&t.kind!=4) throw new InvalidDataException("Unconverted timeline kind");
            for(int c=0;c<t.frames[0].values.Length;c++) {
                int component=slot?c:t.kind==0?0:t.kind==1?c+1:c+3;
                float original=slot?source.slots[t.index].color[c]:source.bones[t.index].values[component];
                var curve=BuildCoinAppearance.Curve(t.frames,c,slot||t.kind==4?0:original,!slot&&t.kind==4?original:1);
                if(t.frames[0].time>0)curve.AddKey(new Keyframe(0,original,float.PositiveInfinity,float.PositiveInfinity));
                channels.Add(new RecoveredRigAnimation.Channel{slot=slot,index=t.index,component=component,curve=curve});
            }
        }
        data.channels=channels.ToArray(); data=SaveAsset(data,folder+"/"+file+".asset");
        var node=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(RecoveredWorldRig),typeof(Animation));node.layer=5;node.transform.SetParent(parent,false);
        var settings=new SerializedObject(node.GetComponent<RecoveredWorldRig>());settings.FindProperty("dataPath").stringValue=folder.Substring("Assets/Resources/".Length)+"/"+file;settings.ApplyModifiedPropertiesWithoutUndo();
        var material=new Material(Shader.Find("DragonLegend/Recovered PMA World Rig"));material=SaveAsset(material,folder+"/"+file+".mat");
        var renderer=node.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.sortingOrder=file=="ef_wild3"?0:1;
        var clock=new AnimationClip{name=animation.name,legacy=true,frameRate=30,wrapMode=loop?WrapMode.Loop:WrapMode.Once};
        clock.SetCurve("",typeof(RecoveredWorldRig),"poseTime",AnimationCurve.Linear(0,0,data.duration,data.duration));
        clock=SaveAsset(clock,folder+"/"+(loop?file:animation.name)+".anim");var player=node.GetComponent<Animation>();player.AddClip(clock,animation.name);player.clip=clock;player.playAutomatically=loop;
        return node;
    }
    static void ValidateClip(float[] vertices)
    {
        if(vertices.Length!=8)throw new InvalidDataException("Clip shape changed");
        float winding=0;
        for(int i=0;i<4;i++) {
            int a=i*2,b=((i+1)%4)*2,c=((i+2)%4)*2;
            float cross=(vertices[b]-vertices[a])*(vertices[c+1]-vertices[b+1])-(vertices[b+1]-vertices[a+1])*(vertices[c]-vertices[b]);
            if(cross==0||winding*cross<0)throw new InvalidDataException("Non-convex clipping requires polygon decomposition");
            winding=cross;
        }
    }
    public static Vector2 MeshUv(BuildCoinAppearance.Region region,Vector2 uv,float width,float height)
    {
        var b=region.bounds;var o=region.offsets;
        if(region.rotate==0)return new Vector2((b[0]-o[0]+uv.x*o[2])/width,1-(b[1]-(o[3]-o[1]-b[3])+uv.y*o[3])/height);
        if(region.rotate==90)return new Vector2((b[0]-(o[3]-o[1]-b[3])+uv.y*o[3])/width,1-(b[1]-(o[2]-o[0]-b[2])+(1-uv.x)*o[2])/height);
        throw new InvalidDataException("Unconverted mesh atlas rotation");
    }
    static Color ColorOf(float[] v)=>new Color(v[0],v[1],v[2],v[3]);
    static T SaveAsset<T>(T value,string path) where T:Object {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old==null){AssetDatabase.CreateAsset(value,path);return value;}
        EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);return old;
    }
}
