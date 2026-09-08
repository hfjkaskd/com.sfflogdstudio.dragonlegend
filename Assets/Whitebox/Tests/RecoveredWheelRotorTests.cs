using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class RecoveredWheelRotorTests
{
    [UnityTest]
    public IEnumerator SourceWheelShowsBothProfilesAndStopsAllEightWedgesWithLiveRewards()
    {
        float scale = Time.timeScale, delta = Time.captureDeltaTime;
        var random = Random.state;
        var host = new GameObject("Wheel capture", typeof(RectTransform), typeof(Canvas));
        var cameraHost = new GameObject("Wheel camera", typeof(Camera));
        var camera = cameraHost.GetComponent<Camera>();
        var canvas = host.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
        var target = new RenderTexture(1080, 1920, 24);
        camera.targetTexture = target; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        try
        {
            Time.timeScale = 1; Time.captureDeltaTime = .125f;
            var rotor = Object.Instantiate(Resources.Load<RecoveredWheelRotor>("RecoveredUI/WheelRotor"), host.transform, false);
            var config = new RrggiomgPoro {
                KtgglRgkorp = new List<int> { 111, 12345, 333, 23456, 555, 34567, 777, 45678 },
                KtgglRgkorpKgiitr = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 }
            };
            var rules = new RecoveredGameplayRules(new GoldenDragonAutoGenConfig { Rrggiomg = config });
            Assert.AreEqual(new Vector2(828, 828), ((RectTransform)rotor.transform).sizeDelta);
            Assert.AreEqual(new Vector2(0, -268), ((RectTransform)rotor.transform).anchoredPosition);
            Assert.AreEqual(2, rotor.Duration);
            Assert.AreEqual("fw_bg01", rotor.GetComponent<Image>().sprite.name);
            var instances = new RecoveredWheelItem[8];
            string[] labels = { "fw_major", null, "fw_minor", null, "fw_grand", null, "fw_minor", null };
            for (int profile = 0; profile < 2; profile++)
            {
                rotor.Prepare(rules, profile == 1, profile);
                for (int i = 0; i < 8; i++)
                {
                    var item = rotor.Item(i);
                    if (profile == 0) instances[i] = item; else Assert.AreSame(instances[i], item, "Show reuses the eight initialized items.");
                    var anchor = (RectTransform)item.transform.parent;
                    Assert.That(Vector2.Distance(anchor.anchoredPosition, new Vector2(-Mathf.Sin(i * Mathf.PI / 4), Mathf.Cos(i * Mathf.PI / 4)) * 268), Is.LessThan(.001f));
                    Assert.That(Quaternion.Angle(anchor.localRotation, Quaternion.Euler(0, 0, i * 45)), Is.LessThan(.05f));
                    Assert.AreEqual(Vector2.zero, ((RectTransform)item.transform).anchoredPosition);
                    bool cash = i % 2 == 1;
                    Assert.AreEqual(cash && profile == 0, item.Cash.gameObject.activeSelf);
                    Assert.AreEqual(cash && profile == 1, item.Coin.gameObject.activeSelf);
                    Assert.AreEqual(!cash, item.Jackpot.gameObject.activeSelf);
                    if (cash)
                    {
                        string expected = profile == 0 ? "$" + (config.KtgglRgkorp[i] / 100f).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) :
                            "R$" + (config.KtgglRgkorp[i] / 100f).ToString("F2", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
                        Assert.AreEqual(expected, (profile == 0 ? item.CashText : item.CoinText).text);
                        Assert.IsNotNull(item.CashText.font); Assert.IsNotNull(item.CoinText.font);
                    }
                    else
                    {
                        Assert.AreEqual(labels[i], item.Jackpot.sprite.name);
                        // Serialized quaternion is -90 degrees; the source's
                        // stale Inspector Euler hint says +90 and is not used.
                        Assert.That(Quaternion.Angle(item.Jackpot.transform.localRotation, Quaternion.Euler(0, 0, i == 6 ? 90 : -90)), Is.LessThan(.05f));
                        Assert.AreEqual(item.Jackpot.sprite.rect.size, item.Jackpot.rectTransform.sizeDelta);
                    }
                }
                yield return null;
                Capture(camera, target, profile == 0 ? "current-wheel-rotor-us.png" : "current-wheel-rotor-alternative.png");
            }
            int sounds = 0;
            rotor.SoundRequested += sound => { Assert.AreEqual("wheelSpin", sound); sounds++; };
            int[] types = { 0, 1, 2, 1, 3, 1, 2, 1 };
            for (int selected = 0; selected < 8; selected++)
            {
                for (int i = 0; i < 8; i++) config.KtgglRgkorpKgiitr[i] = i == selected ? 1000000 : 0;
                Random.InitState(97); Assert.Greater(Random.Range(0, 1000000), 0); int nextRandom = Random.Range(0, 1000000);
                Random.InitState(97); rotor.Prepare(rules, false, 0);
                Assert.AreEqual(selected, rotor.ResultIndex);
                Assert.AreEqual(nextRandom, Random.Range(0, 1000000), "Prepare consumes exactly one weighted draw.");
                config.KtgglRgkorp[selected] += 100; // The source reads this when the spin finishes, not during selection.
                int callbacks = 0;
                rotor.transform.localEulerAngles = new Vector3(17, 23, 31);
                rotor.StartSpin((type, reward) => {
                    callbacks++;
                    Assert.IsFalse(rotor.IsSpinning);
                    Assert.AreEqual(types[rotor.ResultIndex], (int)type);
                    Assert.AreEqual((float)config.KtgglRgkorp[rotor.ResultIndex], reward);
                    var worldOffset = rotor.Item(rotor.ResultIndex).transform.parent.position - rotor.transform.position;
                    Assert.That(Mathf.Abs(worldOffset.x), Is.LessThan(.05f));
                    Assert.Greater(worldOffset.y, 0, "Winning wedge must finish at the top of the wheel.");
                });
                Assert.That(Quaternion.Angle(Quaternion.identity, rotor.transform.localRotation), Is.LessThan(.001f));
                Time.timeScale = 0;
                for (int frame = 0; frame < 3; frame++) yield return null;
                Assert.AreEqual(0, rotor.Rotation); Assert.AreEqual(0, callbacks);
                Time.timeScale = 1;
                float angle = -1800 - selected * 45;
                for (int frame = 1; frame <= 16; frame++)
                {
                    yield return null;
                    float t = frame / 16f;
                    Assert.That(rotor.Rotation, Is.EqualTo(angle * (1 - Mathf.Pow(1 - t, 3))).Within(.002f));
                    Assert.AreEqual(frame == 16 ? 1 : 0, callbacks);
                }
                for (int frame = 0; frame < 3; frame++) yield return null;
                Assert.AreEqual(1, callbacks);
            }
            Assert.AreEqual(8, sounds);
            rotor.Item(6).Initialize(RecoveredWheelType.Mini, 2, rules, false, 0);
            Assert.That(Quaternion.Angle(rotor.Item(6).Jackpot.transform.localRotation, Quaternion.Euler(0, 0, 90)), Is.LessThan(.05f), "Native initialization retains the index-6 rotation when reused elsewhere.");
        }
        finally
        {
            camera.targetTexture = null; Object.Destroy(target); Object.Destroy(host); Object.Destroy(cameraHost);
            Time.timeScale = scale; Time.captureDeltaTime = delta; Random.state = random;
        }
    }
    private static void Capture(Camera camera, RenderTexture target, string name)
    {
        var previous = RenderTexture.active;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../Artifacts/" + name), image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; Object.Destroy(image); }
    }
}
