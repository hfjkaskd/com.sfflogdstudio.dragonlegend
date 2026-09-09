using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredTreasureCard : MonoBehaviour
    {
        [SerializeField] private GameObject cardBack, cardFront;
        [SerializeField] private float flipHalfDuration;
        [SerializeField] private AnimationCurve closeCurve, openCurve;
        public bool IsFlipped { get; private set; }
        public float Reward => 0; // Native _reward is never assigned by Init or Flip.
        public GameObject Back => cardBack;
        public GameObject Front => cardFront;
        public float HalfDuration => flipHalfDuration;
        public event Action<RecoveredTreasureCard> OnFlipComplete;
        public event Action<string> SoundRequested;

        // 23db8c4: index is unused; neither Init nor SetFlipped cancels an active sequence.
        public void Init(int collectIndex)
        {
            SetFlipped(false);
            transform.localScale = Vector3.one;
        }
        public void SetFlipped(bool flipped)
        {
            IsFlipped = flipped;
            cardBack.SetActive(!flipped);
            cardFront.SetActive(flipped);
        }
        public void Flip(Action onComplete = null)
        {
            if (IsFlipped) return;
            IsFlipped = true;
            SoundRequested?.Invoke("cardReavel"); // ELF string relocation at 04f1eb80.
            RecoveredTreasureCardRunner.Run(Animate(onComplete));
        }
        private IEnumerator Animate(Action onComplete)
        {
            // A global runner preserves native tween updates while the card is disabled.
            // The first transform read occurs at startup, rather than at Flip allocation.
            if (this == null) yield break;
            float from = transform.localScale.x, elapsed = 0;
            bool swapped = false;
            while (this != null)
            {
                elapsed += Time.deltaTime;
                if (elapsed < flipHalfDuration)
                    SetScaleX(from * (1 - closeCurve.Evaluate(elapsed / flipHalfDuration)));
                else
                {
                    if (!swapped)
                    {
                        SetScaleX(0);
                        cardBack.SetActive(false); cardFront.SetActive(true);
                        swapped = true;
                    }
                    float t = Mathf.Clamp01((elapsed - flipHalfDuration) / flipHalfDuration);
                    SetScaleX(openCurve.Evaluate(t));
                    if (t >= 1)
                    {
                        OnFlipComplete?.Invoke(this);
                        onComplete?.Invoke();
                        yield break;
                    }
                }
                yield return null;
            }
        }
        private void SetScaleX(float value)
        {
            var scale = transform.localScale; scale.x = value; transform.localScale = scale;
        }
    }

    // Runtime animation service only; no UI is constructed here.
    internal sealed class RecoveredTreasureCardRunner : MonoBehaviour
    {
        private static RecoveredTreasureCardRunner instance;
        private readonly List<IEnumerator> pending = new List<IEnumerator>(2);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => instance = null;
        internal static void EnsureCreated()
        {
            if (instance == null)
            {
                var host = new GameObject("Treasure card animations");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<RecoveredTreasureCardRunner>();
            }
        }
        internal static void Run(IEnumerator animation)
        {
            EnsureCreated();
            instance.pending.Add(animation);
        }
        private void Update()
        {
            // Newly scheduled sequences wait until the following Update.
            int count = pending.Count;
            for (int i = 0; i < count; i++)
            {
                bool active = false;
                try { active = pending[i].MoveNext(); }
                catch (Exception error) { Debug.LogException(error); }
                if (active) continue;
                (pending[i] as IDisposable)?.Dispose();
                pending.RemoveAt(i); i--; count--;
            }
        }
    }
}
