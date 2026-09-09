using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;

public sealed class RecoveredBonusWorldTests
{
    [Test]
    public void AllSourceClipsSwitchOnOneMeshWithoutChangingTopology()
    {
        string[] names={"glow","idle_bao","idle_cai","idle_chun","idle_jin","idle_zhao",
            "zcjb_b_bao","zcjb_b_cai","zcjb_b_chun","zcjb_b_jin","zcjb_b_zhao","zcjb_chuxian","zcjb_idle"};
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/BonusWorld/zcjb_idle/zcjb_idle"));
        try
        {
            var rig=root.GetComponent<RecoveredWorldRig>();
            Assert.IsNull(root.GetComponent<CanvasRenderer>());
            Assert.IsNotNull(root.GetComponent<MeshRenderer>());
            var mesh=rig.CurrentMesh;
            foreach(string name in names)
            {
                var data=Resources.Load<RecoveredWorldRigData>("RecoveredSymbols/BonusWorld/"+name+"/RecoveredCoinEffect");
                Assert.IsNotNull(data,name);Assert.AreEqual(29,data.bones.Length);Assert.AreEqual(59,data.attachments.Length);
                rig.SelectClipData(data);
                for(int sample=0;sample<=4;sample++)
                {
                    rig.Sample(data.duration*sample/4);
                    Assert.AreSame(mesh,rig.CurrentMesh,"Clip switches must reuse the instance mesh.");
                    foreach(var vertex in mesh.vertices)
                        Assert.IsTrue(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y)&&float.IsFinite(vertex.z),name);
                    Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);
                    foreach(int index in mesh.triangles)Assert.That(index,Is.InRange(0,mesh.vertexCount-1));
                }
            }
        }
        finally {Object.DestroyImmediate(root);}
    }
}
