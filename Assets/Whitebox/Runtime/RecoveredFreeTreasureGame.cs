using System;
using System.Collections;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // UIMainView.CheckSmallGame branch 2 and DisplayClass100_0 callbacks 2/3.
    public sealed class RecoveredFreeTreasureGame : MonoBehaviour
    {
        [SerializeField] private RectTransform icon;
        [SerializeField] private RecoveredTreasureWindow window;
        [SerializeField] private RecoveredTreasureDeparture departure;
        [SerializeField] private Vector3 entryScale, arrivalScale;
        [SerializeField] private float flightDuration, arcHeightRatio;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        public bool IsRunning { get; private set; }
        public int SelectedId { get; private set; }
        public RectTransform Icon => icon;
        public RecoveredTreasureWindow Window => window;
        public RecoveredTreasureDeparture Departure => departure;

        public void Bind(RecoveredPlayerProgress progress, RecoveredGameplayRules gameplayRules, IAdFacade ads,
            RecoveredCashFlightPresenter cash, Transform main, bool isA, int language, RectTransform collectionDestination)
        {
            player = progress; rules = gameplayRules;
            window.Bind(progress, gameplayRules, ads, cash, main, isA, language);
            departure.Bind(window, main, collectionDestination);
        }
        public void Begin(Vector3 source, Action<float> completed)
        {
            IsRunning = true;
            icon.position = source; icon.localScale = entryScale; icon.gameObject.SetActive(true);
            int id = rules.RandomCollectIndex();
            // Native compares the returned ID directly to the stored ordinal; retain that quirk.
            while (id == player.RandomIndex) id = rules.RandomCollectIndex();
            SelectedId = id;
            player.SetCollectData(id, 1);
            window.Show(id, value => { IsRunning = false; completed?.Invoke(value); }, target => {
                // Both endpoints are captured by the original Fly call. Position is registered first.
                RecoveredTreasureCardRunner.Run(Fly(source, target.position));
                RecoveredTreasureCardRunner.Run(Scale());
            });
        }
        private IEnumerator Fly(Vector3 start, Vector3 end)
        {
            Vector3 control = (start + end) * .5f + Vector3.up * (Vector3.Distance(start, end) * arcHeightRatio);
            float elapsed = 0;
            while (this != null && icon != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flightDuration), eased = InOutSine(t), inverse = 1 - eased;
                icon.position = inverse * inverse * start + 2 * inverse * eased * control + eased * eased * end;
                if (t >= 1) { window.RefreshCollectCard(icon.gameObject); yield break; }
                yield return null;
            }
        }
        private IEnumerator Scale()
        {
            if (this == null || icon == null) yield break;
            Vector3 start = icon.localScale;
            float elapsed = 0;
            while (this != null && icon != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flightDuration);
                // Independent scale tween still writes after the position callback hides/reset the icon.
                icon.localScale = Vector3.LerpUnclamped(start, arrivalScale, InOutSine(t));
                if (t >= 1) yield break;
                yield return null;
            }
        }
        private static float InOutSine(float t) => (1 - Mathf.Cos(Mathf.PI * t)) * .5f;
    }
}
