using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
public sealed class RecoveredWinBurstTests
{
    [Test]
    public void NoScaleRetainsReflectionAndTranslatedPosition()
    {
        var root=new GameObject("Rig",typeof(RectTransform),typeof(RecoveredRegionRig));var rig=root.GetComponent<RecoveredRegionRig>();
        try {
            rig.bones=new[]{new RecoveredRegionRig.Bone{parent=-1,x=10,y=20,scaleX=2,scaleY=.5f},new RecoveredRegionRig.Bone{parent=0,mode=3,x=3,y=4,rotation=45,scaleX=.4f,scaleY=.7f}};
            rig.RefreshPose();var matrix=rig.BoneMatrix(1);
            Assert.AreEqual(16,matrix.m03,.00001f);Assert.AreEqual(22,matrix.m13,.00001f);
            Assert.AreEqual(.388057f,matrix.m00,.00001f);Assert.AreEqual(.0970143f,matrix.m10,.00001f);
            Assert.AreEqual(-.169775f,matrix.m01,.00001f);Assert.AreEqual(.6790998f,matrix.m11,.00001f);
            rig.bones[0].scaleX=-2;rig.RefreshPose();matrix=rig.BoneMatrix(1);
            Assert.AreEqual(4,matrix.m03,.00001f);Assert.AreEqual(-.388057f,matrix.m00,.00001f);
            Assert.AreEqual(.169775f,matrix.m01,.00001f);Assert.AreEqual(.6790998f,matrix.m11,.00001f);
        } finally {Object.DestroyImmediate(root);}
    }
    [UnityTest]
    public IEnumerator BurstPreservesNativePosesLengthAndOneShotCompletion()
    {
        var root=new GameObject("Canvas",typeof(Canvas));
        var burst=Object.Instantiate(Resources.Load<RecoveredWinBurst>("RecoveredUI/WinBurst"),root.transform,false);
        var rig=burst.GetComponent<RecoveredRegionRig>();float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Assert.AreEqual(66,rig.bones.Length);Assert.AreEqual(42,rig.slots.Length);Assert.AreEqual(384,rig.regions.Length);
            // Original bkbai0: atlas bounds 2,515,331,87, rotate 90; physical size 87x331.
            // Geometry BL/TL/TR/BR maps to packed BR/BL/TL/TR after Unity's vertical UV flip.
            var uv=rig.regions[0].uv;
            Assert.AreEqual(89f/934,uv[0].x,.000001f);Assert.AreEqual(1-846f/848,uv[0].y,.000001f);
            Assert.AreEqual(2f/934,uv[1].x,.000001f);Assert.AreEqual(1-846f/848,uv[1].y,.000001f);
            Assert.AreEqual(2f/934,uv[2].x,.000001f);Assert.AreEqual(1-515f/848,uv[2].y,.000001f);
            Assert.AreEqual(89f/934,uv[3].x,.000001f);Assert.AreEqual(1-515f/848,uv[3].y,.000001f);
            Assert.AreEqual(1,burst.GetComponentsInChildren<CanvasRenderer>().Length);Assert.IsFalse(burst.GetComponent<CanvasRenderer>().cullTransparentMesh);
            Assert.AreEqual(new Vector2(50,50),((RectTransform)burst.transform).sizeDelta);
            var clip=burst.GetComponent<Animation>().GetClip("animation");Assert.AreEqual(35f/30,clip.length,.00001f);
            clip.SampleAnimation(burst.gameObject,0);rig.RefreshPose();Assert.AreEqual(.4f,rig.bones[15].scaleX,.00001f);
            Assert.AreEqual(.4f,((Vector3)rig.BoneMatrix(15).GetColumn(0)).magnitude,.00001f);
            clip.SampleAnimation(burst.gameObject,.35f);Assert.AreEqual(153.5045f,rig.bones[15].rotation,.001f);
            clip.SampleAnimation(burst.gameObject,.7f);rig.RefreshPose();
            Assert.AreEqual(-28.37482f,rig.bones[15].x,.001f);Assert.AreEqual(-558.897827f,rig.bones[15].y,.001f);
            Assert.AreEqual(.55f,((Vector3)rig.BoneMatrix(15).GetColumn(0)).magnitude,.0001f);
            Time.timeScale=1;Time.captureDeltaTime=.05f;int completed=0;burst.Completed+=value=>completed++;
            burst.Play();Time.timeScale=0;for(int i=0;i<10;i++)yield return null;
            Assert.AreEqual(0,completed);Assert.IsTrue(burst.IsPlaying);Time.timeScale=1;float start=Time.time;
            for(int i=0;i<35&&burst.IsPlaying;i++)yield return null;
            Assert.AreEqual(1,completed);Assert.GreaterOrEqual(Time.time-start,35f/30-.001f);
            burst.Play();burst.gameObject.SetActive(false);burst.gameObject.SetActive(true);
            for(int i=0;i<30;i++)yield return null;Assert.AreEqual(1,completed);
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(root);}
    }
}
