using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class RecoveredBaseReelControllerTests
{
    [UnityTest] public IEnumerator OrdinaryFiveColumns() => Run(-1);
    [UnityTest] public IEnumerator AnticipationFromFirstColumn() => Run(0);
    [UnityTest] public IEnumerator AnticipationFromMiddleColumn() => Run(2);
    [UnityTest] public IEnumerator AnticipationFromLastColumn() => Run(4);

    private static IEnumerator Run(int selected)
    {
        var state = Random.state; float captureDelta = Time.captureDeltaTime; float scale = Time.timeScale;
        var controller = Object.Instantiate(Resources.Load<RecoveredBaseReelController>("RecoveredSymbols/BaseReels"));
        var order = new List<int>(); var highlights = new List<string>();
        var showTimes = new float[5]; int vibrations = 0, sounds = 0, completed = 0;
        try {
            Time.captureDeltaTime = 0.05f; Time.timeScale = 1;
            controller.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"));
            controller.StopAnimationRequested += index => order.Add(index);
            controller.ReelStopSoundRequested += () => sounds++;
            controller.VibrationRequested += milliseconds => { Assert.AreEqual(200, milliseconds); vibrations++; };
            controller.AnticipationVisibilityRequested += (index, visible) => {
                highlights.Add(index + (visible ? "+" : "-"));
                if (visible) showTimes[index] = Time.time;
                else if (showTimes[index] > 0) Assert.GreaterOrEqual(Time.time - showTimes[index], 0.999f);
            };
            controller.ReelsStopped += () => completed++;
            yield return null;
            controller.Begin(selected, index => new[] { index, (index + 3) % 7, 7 });
            Assert.IsTrue(controller.MotionAt(0).IsSpinning);
            for (int i = 1; i < 5; i++) Assert.IsFalse(controller.MotionAt(i).IsSpinning);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual(-3.8f + 1.9f * i, controller.ReelAt(i).transform.localPosition.x, 0.0001f);
                Assert.AreEqual(-0.01f, controller.ReelAt(i).transform.localPosition.y);
            }
            yield return null; yield return null;
            Assert.IsFalse(controller.MotionAt(1).IsSpinning, "0.15 second stagger cannot start column 1 after only 0.10 seconds.");
            yield return null; yield return null;
            Assert.IsTrue(controller.MotionAt(1).IsSpinning);
            for (int frame = 0; frame < 200 && controller.IsRunning && controller.Error == null; frame++) yield return null;
            Assert.IsNull(controller.Error); Assert.IsFalse(controller.IsRunning); Assert.AreEqual(5, controller.StoppedCount);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, order);
            Assert.AreEqual(selected < 0 ? 5 : selected, sounds);
            Assert.AreEqual(selected < 0 ? 0 : 5 - selected, vibrations);
            var expected = new List<string> { "0-", "1-", "2-", "3-", "4-" };
            if (selected >= 0) for (int i = selected; i < 5; i++) { expected.Add(i + "+"); expected.Add(i + "-"); }
            CollectionAssert.AreEqual(expected, highlights);
            for (int i = 0; i < 5; i++) {
                Assert.IsFalse(controller.MotionAt(i).IsSpinning);
                Assert.AreEqual(i, controller.ReelAt(i).SymbolId(0));
                Assert.AreEqual((i + 3) % 7, controller.ReelAt(i).SymbolId(1));
                Assert.AreEqual(7, controller.ReelAt(i).SymbolId(2));
            }
            Assert.AreEqual(1, completed);
            yield return null; Assert.AreEqual(1, completed);
            if (selected == 2) Capture(controller);
        } finally { Time.captureDeltaTime = captureDelta; Time.timeScale = scale; Object.Destroy(controller.gameObject); Random.state = state; }
    }
    private static void Capture(RecoveredBaseReelController controller)
    {
        var host = new GameObject("Controller capture camera"); var camera = host.AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 3.6f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 1;
        var render = new RenderTexture(1080, 720, 24); var previous = RenderTexture.active;
        var image = new Texture2D(1080, 720, TextureFormat.RGB24, false);
        try {
            camera.targetTexture = render;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = render });
            RenderTexture.active = render; image.ReadPixels(new Rect(0, 0, 1080, 720), 0, 0); image.Apply();
            var pixels = image.GetPixels32(); int visible = 0;
            for (int y = 0; y < 720; y++) for (int x = 0; x < 1080; x++) {
                var color = pixels[y * 1080 + x]; bool drawn = color.r > 8 || color.g > 8 || color.b > 8;
                if (y >= 625) Assert.IsFalse(drawn, "Clip must exclude upper recycled slots.");
                if (drawn) visible++;
            }
            Assert.Greater(visible, 10000);
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/current-base-controller.png")), image.EncodeToPNG());
        } finally { RenderTexture.active = previous; Object.Destroy(host); Object.Destroy(image); render.Release(); Object.Destroy(render); }
    }
}
