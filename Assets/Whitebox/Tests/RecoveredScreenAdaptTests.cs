using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class RecoveredScreenAdaptTests
{
    [TestCase(0f,12f,24f,-36f,-48f)]
    [TestCase(.5f,14f,28f,-42f,-56f)]
    [TestCase(1f,16f,32f,-48f,-64f)]
    public void SafeInsetsUseNativeLinearReferenceBlend(float match,float left,float bottom,float right,float top)
    {
        var host=new GameObject("Adapt host",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var child=new GameObject("Adapt content",typeof(RectTransform));
        try
        {
            child.transform.SetParent(host.transform,false);var scaler=host.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1200,1600);scaler.matchWidthOrHeight=match;
            var adapt=child.AddComponent<RecoveredScreenAdapt>();adapt.Apply(1000,1000,new Rect(10,20,960,940));var rect=(RectTransform)child.transform;
            Assert.AreEqual(Vector2.zero,rect.anchorMin);Assert.AreEqual(Vector2.one,rect.anchorMax);
            Assert.That(rect.offsetMin.x,Is.EqualTo(left).Within(.001f));Assert.That(rect.offsetMin.y,Is.EqualTo(bottom).Within(.001f));Assert.That(rect.offsetMax.x,Is.EqualTo(right).Within(.001f));Assert.That(rect.offsetMax.y,Is.EqualTo(top).Within(.001f));
            Assert.AreEqual(new Rect(10,20,960,940),adapt.LastSafeArea);Assert.AreEqual(1000,adapt.LastWidth);Assert.AreEqual(1000,adapt.LastHeight);
            adapt.Apply(2000,1000,new Rect(0,0,2000,1000));Assert.That(rect.offsetMin.sqrMagnitude,Is.LessThan(.00001f));Assert.That(rect.offsetMax.sqrMagnitude,Is.LessThan(.00001f));
        }
        finally{Object.DestroyImmediate(child);Object.DestroyImmediate(host);}
    }
    [Test]
    public void InvalidDimensionsPreserveLayoutAndLastAppliedState()
    {
        var host=new GameObject("Adapt host",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var child=new GameObject("Adapt content",typeof(RectTransform));
        try
        {
            child.transform.SetParent(host.transform,false);var adapt=child.AddComponent<RecoveredScreenAdapt>();adapt.Apply(100,200,new Rect(2,3,90,180));var rect=(RectTransform)child.transform;var min=rect.offsetMin;var max=rect.offsetMax;var last=adapt.LastSafeArea;
            adapt.Apply(1,200,new Rect(0,0,100,200));adapt.Apply(100,0,new Rect(0,0,100,200));adapt.Apply(100,200,new Rect(0,0,1,200));adapt.Apply(100,200,new Rect(0,0,100,float.NaN));
            Assert.AreEqual(min,rect.offsetMin);Assert.AreEqual(max,rect.offsetMax);Assert.AreEqual(last,adapt.LastSafeArea);Assert.AreEqual(100,adapt.LastWidth);
        }
        finally{Object.DestroyImmediate(child);Object.DestroyImmediate(host);}
    }
    [Test]
    public void ParentScalerWinsOverUiRootAndMissingScalerDoesNotInventSettings()
    {
        var fallback=new GameObject("UI root",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var host=new GameObject("Parent",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var child=new GameObject("Content",typeof(RectTransform));
        try
        {
            var rect=(RectTransform)child.transform;var adapt=child.AddComponent<RecoveredScreenAdapt>();var min=rect.offsetMin;adapt.Apply(100,100,new Rect(10,10,80,80));Assert.AreEqual(min,rect.offsetMin);Assert.AreEqual(0,adapt.LastWidth);
            fallback.GetComponent<CanvasScaler>().referenceResolution=new Vector2(200,200);adapt.BindUiRootScaler(fallback.GetComponent<CanvasScaler>());adapt.Apply(100,100,new Rect(10,10,80,80));Assert.AreEqual(new Vector2(20,20),rect.offsetMin);
            host.GetComponent<CanvasScaler>().referenceResolution=new Vector2(300,300);child.transform.SetParent(host.transform,false);adapt.BindUiRootScaler(fallback.GetComponent<CanvasScaler>());adapt.Apply(100,100,new Rect(10,10,80,80));Assert.AreEqual(new Vector2(30,30),rect.offsetMin);
        }
        finally{Object.DestroyImmediate(child);Object.DestroyImmediate(host);Object.DestroyImmediate(fallback);}
    }
}
