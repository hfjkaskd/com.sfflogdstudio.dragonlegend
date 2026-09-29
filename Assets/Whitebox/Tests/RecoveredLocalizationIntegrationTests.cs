using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredLocalizationIntegrationTests
{
    [UnityTest]
    public IEnumerator SettingsPortugueseLayoutFollowsLanguageEventsAndReactivationWithoutChangingEnglish()
    {
        int previousLanguage = GameLocalization.CurrentLanguage;
        GameLocalization.SetLanguage(0);
        var window = Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/MainUtility/Settings"));
        try
        {
            LocalizedGameText off = null;
            foreach (var binding in window.GetComponentsInChildren<LocalizedGameText>(true))
                if (binding.Key == "settings.off") { off = binding; break; }
            Assert.IsNotNull(off, "The real Settings prefab must contain a localized OFF label.");
            var text = off.GetComponent<TMPro.TMP_Text>();
            Assert.IsNotNull(off.GetComponent<LocalizedTextLayout>(), "Portuguese layout must be authored on the prefab.");
            float englishFontSize = text.fontSize;
            bool englishWrapping = text.enableWordWrapping;
            Vector2 englishBounds = text.rectTransform.sizeDelta;
            for (var node = off.transform; node != null; node = node.parent) node.gameObject.SetActive(true);
            Assert.AreEqual("OFF", text.text);

            // Do not call Refresh: these changes must arrive through runtime events.
            GameLocalization.SetLanguage(1);
            Assert.AreEqual("NÃO", text.text);
            Assert.AreEqual(52, text.fontSize, .01f);
            Assert.IsFalse(text.enableWordWrapping, "NÃO must stay on one line inside the switch.");

            GameLocalization.SetLanguage(0);
            Assert.AreEqual("OFF", text.text);
            Assert.AreEqual(englishFontSize, text.fontSize, .01f);
            Assert.AreEqual(englishWrapping, text.enableWordWrapping);
            Assert.AreEqual(englishBounds, text.rectTransform.sizeDelta);

            window.SetActive(false);
            GameLocalization.SetLanguage(1);
            Assert.AreEqual("OFF", text.text, "An inactive label must unsubscribe from language events.");
            Assert.AreEqual(englishFontSize, text.fontSize, .01f);
            window.SetActive(true);
            Assert.AreEqual("NÃO", text.text);
            Assert.AreEqual(52, text.fontSize, .01f);
            Assert.IsFalse(text.enableWordWrapping);
            window.SetActive(false);
            Assert.AreEqual(englishFontSize, text.fontSize, .01f, "Disabling restores the saved English layout.");
            Assert.AreEqual(englishBounds, text.rectTransform.sizeDelta);
        }
        finally
        {
            window.SetActive(false);
            Object.Destroy(window);
            GameLocalization.SetLanguage(previousLanguage);
        }
        yield return null;
    }

    private static RecoveredGameplayRules CashRules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Rgpggm = new RgpggmPoro {
            Qogt = new List<int> {100}, Rogk1roil = new List<int> {20}, Rimgg1 = new List<int> {7}
        }
    });

    [UnityTest]
    public IEnumerator CashOutCardKeepsExplicitBrazilianLanguageAcrossCountdownAndReactivation()
    {
        int previousLanguage = GameLocalization.CurrentLanguage;
        var item = Object.Instantiate(Resources.Load<RecoveredCashOutItem>("RecoveredUI/CashOutItem"));
        try
        {
            GameLocalization.SetLanguage(0);
            int now = 102;
            var data = new PlayerData();
            data.PlayerCashOutDatas.Add(new PlayerCashOutData {id=0, type=1, step=0, count=3, time=100});
            var rules = CashRules();
            item.Bind(new RecoveredPlayerProgress(rules, () => Assert.Fail("Presentation must not save"), data), rules, () => now);
            item.Initialize(0, -1, 1, null, 1);
            Assert.AreEqual("Gire 3/20 vezes", item.TaskText.text);
            Assert.AreEqual("Em análise 00:00:05", item.TimeText.text);
            StringAssert.StartsWith("R$", item.AmountText.text);

            item.gameObject.SetActive(false);
            item.gameObject.SetActive(true);
            Assert.AreEqual("Gire 3/20 vezes", item.TaskText.text, "Static labels must not overwrite dynamic text on enable.");
            now = 107;
            item.RefreshTime();
            Assert.AreEqual("Em análise 00:00:00", item.TimeText.text);

            item.Initialize(0, -1, 1, null, 0);
            Assert.AreEqual("Spin 3/20 times", item.TaskText.text);
            Assert.AreEqual("Pending Review00:00:00", item.TimeText.text);
            StringAssert.StartsWith("$", item.AmountText.text);
        }
        finally
        {
            item.Cancel();
            Object.Destroy(item.gameObject);
            GameLocalization.SetLanguage(previousLanguage);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator CashOutBottomTranslatesReadyStateAndIncompleteTaskTipWithExplicitLanguage()
    {
        var bottom = Object.Instantiate(Resources.Load<RecoveredCashOutBottom>("RecoveredUI/CashOutBottom"));
        try
        {
            var data = new PlayerData {GreenCount=100};
            var rules = CashRules();
            bottom.Bind(new RecoveredPlayerProgress(rules, () => Assert.Fail("Presentation must not save"), data), rules, () => 102);
            bottom.Initialize(0, 1, 1);
            Assert.AreEqual("Você já pode sacar!", bottom.DetailText.text);

            data.PlayerCashOutDatas.Add(new PlayerCashOutData {id=0, type=1, step=0, count=3, time=100});
            bottom.Initialize(0, 1, 1);
            string tip = null;
            bottom.TipRequested += value => tip = value;
            bottom.Button.onClick.Invoke();
            Assert.AreEqual("Gire 3/20 vezes", bottom.TaskText.text);
            Assert.AreEqual("Em análise 00:00:05", bottom.TimeText.text);
            Assert.AreEqual("Conclua a tarefa primeiro", tip);
        }
        finally
        {
            bottom.Cancel();
            Object.Destroy(bottom.gameObject);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator LoadingMessageRefreshesWhenServerCountryArrivesBetweenStages()
    {
        int previousLanguage = GameLocalization.CurrentLanguage;
        var loading = Object.Instantiate(Resources.Load<StartupLoadingView>("Loading/StartupLoading"));
        try
        {
            GameLocalization.SetLanguage(0);
            loading.SetStage("Connecting...", .1f);
            var label = loading.GetComponentInChildren<UnityEngine.UI.Text>(true);
            Assert.AreEqual("Connecting...", label.text);
            GameLocalization.SetLanguage(1);
            Assert.AreEqual("Conectando...", label.text);

            loading.gameObject.SetActive(false);
            GameLocalization.SetLanguage(0);
            Assert.AreEqual("Conectando...", label.text);
            loading.gameObject.SetActive(true);
            Assert.AreEqual("Connecting...", label.text);
        }
        finally
        {
            Object.Destroy(loading.gameObject);
            GameLocalization.SetLanguage(previousLanguage);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator GuideRevealsPortugueseAndRestoresEnglishOnNextShow()
    {
        int previousLanguage = GameLocalization.CurrentLanguage;
        var guide = Object.Instantiate(Resources.Load<RecoveredGuideText>("RecoveredUI/GuideText"));
        try
        {
            guide.gameObject.SetActive(true);
            GameLocalization.SetLanguage(1);
            guide.SetText(1);
            guide.RevealAll();
            Assert.AreEqual("Toque em GIRAR e deixe o dragão incendiar suas vitórias!", guide.Label.text);

            GameLocalization.SetLanguage(0);
            guide.SetText(1);
            guide.RevealAll();
            Assert.AreEqual("Click SPIN and let the dragon breathe fire into your wins!", guide.Label.text);
        }
        finally
        {
            guide.Cancel();
            Object.Destroy(guide.gameObject);
            GameLocalization.SetLanguage(previousLanguage);
        }
        yield return null;
    }

    [TestCase("cp_default_1.json")]
    [TestCase("cp_test.json")]
    [TestCase("cp_low_frequency_high_rewards.json")]
    public void EveryConfiguredDailyTaskHasPortugueseTextAndPreservesProgress(string file)
    {
        var config = JsonUtility.FromJson<GoldenDragonAutoGenConfig>(File.ReadAllText(
            Path.Combine(Application.streamingAssetsPath, "RecoveredConfig/Remote/" + file)));
        var rules = new RecoveredGameplayRules(config);
        foreach (var task in rules.GetTaskInfos())
        {
            string english = rules.GetTaskDescription(task.id, 3, 7, 0);
            string portuguese = rules.GetTaskDescription(task.id, 3, 7, 1);
            Assert.AreNotEqual(english, portuguese, "Missing task translation: " + english);
            StringAssert.Contains("3/7", portuguese);
            StringAssert.DoesNotContain("{", portuguese);
            StringAssert.DoesNotContain("}", portuguese);
        }
    }
}
