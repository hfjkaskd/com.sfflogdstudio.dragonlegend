using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BuildJackpotPopupArt
{
    const string Folder="Assets/Resources/RecoveredUI/JackpotPopupArt";
    [Serializable] private class Meshes {public MeshData[] meshes;}
    [Serializable] private class MeshData {public int slot;public string key;public float[] vertices,uvs,weights;public int[] triangles,counts,boneIndices;}
    public static void Save()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        Create("ef_jackpottc","jackpottc",new Vector2(1987.7649f,1634.3843f),new Vector2(.4888889f,.35840994f));
        Create("ef_slpenqian","penqian",Vector2.zero,new Vector2(.5f,.5f));
        Create("ef_shoucanggl","shoucanggl",new Vector2(50,50),new Vector2(.5f,.5f));
        AssetDatabase.SaveAssets();
    }
    static void Create(string name,string sourceFolder,Vector2 size,Vector2 pivot)
    {
        string folder=Folder+"/"+name;Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var source=JsonUtility.FromJson<BuildCoinAppearance.Data>(File.ReadAllText("Tools/Evidence/JackpotPopup/"+name+".json"));
        var meshes=JsonUtility.FromJson<Meshes>(File.ReadAllText("Artifacts/JackpotPopupAuthoring/"+name+".json"));
        string resource="RecoveredArt/Res/Spine/"+sourceFolder+"/"+name;
        string image="Assets/Resources/"+resource+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(image);
        importer.alphaIsTransparency=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(image);
        var root=new GameObject("SkeletonGraphic ("+name+")",typeof(RectTransform),typeof(RecoveredRegionRig),typeof(Animation),typeof(RecoveredRegionAnimator));
        try {
            root.layer=5;var rect=(RectTransform)root.transform;rect.sizeDelta=size;rect.pivot=pivot;
            var rig=root.GetComponent<RecoveredRegionRig>();rig.raycastTarget=false;rig.canvasRenderer.cullTransparentMesh=false;
            rig.bones=new RecoveredRegionRig.Bone[source.bones.Length];
            for(int i=0;i<source.bones.Length;i++) {
                var b=source.bones[i];if(b.mode==2||b.values[5]!=0||b.values[6]!=0)throw new InvalidDataException("Unconverted popup bone");
                rig.bones[i]=new RecoveredRegionRig.Bone{name=b.name,parent=b.parent,mode=b.mode,rotation=b.values[0],x=b.values[1],y=b.values[2],scaleX=b.values[3],scaleY=b.values[4]};
            }
            var ids=new Dictionary<string,int>();var regions=new List<RecoveredRegionRig.Region>();
            for(int i=0;i<source.attachments.Length;i++){var a=source.attachments[i];ids.Add(a.slot+"/"+a.key,i);regions.Add(null);}
            for(int i=0;i<source.attachments.Length;i++) {
                var a=source.attachments[i];string path=string.IsNullOrEmpty(a.path)?a.name:a.path;
                if(a.kind==0) {
                    if(a.sequence==null||a.sequence.count==0)regions[i]=BuildJackpotMeters.Region(source,a,path,atlas);
                    else {
                        var frames=new int[a.sequence.count];
                        for(int f=0;f<frames.Length;f++) {
                            frames[f]=regions.Count;string suffix=(a.sequence.start+f).ToString(CultureInfo.InvariantCulture).PadLeft(a.sequence.digits,'0');
                            regions.Add(BuildJackpotMeters.Region(source,a,path+suffix,atlas));
                        }
                        regions[i]=new RecoveredRegionRig.Region{sequenceFrames=frames,setupIndex=a.sequence.setupIndex};
                    }
                } else if(a.kind==2) {
                    var mesh=Array.Find(meshes.meshes,m=>m.slot==a.slot&&m.key==a.key);if(mesh==null)throw new InvalidDataException("Missing popup mesh");
                    var region=Array.Find(source.regions,r=>r.name==path);var positions=new Vector2[mesh.vertices.Length/2];var uv=new Vector2[mesh.uvs.Length/2];
                    for(int p=0;p<positions.Length;p++)positions[p]=new Vector2(mesh.vertices[p*2],mesh.vertices[p*2+1]);
                    for(int p=0;p<uv.Length;p++)uv[p]=BuildWildWorld.MeshUv(region,new Vector2(mesh.uvs[p*2],mesh.uvs[p*2+1]),atlas.width,atlas.height);
                    regions[i]=new RecoveredRegionRig.Region{vertices=positions,uv=uv,triangles=mesh.triangles,counts=mesh.counts,boneIndices=mesh.boneIndices,weights=mesh.weights,tint=ColorOf(a.color)};
                } else throw new InvalidDataException("Unconverted popup attachment");
            }
            rig.regions=regions.ToArray();rig.slots=new RecoveredRegionRig.Slot[source.slots.Length];
            for(int i=0;i<source.slots.Length;i++) {
                var s=source.slots[i];if(s.blend!=0&&s.blend!=1)throw new InvalidDataException("Unconverted popup blend");
                rig.slots[i]=new RecoveredRegionRig.Slot{bone=s.bone,attachment=string.IsNullOrEmpty(s.attachment)?-1:ids[i+"/"+s.attachment],tint=ColorOf(s.color),additive=s.blend==1};
            }
            var settings=new SerializedObject(rig);settings.FindProperty("atlasPath").stringValue=resource;settings.ApplyModifiedPropertiesWithoutUndo();
            rig.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RecoveredUI/WinBurst/Pma.mat");
            var player=root.GetComponent<Animation>();var animator=root.GetComponent<RecoveredRegionAnimator>();settings=new SerializedObject(animator);
            settings.FindProperty("player").objectReferenceValue=player;settings.FindProperty("rig").objectReferenceValue=rig;
            var array=settings.FindProperty("poses");array.arraySize=source.animations.Length;
            for(int i=0;i<source.animations.Length;i++) {
                var animation=source.animations[i];var poses=BuildJackpotMeters.Poses(source,animation,ids,rig);
                poses=Save(poses,folder+"/"+animation.name+".asset");var entry=array.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("clip").stringValue=animation.name;entry.FindPropertyRelative("animation").objectReferenceValue=poses;
                var clip=new AnimationClip{name=animation.name,legacy=true,frameRate=30,wrapMode=WrapMode.Loop};
                clip.SetCurve("",typeof(RecoveredRegionAnimator),"poseTime",AnimationCurve.Linear(0,0,animation.duration,animation.duration));
                clip=Save(clip,folder+"/"+animation.name+".anim");player.AddClip(clip,animation.name);if(i==0)player.clip=clip;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();player.playAutomatically=true;rig.RefreshPose();
            PrefabUtility.SaveAsPrefabAsset(root,folder+".prefab");
        }finally{Object.DestroyImmediate(root);}
    }
    static Color ColorOf(float[] values)=>new Color(values[0],values[1],values[2],values[3]);
    static T Save<T>(T value,string path) where T:Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old==null){AssetDatabase.CreateAsset(value,path);return value;}
        EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);return old;
    }
}
