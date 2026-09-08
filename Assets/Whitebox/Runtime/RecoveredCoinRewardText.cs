using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCoinRewardText : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private float showDelay;
        [SerializeField] private float scaleDuration;
        [SerializeField] private float peakScale;
        [SerializeField] private float restingScale;
        [SerializeField] private float presentationDelay;
        private RecoveredReelWait wait;
        private float reward, elapsed;
        private int language, phase;
        private Vector3 from;
        public Text Label => label;
        public bool IsScaling => phase != 0;
        public Exception Error { get; private set; }
        // Native PlayAnim starts the separate lamp flight after this delay, without awaiting arrival.
        public event Action PresentationFinished;
        public void Begin(float amount, int languageType)
        {
            Hide(); reward = amount; language = languageType; Error = null;
            wait = RecoveredReelWait.Delay(showDelay, Show, Failed);
        }
        private void Show()
        {
            wait = null;
            label.text = RecoveredCurrency.Format(reward, language, 2);
            label.gameObject.SetActive(true);
            from = label.transform.localScale; elapsed = 0; phase = 1;
            wait = RecoveredReelWait.Delay(presentationDelay, Finished, Failed);
        }
        private void Finished() { wait = null; PresentationFinished?.Invoke(); }
        private void Failed(Exception error) { wait = null; Error = error; }
        private void Update()
        {
            if (phase == 0) return;
            elapsed += Time.deltaTime;
            float t = scaleDuration == 0 ? 1 : Mathf.Clamp01(elapsed / scaleDuration);
            float eased = 1 - (1 - t) * (1 - t);
            label.transform.localScale = Vector3.LerpUnclamped(from, Vector3.one * (phase == 1 ? peakScale : restingScale), eased);
            if (t < 1) return;
            if (phase == 1) { from = label.transform.localScale; elapsed = 0; phase = 2; }
            else phase = 0;
        }
        public void Hide()
        {
            wait?.Cancel(); wait = null; phase = 0;
            label.gameObject.SetActive(false);
        }
        private void OnDisable() { wait?.Cancel(); wait = null; phase = 0; }
    }
}
