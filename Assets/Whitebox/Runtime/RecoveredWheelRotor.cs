using System;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredWheelRotor : MonoBehaviour
    {
        [SerializeField] private RecoveredWheelItem itemPrefab;
        [SerializeField] private RectTransform[] itemAnchors;
        [SerializeField] private float spinDuration, segmentAngle;
        [SerializeField] private int extraRotations;
        [SerializeField] private AnimationCurve spinEase;
        private ObjectPool<RecoveredWheelItem> pool;
        private RecoveredWheelItem[] items;
        private RecoveredGameplayRules rules;
        private Action<RecoveredWheelType, float> completed;
        private float elapsed, endAngle;
        public int ResultIndex { get; private set; }
        public bool IsSpinning { get; private set; }
        public float Rotation { get; private set; }
        public float Duration => spinDuration;
        public RecoveredWheelItem Item(int index) => items[index];
        public event Action<string> SoundRequested;

        private void Awake()
        {
            pool = new ObjectPool<RecoveredWheelItem>(() => Instantiate(itemPrefab),
                actionOnDestroy: value => Destroy(value.gameObject));
            items = new RecoveredWheelItem[itemAnchors.Length];
            for (int i = 0; i < items.Length; i++)
            {
                var item = pool.Get();
                item.transform.SetParent(itemAnchors[i], false);
                ((RectTransform)item.transform).anchoredPosition = Vector2.zero;
                items[i] = item;
            }
        }

        // Selection belongs to OnBeforeShow, before the entrance/delay. Reward
        // lookup belongs to OnSpinComplete, so it observes live configuration.
        public void Prepare(RecoveredGameplayRules gameplayRules, bool isA, int language)
        {
            rules = gameplayRules;
            var map = rules.GetWheelInfo();
            ResultIndex = rules.RandomWheelWeight();
            for (int i = 0; i < items.Length; i++) items[i].Initialize(map[i], i, rules, isA, language);
        }

        public void StartSpin(Action<RecoveredWheelType, float> onCompleted)
        {
            SoundRequested?.Invoke("wheelSpin");
            transform.localEulerAngles = Vector3.zero;
            Rotation = 0;
            endAngle = ResultIndex * -segmentAngle + extraRotations * -360f;
            elapsed = 0;
            completed = onCompleted;
            IsSpinning = true;
        }

        private void Update()
        {
            if (!IsSpinning) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spinDuration);
            Rotation = endAngle * spinEase.Evaluate(t);
            transform.localEulerAngles = new Vector3(0, 0, Rotation);
            if (t < 1) return;
            IsSpinning = false;
            var callback = completed;
            completed = null;
            var type = rules.GetWheelInfo()[ResultIndex];
            float reward = rules.GetWheelReward(ResultIndex);
            callback?.Invoke(type, reward);
        }

        private void OnDestroy()
        {
            completed = null;
            if (pool == null) return;
            for (int i = 0; i < items.Length; i++) if (items[i] != null) pool.Release(items[i]);
            pool.Dispose();
        }
    }
}
