using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // TopTitle 0x23b9018/0x23b92ac/0x23b9480, using native Unity animation updates.
    public sealed class RecoveredBalancePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI greenText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image progressFill;
        [SerializeField] private float animationDuration;
        [SerializeField] private AnimationCurve balanceEase;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private int language;
        private bool cashActive;
        private float cashStart, cashEnd, cashElapsed;
        private readonly List<LevelTween> levelTweens = new List<LevelTween>(4);
        private struct LevelTween { public float start, target, ratio, elapsed; public int level; public bool started; }
        public string BalanceText => greenText.text;
        public string LevelText => levelText.text;
        public string ProgressText => progressText.text;
        public float FillAmount => progressFill.fillAmount;

        public void Bind(RecoveredPlayerProgress progress, RecoveredGameplayRules gameplayRules, int languageType)
        {
            Unbind();
            player = progress ?? throw new ArgumentNullException(nameof(progress));
            rules = gameplayRules ?? throw new ArgumentNullException(nameof(gameplayRules));
            language = languageType;
            player.GreenCountChanged += OnGreenCountChanged;
            player.LevelExperienceChanged += OnLevelExperienceChanged;
            greenText.text = RecoveredCurrency.Format(player.GreenCount, language);
            levelText.text = string.Format("{0}", player.Level);
            float need = rules.GetNeedPro(player.Level);
            progressText.text = string.Format("{0}/{1}", player.Experience, need);
            progressFill.fillAmount = player.Experience / need;
        }
        public void Unbind()
        {
            if (player != null) {
                player.GreenCountChanged -= OnGreenCountChanged;
                player.LevelExperienceChanged -= OnLevelExperienceChanged;
            }
            player = null; rules = null; cashActive = false; levelTweens.Clear();
        }
        private void OnDestroy() => Unbind();
        private void OnGreenCountChanged(float before, float after)
        {
            // Kill the previous cash tween without completing; restart from the event's old balance.
            cashStart = before; cashEnd = after; cashElapsed = 0; cashActive = true;
        }
        private void OnLevelExperienceChanged(int level, float experience, float need)
        {
            progressText.text = string.Format("{0}/{1}", experience, need);
            float ratio = experience / need;
            // Original does not kill preceding fill tweens. Capture start on the first update.
            levelTweens.Add(new LevelTween { target = Mathf.Clamp01(ratio), ratio = ratio, level = level });
        }
        private void Update() => AdvanceAnimations(Time.deltaTime);
        public void AdvanceAnimations(float deltaTime)
        {
            if (cashActive) {
                cashElapsed += deltaTime;
                float t = Mathf.Clamp01(cashElapsed / animationDuration);
                float amount = cashStart + (cashEnd - cashStart) * balanceEase.Evaluate(t);
                greenText.text = RecoveredCurrency.Format(amount, language);
                if (t >= 1) cashActive = false;
            }
            for (int i = 0; i < levelTweens.Count;) {
                var tween = levelTweens[i];
                if (!tween.started) { tween.start = progressFill.fillAmount; tween.started = true; }
                tween.elapsed += deltaTime;
                float t = Mathf.Clamp01(tween.elapsed / animationDuration);
                progressFill.fillAmount = tween.start + (tween.target - tween.start) * t;
                if (t < 1) { levelTweens[i] = tween; i++; continue; }
                levelTweens.RemoveAt(i);
                // 0x23b9694 compares the raw ratio exactly with one, not the clamped Image value.
                if (tween.ratio == 1) {
                    levelText.text = string.Format("{0}", tween.level);
                    progressFill.fillAmount = 0;
                    progressText.text = string.Format("{0}/{1}", 0, (float)rules.GetNeedPro(player.Level));
                }
            }
        }
    }

    public static class RecoveredCurrency
    {
        private static readonly NumberFormatInfo English = CultureInfo.GetCultureInfo("en-US").NumberFormat;
        private static readonly NumberFormatInfo Brazilian = CultureInfo.GetCultureInfo("pt-BR").NumberFormat;
        // CurrencyUtils 0x238c4e4. LanguageType EN=0; every nonzero value takes BR branch.
        public static string Format(float value, int languageType, int decimals = 2)
        {
            var format = languageType == 0 ? English : Brazilian;
            string specifier = decimals == 2 ? "F2" : (decimals == 0 ? "N0" : "F" + decimals.ToString());
            string number = (value / 100f).ToString(specifier, CultureInfo.InvariantCulture);
            return format.CurrencySymbol + number.Replace(".", format.NumberDecimalSeparator);
        }
    }
}
