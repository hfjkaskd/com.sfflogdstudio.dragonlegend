using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredMoreWildWindow : MonoBehaviour, IRecoveredMoreWildView
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text tips, claimText;
        [SerializeField] private Button claimButton, closeButton;
        [SerializeField] private RecoveredFirstSpinGuide guide;
        [SerializeField] private RectTransform fingerPrefab;
        [SerializeField] private string tipsFormat, freeText, adText;
        [SerializeField] private float duration;
        [SerializeField] private AnimationCurve enterEase, exitEase;
        private RecoveredMoreWildClaim claim;
        private RectTransform finger;
        private Camera camera;
        private int phase;
        private float elapsed;
        public Button ClaimButton => claimButton;
        public Button CloseButton => closeButton;
        public TMP_Text Tips => tips;
        public TMP_Text ClaimText => claimText;
        public RecoveredFirstSpinGuide Guide => guide;
        public RectTransform Finger => finger;
        public event Action<string> SoundRequested;
        private void Awake()
        {
            claimButton.onClick.AddListener(Claim);
            closeButton.onClick.AddListener(Close);
        }
        public void Bind(GameEntry game)
        {
            Cancel();
            claim = new RecoveredMoreWildClaim(game.Rules, game.PlayerProgress, game.PlayerStore.Data, game.Ads, this);
            camera = game.GetComponent<Canvas>().worldCamera;
            GetComponent<Canvas>().worldCamera = camera;
        }
        public void Show(bool isFree)
        {
            if (gameObject.activeSelf) return;
            gameObject.SetActive(true);
            tips.text = string.Format(tipsFormat, claim.BeforeShow(isFree));
            claimText.text = isFree ? freeText : adText;
            content.localScale = Vector3.zero; elapsed = 0; phase = 1;
        }
        private void AfterShow()
        {
            if (!claim.IsFree) return;
            guide.Show((RectTransform)transform, (RectTransform)claimButton.transform, camera, 2);
            if (finger == null) finger = Instantiate(fingerPrefab, claimText.transform, false);
            else finger.SetParent(claimText.transform, false);
            finger.localScale = Vector3.one; finger.anchoredPosition = Vector2.zero;
            finger.gameObject.SetActive(true);
        }
        private void Claim() => claim.Click("ClaimBtn");
        private void Close() => claim.Click("CloseBtn");
        public void PlaySound(string sound) => SoundRequested?.Invoke(sound);
        public void HideFinger() { if (finger != null) finger.gameObject.SetActive(false); }
        public void Hide()
        {
            guide.Hide(); content.localScale = Vector3.one; elapsed = 0; phase = 2;
        }
        private void Update()
        {
            if (phase == 0) return;
            elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / duration);
            content.localScale = Vector3.one * (phase == 2 ? 1 - exitEase.Evaluate(t) : enterEase.Evaluate(t));
            if (t < 1) return;
            bool closing = phase == 2; phase = 0;
            if (closing) gameObject.SetActive(false); else AfterShow();
        }
        public void Cancel()
        {
            claim?.Cancel(); guide.Hide(); HideFinger(); phase = 0; gameObject.SetActive(false);
        }
        private void OnDestroy() => claim?.Cancel();
    }
}
