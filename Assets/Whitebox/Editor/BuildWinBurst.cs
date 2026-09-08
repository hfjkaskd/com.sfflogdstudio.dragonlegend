using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class BuildWinBurst
{
    const string Folder="Assets/Resources/RecoveredUI/WinBurst";
    public static void Save()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var source=JsonUtility.FromJson<BuildCoinAppearance.Data>(File.ReadAllText("Assets/Whitebox/Editor/RecoveredWinBurst.json"));
        const string atlasPath="Assets/Resources/RecoveredArt/Res/Spine/uixiawin/ef_sluixiawin.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(atlasPath);importer.alphaIsTransparency=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        var root=new GameObject("Boom",typeof(RectTransform),typeof(RecoveredRegionRig),typeof(Animation),typeof(RecoveredWinBurst));
        try {
            root.layer=5;var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(50,50);rect.anchoredPosition=new Vector2(-7,58);
            var rig=root.GetComponent<RecoveredRegionRig>();rig.bones=new RecoveredRegionRig.Bone[source.bones.Length];
            root.GetComponent<CanvasRenderer>().cullTransparentMesh=false;
            for(int i=0;i<source.bones.Length;i++) {
                var b=source.bones[i];if(b.mode!=0&&b.mode!=3)throw new InvalidDataException("Unsupported bone inheritance");
                if(b.values[5]!=0||b.values[6]!=0)throw new InvalidDataException("Unsupported bone shear");
                rig.bones[i]=new RecoveredRegionRig.Bone{name=b.name,parent=b.parent,mode=b.mode,rotation=b.values[0],x=b.values[1],y=b.values[2],scaleX=b.values[3],scaleY=b.values[4]};
            }
            var ids=new Dictionary<string,int>();rig.regions=new RecoveredRegionRig.Region[source.attachments.Length];
            for(int i=0;i<source.attachments.Length;i++) {
                var a=source.attachments[i];ids.Add(a.slot+"/"+a.key,i);var region=Array.Find(source.regions,r=>r.name==a.name);
                var b=region.bounds;var o=region.offsets;var v=a.values;
                float x0=o[0]*v[5]/o[2]-v[5]*.5f,y0=o[1]*v[6]/o[3]-v[6]*.5f;
                float x1=x0+b[2]*v[5]/o[2],y1=y0+b[3]*v[6]/o[3];
                var points=new[]{new Vector2(x0,y0),new Vector2(x0,y1),new Vector2(x1,y1),new Vector2(x1,y0)};
                var transform=Matrix4x4.TRS(new Vector3(v[1],v[2],0),Quaternion.Euler(0,0,v[0]),new Vector3(v[3],v[4],1));
                for(int p=0;p<4;p++)points[p]=transform.MultiplyPoint3x4(points[p]);
                bool rotated=region.rotate==90;if(region.rotate!=0&&!rotated)throw new InvalidDataException("Unsupported atlas rotation");
                float u0=(float)b[0]/atlas.width,u1=(float)(b[0]+(rotated?b[3]:b[2]))/atlas.width;
                float t1=1-(float)b[1]/atlas.height,t0=1-(float)(b[1]+(rotated?b[2]:b[3]))/atlas.height;
                var uv=rotated?new[]{new Vector2(u0,t1),new Vector2(u1,t1),new Vector2(u1,t0),new Vector2(u0,t0)}:new[]{new Vector2(u0,t0),new Vector2(u0,t1),new Vector2(u1,t1),new Vector2(u1,t0)};
                rig.regions[i]=new RecoveredRegionRig.Region{vertices=points,uv=uv,tint=ColorOf(a.color)};
            }
            rig.slots=new RecoveredRegionRig.Slot[source.slots.Length];
            for(int i=0;i<source.slots.Length;i++) {var s=source.slots[i];if(s.blend!=0&&s.blend!=1)throw new InvalidDataException("Unsupported blend");rig.slots[i]=new RecoveredRegionRig.Slot{bone=s.bone,attachment=string.IsNullOrEmpty(s.attachment)?-1:ids[i+"/"+s.attachment],tint=ColorOf(s.color),additive=s.blend==1};}
            var settings=new SerializedObject(rig);settings.FindProperty("atlasPath").stringValue="RecoveredArt/Res/Spine/uixiawin/ef_sluixiawin";settings.ApplyModifiedPropertiesWithoutUndo();
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Pma.mat");if(material==null){material=new Material(Shader.Find("DragonLegend/Recovered PMA Region Rig"));AssetDatabase.CreateAsset(material,Folder+"/Pma.mat");}rig.material=material;
            var setup=new AnimationClip{name="setup",legacy=true,frameRate=30};
            setup.SetCurve("",typeof(RecoveredWinBurst),"poseTime",AnimationCurve.Constant(0,0,0));
            var clip=new AnimationClip{name="animation",legacy=true,frameRate=30,wrapMode=WrapMode.Once};
            clip.SetCurve("",typeof(RecoveredWinBurst),"poseTime",AnimationCurve.Linear(0,0,source.animations[0].duration,source.animations[0].duration));
            var channels=new List<RecoveredRigAnimation.Channel>();
            foreach(var t in source.animations[0].timelines) {
                bool slot=t.domain=="slot";
                if(slot&&t.kind==0) {
                    var keys=new List<Keyframe>();if(t.frames[0].time>0)keys.Add(new Keyframe(0,rig.slots[t.index].attachment,float.PositiveInfinity,float.PositiveInfinity));
                    foreach(var f in t.frames)keys.Add(new Keyframe(f.time,string.IsNullOrEmpty(f.attachment)?-1:ids[t.index+"/"+f.attachment],float.PositiveInfinity,float.PositiveInfinity));
                    channels.Add(new RecoveredRigAnimation.Channel{slot=true,index=t.index,component=-1,curve=new AnimationCurve(keys.ToArray())});continue;
                }
                if(!slot&&t.domain!="bone")throw new InvalidDataException("Unsupported timeline domain");
                for(int c=0;c<t.frames[0].values.Length;c++) {
                    int component=t.kind==0?0:t.kind==1?c+1:c+3;
                    float original=slot?source.slots[t.index].color[c]:source.bones[t.index].values[component];
                    float offset=slot||t.kind==4?0:original,factor=!slot&&t.kind==4?original:1;
                    var curve=BuildCoinAppearance.Curve(t.frames,c,offset,factor);
                    if(t.frames[0].time>0)curve.AddKey(new Keyframe(0,original,float.PositiveInfinity,float.PositiveInfinity));
                    channels.Add(new RecoveredRigAnimation.Channel{slot=slot,index=t.index,component=slot?c:component,curve=curve});
                }
            }
            var poses=AssetDatabase.LoadAssetAtPath<RecoveredRigAnimation>(Folder+"/poses.asset");
            if(poses==null){poses=ScriptableObject.CreateInstance<RecoveredRigAnimation>();AssetDatabase.CreateAsset(poses,Folder+"/poses.asset");}
            poses.channels=channels.ToArray();EditorUtility.SetDirty(poses);
            setup=SaveClip(setup,Folder+"/setup.anim");clip=SaveClip(clip,Folder+"/animation.anim");
            var player=root.GetComponent<Animation>();player.playAutomatically=false;player.AddClip(clip,"animation");
            var driver=new SerializedObject(root.GetComponent<RecoveredWinBurst>());driver.FindProperty("rig").objectReferenceValue=rig;driver.FindProperty("poses").objectReferenceValue=poses;driver.FindProperty("player").objectReferenceValue=player;driver.FindProperty("setup").objectReferenceValue=setup;driver.ApplyModifiedPropertiesWithoutUndo();
            rig.RefreshPose();PrefabUtility.SaveAsPrefabAsset(root,Folder+".prefab");AssetDatabase.SaveAssets();
        } finally {Object.DestroyImmediate(root);}
        BuildSpinPlayfield.Save();
    }
    static Color ColorOf(float[] v)=>new Color(v[0],v[1],v[2],v[3]);
    static AnimationClip SaveClip(AnimationClip clip,string path){var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(old==null){AssetDatabase.CreateAsset(clip,path);return clip;}EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);return old;}
}
