using System;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;

public sealed class RecoveredNpcConstraintTests
{
    [Serializable] private class Reference
    {
        public int[] parents,modes;
        public RecoveredNpcConstraints program;
        public Frame[] frames;
    }
    [Serializable] private class Frame {public string clip;public float time;public float[] pose,matrices;}
    [Test]
    public void AllNpcClipsMatchOrderedSourceBoneMatricesWithoutAllocations()
    {
        var source=JsonUtility.FromJson<Reference>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/npc-constraint-poses.json")));
        var bones=new RecoveredRegionRig.Bone[source.parents.Length];var matrices=new Matrix4x4[bones.Length];
        for(int i=0;i<bones.Length;i++)bones[i]=new RecoveredRegionRig.Bone{parent=source.parents[i],mode=source.modes[i]};
        Assert.AreEqual(156,bones.Length);Assert.AreEqual(166,source.program.steps.Length);Assert.AreEqual(32,source.frames.Length);
        foreach(var frame in source.frames) {
            for(int i=0;i<bones.Length;i++) {
                var b=bones[i];int p=i*5;b.rotation=frame.pose[p];b.x=frame.pose[p+1];b.y=frame.pose[p+2];b.scaleX=frame.pose[p+3];b.scaleY=frame.pose[p+4];
            }
            source.program.Evaluate(bones,matrices);
            for(int i=0;i<bones.Length;i++) {
                int p=i*6;var m=matrices[i];string context=$"{frame.clip} {frame.time} bone {i}";
                Assert.AreEqual(frame.matrices[p],m.m00,.003f,context+" a");
                Assert.AreEqual(frame.matrices[p+1],m.m01,.003f,context+" b");
                Assert.AreEqual(frame.matrices[p+2],m.m10,.003f,context+" c");
                Assert.AreEqual(frame.matrices[p+3],m.m11,.003f,context+" d");
                Assert.AreEqual(frame.matrices[p+4],m.m03,.003f,context+" x");
                Assert.AreEqual(frame.matrices[p+5],m.m13,.003f,context+" y");
                Assert.AreEqual(frame.pose[i*5+1],bones[i].x,"Constraints must not mutate animation input");
            }
        }
        for(int i=0;i<30;i++)source.program.Evaluate(bones,matrices);
        long before=GC.GetAllocatedBytesForCurrentThread(),start=System.Diagnostics.Stopwatch.GetTimestamp();
        for(int i=0;i<1000;i++)source.program.Evaluate(bones,matrices);
        long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        Assert.AreEqual(0,bytes);TestContext.WriteLine($"1000 NPC constraint poses: {ms:F2} ms; {bytes} allocated bytes");
    }
    [Test]
    public void AbsoluteLocalScaleKeepsNativeDivisionAndZeroGuard()
    {
        var bones=new[]{new RecoveredRegionRig.Bone{parent=-1},
            new RecoveredRegionRig.Bone{parent=0,scaleX=1,scaleY=2},
            new RecoveredRegionRig.Bone{parent=0,scaleX=2,scaleY=0}};
        var program=new RecoveredNpcConstraints{steps=new[]{0,1,2,-1},constraints=new[]{
            new RecoveredNpcConstraints.Constraint{local=true,bone=2,target=1,scaleMix=new Vector2(-.5f,-.5f)}
        }};
        var matrices=new Matrix4x4[3];program.Evaluate(bones,matrices);
        Assert.AreEqual(1.25f,matrices[2].m00);Assert.AreEqual(0,matrices[2].m11);
        Assert.AreEqual(2,bones[2].scaleX);Assert.AreEqual(0,bones[2].scaleY);
    }
}
