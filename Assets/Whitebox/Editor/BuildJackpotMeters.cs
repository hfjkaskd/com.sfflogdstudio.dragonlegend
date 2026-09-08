using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildJackpotMeters
{
    const string Folder="Assets/Resources/RecoveredUI/JackpotMeters";
    static Dictionary<string,string> blocks;
    public static void Save()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var text=File.ReadAllText("C:/Projects/Nut Sort Relax/reconstruction/mumu-current/reference-unity/ExportedProject/Assets/Res/ViewPrefabs/UIMainView.prefab").Replace("\r","");
        blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(text,@"--- !u!\d+ &(\d+)\n(.*?)(?=\n--- !u!|\z)",RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Groups[2].Value);
        var root=Rect("JackPot",null,"224335036306139620").gameObject;
        var group=root.AddComponent<RecoveredJackpotMeters>();var meters=new RecoveredJackpotMeter[3];
        try {
            var roots=new[]{"224408501777709171","224008770253507523","224717369901526261"};
            var icons=new[]{"224274088443893264","224045549052472940","224070205081729383"};
            var labels=new[]{"224079961849258352","224391381606989912","224075734428016886"};
            var tmps=new[]{"224222115391670268","224809288501262078","224191838273672616"};
            var names=new[]{"grand","major","minor"};
            for(int i=0;i<3;i++) {
                var meter=Rect(i==0?"Grand":i==1?"Major":"Minior",root.transform,roots[i]).gameObject.AddComponent<RecoveredJackpotMeter>();meters[i]=meter;
                var node=Rect("Spine",meter.transform,icons[i]).gameObject;
                var source=JsonUtility.FromJson<BuildCoinAppearance.Data>(File.ReadAllText("Tools/Evidence/Jackpot/ef_"+names[i]+"icon.json"));
                var icon=CreateIcon(node,source,names[i]);
                var tmp=Rect("Text (TMP)",meter.transform,tmps[i]).gameObject.AddComponent<TextMeshProUGUI>();
                tmp.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("31628181d58311344bb283c127fc9aba"));
                tmp.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("71fce5925b6d578448b06d587acf8e25"));
                tmp.fontSize=i==0?60:48;tmp.enableAutoSizing=false;tmp.fontSizeMin=18;tmp.fontSizeMax=i==0?60:42;
                tmp.alignment=TextAlignmentOptions.Center;tmp.enableWordWrapping=false;tmp.overflowMode=TextOverflowModes.Overflow;
                tmp.enableVertexGradient=true;var top=new Color(.54509807f,1,.2784314f,1);var bottom=new Color(.2627451f,.7882353f,.2627451f,1);
                tmp.colorGradient=new VertexGradient(top,top,bottom,bottom);tmp.text="$ 1,789.00";
                var label=Rect("Text (Legacy)",meter.transform,labels[i]).gameObject.AddComponent<Text>();
                label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
                label.fontSize=0;label.resizeTextForBestFit=false;label.resizeTextMinSize=0;label.resizeTextMaxSize=60;label.alignment=TextAnchor.MiddleCenter;
                label.alignByGeometry=false;label.supportRichText=true;label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;label.text="123";
                var settings=new SerializedObject(meter);settings.FindProperty("icon").objectReferenceValue=icon;settings.FindProperty("rewardText").objectReferenceValue=tmp;
                settings.FindProperty("greenRewardText").objectReferenceValue=label;settings.FindProperty("rewardDuration").floatValue=.3f;settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var groupSettings=new SerializedObject(group);var array=groupSettings.FindProperty("meters");array.arraySize=3;
            for(int i=0;i<3;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=meters[i];groupSettings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Folder+".prefab");AssetDatabase.SaveAssets();
        }finally{Object.DestroyImmediate(root);}
        BuildSpinPlayfield.Save();
    }
    static RecoveredJackpotIcon CreateIcon(GameObject node,BuildCoinAppearance.Data source,string tier)
    {
        string folder=Folder+"/"+tier;Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        string atlasPath="RecoveredArt/Res/Spine/jackpot3小个/"+tier+"_icon/ef_"+tier+"icon";
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/"+atlasPath+".png");
        importer.alphaIsTransparency=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/"+atlasPath+".png");
        var rig=node.AddComponent<RecoveredRegionRig>();node.GetComponent<CanvasRenderer>().cullTransparentMesh=false;
        rig.raycastTarget=false;rig.bones=new RecoveredRegionRig.Bone[source.bones.Length];
        for(int i=0;i<source.bones.Length;i++) {
            var b=source.bones[i];if(b.mode!=0&&b.mode!=1)throw new InvalidDataException("Unexpected jackpot bone inheritance");
            rig.bones[i]=new RecoveredRegionRig.Bone{name=b.name,parent=b.parent,mode=b.mode,rotation=b.values[0],x=b.values[1],y=b.values[2],scaleX=b.values[3],scaleY=b.values[4]};
        }
        var ids=new Dictionary<string,int>();var regions=new List<RecoveredRegionRig.Region>();
        foreach(var attachment in source.attachments) {
            if(attachment.kind!=0)throw new InvalidDataException("Unexpected jackpot attachment");
            ids.Add(attachment.slot+"/"+attachment.key,regions.Count);regions.Add(null);
        }
        for(int i=0;i<source.attachments.Length;i++) {
            var a=source.attachments[i];string path=string.IsNullOrEmpty(a.path)?a.name:a.path;
            // JsonUtility creates an empty nested record for an absent JSON field.
            if(a.sequence==null||a.sequence.count==0)regions[i]=Region(source,a,path,atlas);
            else {
                var sequence=a.sequence;var indices=new int[sequence.count];
                for(int f=0;f<indices.Length;f++) {
                    indices[f]=regions.Count;string frame=(sequence.start+f).ToString(CultureInfo.InvariantCulture).PadLeft(sequence.digits,'0');
                    regions.Add(Region(source,a,path+frame,atlas));
                }
                regions[i]=new RecoveredRegionRig.Region{sequenceFrames=indices,setupIndex=sequence.setupIndex};
            }
        }
        foreach(var region in regions)
            if((region.sequenceFrames==null||region.sequenceFrames.Length==0)&&(region.vertices==null||region.vertices.Length!=4||region.uv==null||region.uv.Length!=4))
                throw new InvalidDataException("Incomplete jackpot region");
        rig.regions=regions.ToArray();rig.slots=new RecoveredRegionRig.Slot[source.slots.Length];
        for(int i=0;i<source.slots.Length;i++) {
            var s=source.slots[i];if(s.blend!=0&&s.blend!=1)throw new InvalidDataException("Unexpected jackpot blend");
            rig.slots[i]=new RecoveredRegionRig.Slot{bone=s.bone,attachment=string.IsNullOrEmpty(s.attachment)?-1:ids[i+"/"+s.attachment],tint=ColorOf(s.color),additive=s.blend==1};
        }
        var settings=new SerializedObject(rig);settings.FindProperty("atlasPath").stringValue=atlasPath;settings.ApplyModifiedPropertiesWithoutUndo();
        rig.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/WinBurst/Pma.mat");
        var player=node.AddComponent<Animation>();var icon=node.AddComponent<RecoveredJackpotIcon>();
        settings=new SerializedObject(icon);settings.FindProperty("rig").objectReferenceValue=rig;settings.FindProperty("player").objectReferenceValue=player;
        foreach(var animation in source.animations) {
            var poses=Poses(source,animation,ids,rig);string asset=folder+"/"+animation.name+".asset";
            var previous=AssetDatabase.LoadAssetAtPath<RecoveredRigAnimation>(asset);
            if(previous==null)AssetDatabase.CreateAsset(poses,asset);else{EditorUtility.CopySerialized(poses,previous);Object.DestroyImmediate(poses);poses=previous;}
            settings.FindProperty(animation.name).objectReferenceValue=poses;
            var clip=new AnimationClip{name=animation.name,legacy=true,frameRate=30,wrapMode=animation.name=="idle"?WrapMode.Loop:WrapMode.Once};
            clip.SetCurve("",typeof(RecoveredJackpotIcon),"poseTime",AnimationCurve.Linear(0,0,animation.duration,animation.duration));
            string path=folder+"/"+animation.name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(old==null)AssetDatabase.CreateAsset(clip,path);else{EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);clip=old;}
            player.AddClip(clip,animation.name);if(animation.name=="idle")player.clip=clip;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();player.playAutomatically=true;rig.RefreshPose();return icon;
    }
    static RecoveredRigAnimation Poses(BuildCoinAppearance.Data source,BuildCoinAppearance.Clip animation,Dictionary<string,int> ids,RecoveredRegionRig rig)
    {
        var channels=new List<RecoveredRigAnimation.Channel>();var sequences=new List<RecoveredRigAnimation.SequenceChannel>();
        foreach(var t in animation.timelines) {
            if(t.domain=="sequence") {
                var a=Array.Find(source.attachments,x=>x.slot==t.index&&x.key==t.attachment);
                var frames=new RecoveredRigAnimation.SequenceFrame[t.frames.Length];
                for(int i=0;i<frames.Length;i++){var f=t.frames[i];frames[i]=new RecoveredRigAnimation.SequenceFrame{time=f.time,delay=f.delay,index=f.index,mode=f.mode};}
                sequences.Add(new RecoveredRigAnimation.SequenceChannel{slot=t.index,attachment=ids[t.index+"/"+t.attachment],count=a.sequence.count,frames=frames});continue;
            }
            bool slot=t.domain=="slot";
            if(slot&&t.kind==0) {
                var keys=new List<Keyframe>();if(t.frames[0].time>0)keys.Add(new Keyframe(0,rig.slots[t.index].attachment,float.PositiveInfinity,float.PositiveInfinity));
                foreach(var f in t.frames)keys.Add(new Keyframe(f.time,string.IsNullOrEmpty(f.attachment)?-1:ids[t.index+"/"+f.attachment],float.PositiveInfinity,float.PositiveInfinity));
                channels.Add(new RecoveredRigAnimation.Channel{slot=true,index=t.index,component=-1,curve=new AnimationCurve(keys.ToArray())});continue;
            }
            if(slot?t.kind!=1:t.domain!="bone"||(t.kind!=1&&t.kind!=4))throw new InvalidDataException("Unexpected jackpot timeline");
            for(int c=0;c<t.frames[0].values.Length;c++) {
                int component=slot?c:t.kind==1?c+1:c+3;float original=slot?source.slots[t.index].color[c]:source.bones[t.index].values[component];
                var curve=BuildCoinAppearance.Curve(t.frames,c,slot||t.kind==4?0:original,!slot&&t.kind==4?original:1);
                if(t.frames[0].time>0)curve.AddKey(new Keyframe(0,original,float.PositiveInfinity,float.PositiveInfinity));
                channels.Add(new RecoveredRigAnimation.Channel{slot=slot,index=t.index,component=component,curve=curve});
            }
        }
        var poses=ScriptableObject.CreateInstance<RecoveredRigAnimation>();poses.channels=channels.ToArray();poses.sequences=sequences.ToArray();return poses;
    }
    static RecoveredRegionRig.Region Region(BuildCoinAppearance.Data source,BuildCoinAppearance.Attachment a,string path,Texture2D atlas)
    {
        var region=Array.Find(source.regions,r=>r.name==path);if(region==null)throw new InvalidDataException("Missing atlas frame "+path);
        var b=region.bounds;var o=region.offsets;var v=a.values;
        float x=o[0]*v[5]/o[2]-v[5]*.5f,y=o[1]*v[6]/o[3]-v[6]*.5f;
        var points=new[]{new Vector2(x,y),new Vector2(x,y+b[3]*v[6]/o[3]),new Vector2(x+b[2]*v[5]/o[2],y+b[3]*v[6]/o[3]),new Vector2(x+b[2]*v[5]/o[2],y)};
        var transform=Matrix4x4.TRS(new Vector3(v[1],v[2],0),Quaternion.Euler(0,0,v[0]),new Vector3(v[3],v[4],1));for(int i=0;i<4;i++)points[i]=transform.MultiplyPoint3x4(points[i]);
        bool rotated=region.rotate==90;if(region.rotate!=0&&!rotated)throw new InvalidDataException("Unexpected rotation");
        float u0=(float)b[0]/atlas.width,u1=(float)(b[0]+(rotated?b[3]:b[2]))/atlas.width,t1=1-(float)b[1]/atlas.height,t0=1-(float)(b[1]+(rotated?b[2]:b[3]))/atlas.height;
        var uv=rotated?new[]{new Vector2(u1,t0),new Vector2(u0,t0),new Vector2(u0,t1),new Vector2(u1,t1)}:new[]{new Vector2(u0,t0),new Vector2(u0,t1),new Vector2(u1,t1),new Vector2(u1,t0)};
        return new RecoveredRegionRig.Region{vertices=points,uv=uv,tint=ColorOf(a.color)};
    }
    static Color ColorOf(float[] values)=>new Color(values[0],values[1],values[2],values[3]);
    static RectTransform Rect(string name,Transform parent,string id)
    {
        string data=blocks[id];var root=new GameObject(name,typeof(RectTransform));root.layer=5;root.transform.SetParent(parent,false);var rect=(RectTransform)root.transform;
        rect.anchorMin=Vector(data,"m_AnchorMin");rect.anchorMax=Vector(data,"m_AnchorMax");rect.pivot=Vector(data,"m_Pivot");rect.sizeDelta=Vector(data,"m_SizeDelta");rect.anchoredPosition=Vector(data,"m_AnchoredPosition");
        var scale=Vector(data,"m_LocalScale");rect.localScale=new Vector3(scale.x,scale.y,scale.x);return rect;
    }
    static Vector2 Vector(string data,string key)
    {
        var match=Regex.Match(data,key+@": \{x: ([^,]+), y: ([^,}]+)");return new Vector2(float.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture),float.Parse(match.Groups[2].Value,CultureInfo.InvariantCulture));
    }
}
