using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RecoveredSpinButtonTests
{
    [UnityTest]
    public IEnumerator NativeCurvesPreserveRotationAlphaAndResetClickLayers()
    {
        var button = Object.Instantiate(Resources.Load<RecoveredSpinButton>("RecoveredUI/SpinButton"));
        try {
            var player = button.GetComponent<Animation>(); player.enabled = false;
            var setup = Resources.Load<AnimationClip>("RecoveredUI/SpinButton/setup");
            var idle = Resources.Load<AnimationClip>("RecoveredUI/SpinButton/idle");
            var click = Resources.Load<AnimationClip>("RecoveredUI/SpinButton/dianji");
            Assert.AreEqual(4, idle.length); Assert.AreEqual(1, click.length);
            setup.SampleAnimation(button.gameObject, 0); idle.SampleAnimation(button.gameObject, 1);
            var leaf = button.transform.Find("Visual/root/dx/yezi");
            Assert.AreEqual(270, leaf.localEulerAngles.z, 0.01f);
            Assert.AreEqual(90, leaf.Find("yezi2").localEulerAngles.z, 0.01f);
            var glow = leaf.Find("Slot5/Image").GetComponent<Image>();
            Assert.IsTrue(glow.enabled); Assert.AreEqual(96f / 255, glow.color.a, 0.0001f);
            setup.SampleAnimation(button.gameObject, 0); click.SampleAnimation(button.gameObject, 0.1f);
            Assert.AreEqual(152, leaf.localEulerAngles.z, 0.01f); // -60 - 740 * 0.1 / 0.5.
            var ring = button.transform.Find("Visual/root/dx/diabn/Slot2/Image").GetComponent<Image>();
            Assert.IsTrue(ring.enabled); Assert.AreEqual(162f / 255, ring.color.a, 0.0001f);
            setup.SampleAnimation(button.gameObject, 0); idle.SampleAnimation(button.gameObject, 0);
            Assert.IsFalse(ring.enabled); Assert.AreEqual(0, leaf.localEulerAngles.z, 0.01f);
            yield return null;
        } finally { Object.Destroy(button.gameObject); }
    }
    [UnityTest]
    public IEnumerator VisibleButtonReceivesPointerAndClickReturnsToIdle()
    {
        var host = new GameObject("Spin button canvas", typeof(Canvas), typeof(GraphicRaycaster));
        host.layer = 5; // Camera culling applies to the root screen-space Canvas layer.
        var cameraHost = new GameObject("Button camera", typeof(Camera));
        var eventsHost = new GameObject("Button events", typeof(EventSystem));
        var camera = cameraHost.GetComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true; camera.orthographicSize = 5; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black; camera.cullingMask = 32;
        var canvas = host.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var render = new RenderTexture(512, 512, 24); camera.targetTexture = render;
        var button = Object.Instantiate(Resources.Load<RecoveredSpinButton>("RecoveredUI/SpinButton"), host.transform, false);
        var previous = RenderTexture.active; Texture2D capture = null; float scale = Time.timeScale, dt = Time.captureDeltaTime;
        try {
            Time.timeScale = 1; Time.captureDeltaTime = 0.05f;
            int clicks = 0; button.Button.onClick.AddListener(() => { clicks++; button.PlayAcceptedClick(); });
            yield return null; Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(camera, button.Button.targetGraphic.transform.position);
            var pointer = new PointerEventData(eventsHost.GetComponent<EventSystem>()) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); host.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.IsNotEmpty(hits); Assert.AreSame(button.Button.targetGraphic.gameObject, hits[0].gameObject);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(1, clicks); Assert.IsTrue(button.IsClickAnimationPlaying);
            yield return null; yield return null;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = render });
            RenderTexture.active = render; capture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); capture.Apply();
            int green = 0; foreach (var c in capture.GetPixels32()) if (c.g > c.r && c.g > 50) green++;
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/current-spin-button.png")), capture.EncodeToPNG());
            Assert.Greater(green, 1000, "Actual green button artwork must render.");
            for (int i = 0; i < 40 && button.IsClickAnimationPlaying; i++) yield return null;
            Assert.IsFalse(button.IsClickAnimationPlaying); Assert.IsTrue(button.GetComponent<Animation>().IsPlaying("idle"));
        } finally {
            Time.timeScale = scale; Time.captureDeltaTime = dt; RenderTexture.active = previous;
            Object.Destroy(host); Object.Destroy(cameraHost); Object.Destroy(eventsHost);
            if (capture != null) Object.Destroy(capture); render.Release(); Object.Destroy(render);
        }
    }
}
