using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCollectWindow : MonoBehaviour
    {
        [SerializeField] private RectTransform content, fill, listParent;
        [SerializeField] private GameObject gold, green;
        [SerializeField] private TMP_Text rewardText, progressText;
        [SerializeField] private Button closeButton;
        [SerializeField] private RecoveredCollectList listPrefab;
        [SerializeField] private RecoveredSafeArea safeArea;
        [SerializeField] private string progressFormat;
        [SerializeField] private float fromScale, toScale, duration;
        [SerializeField] private AnimationCurve enterEase, exitEase;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private float fillWidth;
        public RecoveredCollectList List { get; private set; }
        public RectTransform Content => content;
        public RectTransform Fill => fill;
        public GameObject Gold => gold;
        public GameObject Green => green;
        public TMP_Text RewardText => rewardText;
        public TMP_Text ProgressText => progressText;
        public Button CloseButton => closeButton;
        public event Action<string> SoundRequested;
        private void Awake() => closeButton.onClick.AddListener(Close);
        public void Bind(RecoveredPlayerProgress progress, RecoveredGameplayRules config, Transform main, bool isA, int language)
        {
            player = progress; rules = config;
            GetComponent<Canvas>().worldCamera = main.GetComponent<Canvas>().worldCamera;
            safeArea.BindRootScaler(main.GetComponentInParent<CanvasScaler>());
            // Original active-prefab Awake applies Adapt before UICollectView.OnInit.
            safeArea.AdaptScreen();
            if (isA) { gold.SetActive(true); green.SetActive(false); return; }
            gold.SetActive(false); green.SetActive(true);
            rewardText.text = rules.GetCollectReward(language);
            fillWidth = fill.rect.width;
        }
        public void Show()
        {
            if (List == null) {
                // Original CreateList initializes the unparented prefab before SetParent(false).
                List = Instantiate(listPrefab);
                List.Initialize(rules, player, listParent.rect.size);
                List.transform.SetParent(listParent, false);
            }
            else List.RefreshData();
            int count = player.CollectRecords.Count;
            progressText.text = string.Format(progressFormat, count, rules.GetCollectInfos().Count);
            fill.sizeDelta = new Vector2(fillWidth * count / rules.GetCollectInfos().Count, fill.rect.height);
            // BaseUIManager.Base_ShowWindow calls BeforeShow before activating the window.
            gameObject.SetActive(true);
            content.localScale = Vector3.one * fromScale;
            RecoveredTreasureCardRunner.Run(Animate(fromScale, toScale, enterEase, false));
        }
        private void Close()
        {
            SoundRequested?.Invoke("click");
            content.localScale = Vector3.one * toScale;
            RecoveredTreasureCardRunner.Run(Animate(toScale, fromScale, exitEase, true));
        }
        private IEnumerator Animate(float from, float to, AnimationCurve ease, bool hide)
        {
            float elapsed = 0;
            while (this != null) {
                elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / duration);
                content.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, ease.Evaluate(t));
                if (t >= 1) { if (hide) gameObject.SetActive(false); yield break; }
                yield return null;
            }
        }
    }
}
