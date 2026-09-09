using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // UIFreeSpinEndView 23afcfc / 23b04d4 / 23b011c. BaseWindow has no scale transition.
    public sealed class RecoveredFreeEndWindow : MonoBehaviour
    {
        [SerializeField] private Text totalText;
        [SerializeField] private TMP_Text tipText;
        [SerializeField] private Button continueButton;
        [SerializeField] private RecoveredRegionAnimator artwork;
        [SerializeField] private GameObject confetti;
        [SerializeField] private string tipFormat;
        [SerializeField] private int startClip, idleClip;
        [SerializeField] private float startDelay, countDuration, buttonDelay, buttonDuration;
        [SerializeField] private AnimationCurve countCurve, buttonCurve;
        private RecoveredPlayerProgress player;
        private Func<int> language;
        private Action completed;
        private RecoveredReelWait wait;
        private int bindingVersion;
        public bool IsShown { get; private set; }
        public Text TotalText => totalText;
        public TMP_Text TipText => tipText;
        public Button ContinueButton => continueButton;
        public RecoveredRegionAnimator Artwork => artwork;
        public event Action<string> SoundRequested;
        public event Action<Exception> Failed;

        private void Awake() => continueButton.onClick.AddListener(Continue);
        public void Bind(RecoveredPlayerProgress progress, Transform main, Func<int> currentLanguage)
        {
            CancelForProfileChange();
            player = progress ?? throw new ArgumentNullException(nameof(progress));
            language = currentLanguage ?? throw new ArgumentNullException(nameof(currentLanguage));
            GetComponent<Canvas>().worldCamera = main.GetComponentInParent<Canvas>().worldCamera;
        }
        public void Show(int initialSpinCount, Action afterHide)
        {
            SoundRequested?.Invoke("fsend"); completed = afterHide;
            tipText.gameObject.SetActive(false); totalText.text = ""; continueButton.gameObject.SetActive(false);
            IsShown = true; gameObject.SetActive(true); artwork.PlayOnce(startClip, null);
            wait = RecoveredReelWait.Delay(startDelay, BeginCount, error => Failed?.Invoke(error));
            tipText.text = string.Format(CultureInfo.InvariantCulture, tipFormat, initialSpinCount);
        }
        private void BeginCount()
        {
            wait = null; artwork.Play(idleClip); confetti.SetActive(true);
            // The native sequence's callback executes on its first tween update.
            RecoveredTreasureCardRunner.Run(StartCount(bindingVersion));
        }
        private IEnumerator StartCount(int version)
        {
            if (this == null || version != bindingVersion) yield break;
            SoundRequested?.Invoke("count");
            float total = player.TotalFreeSpinWin;
            RecoveredTreasureCardRunner.Run(Count(total, version));
            tipText.gameObject.SetActive(true);
        }
        private IEnumerator Count(float total, int version)
        {
            float elapsed = 0;
            while (this != null && version == bindingVersion)
            {
                elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / countDuration);
                totalText.text = RecoveredCurrency.Format(total * countCurve.Evaluate(t), language(), 2);
                if (t >= 1)
                {
                    continueButton.gameObject.SetActive(false);
                    RecoveredTreasureCardRunner.Run(RevealButton(version)); yield break;
                }
                yield return null;
            }
        }
        private IEnumerator RevealButton(int version)
        {
            float elapsed = 0; bool revealed = false;
            while (this != null && version == bindingVersion)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= buttonDelay)
                {
                    if (!revealed) { continueButton.gameObject.SetActive(true); continueButton.transform.localScale = Vector3.zero; revealed = true; }
                    float t = Mathf.Clamp01((elapsed - buttonDelay) / buttonDuration);
                    continueButton.transform.localScale = Vector3.one * buttonCurve.Evaluate(t);
                    if (t >= 1) yield break;
                }
                yield return null;
            }
        }
        private void Continue()
        {
            if (!IsShown) return;
            SoundRequested?.Invoke("click"); Hide();
        }
        public void Hide()
        {
            if (!IsShown) return;
            IsShown = false; gameObject.SetActive(false);
            var callback = completed; completed = null; callback?.Invoke();
        }
        private void OnDestroy() { wait?.Cancel(); completed = null; }
        public void CancelForProfileChange()
        {
            bindingVersion++; wait?.Cancel(); wait = null; completed = null; IsShown = false;
            gameObject.SetActive(false);
        }
    }
}
