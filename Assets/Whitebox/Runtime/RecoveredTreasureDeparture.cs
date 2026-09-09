using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // UIMainView.FlyTreasureCard 23ba190; completion 23c2fe4 only despawns.
    public sealed class RecoveredTreasureDeparture : MonoBehaviour
    {
        [SerializeField] private Image cardPrefab;
        private RectTransform destination;
        [SerializeField] private float duration, arcHeightRatio;
        [SerializeField] private Vector3 initialScale, endScale;
        [SerializeField] private AnimationCurve scaleEase;
        private Transform main;
        private RecoveredTreasureWindow window;
        private ObjectPool<Image> pool;
        private readonly List<Flight> flights = new List<Flight>(2);
        private sealed class Flight { public Image image; public bool cancelled; }
        public RectTransform Destination => destination;
        public int ActiveCardCount => flights.Count;
        public int CreatedCardCount => pool == null ? 0 : pool.CountAll;
        public Image ActiveCardAt(int index) => flights[index].image;

        public void Bind(RecoveredTreasureWindow treasureWindow, Transform mainWindow, RectTransform mainDestination)
        {
            Unbind(); main = mainWindow; window = treasureWindow; destination = mainDestination;
            pool = new ObjectPool<Image>(Create, null, Release, DestroyCard);
            window.CollectCardDepartureRequested += Begin;
        }
        private Image Create() => Instantiate(cardPrefab, transform, false);
        private void Release(Image image) { image.gameObject.SetActive(false); image.transform.SetParent(transform, false); }
        private void DestroyCard(Image image) { if (image != null) Destroy(image.gameObject); }
        public void Begin(Vector3 source, Sprite sprite)
        {
            var image = pool.Get(); image.sprite = sprite; image.SetNativeSize();
            image.transform.SetParent(main, false); image.transform.localScale = initialScale;
            image.transform.position = source; image.gameObject.SetActive(true);
            var flight = new Flight { image = image }; flights.Add(flight);
            RecoveredTreasureCardRunner.Run(Animate(flight, source, destination.position));
        }
        private IEnumerator Animate(Flight flight, Vector3 start, Vector3 end)
        {
            if (this == null || flight.cancelled || flight.image == null) yield break;
            var target = flight.image.transform;
            Vector3 scale = target.localScale;
            Vector3 control = (start + end) * .5f + Vector3.up * (Vector3.Distance(start, end) * arcHeightRatio);
            float elapsed = 0;
            while (this != null && !flight.cancelled && target != null)
            {
                elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / duration);
                // Native registers scale first, then the position tween with its despawn callback.
                target.localScale = Vector3.LerpUnclamped(scale, endScale, scaleEase.Evaluate(t));
                float eased = (1 - Mathf.Cos(Mathf.PI * t)) * .5f, inverse = 1 - eased;
                target.position = inverse * inverse * start + 2 * inverse * eased * control + eased * eased * end;
                if (t >= 1) { flights.Remove(flight); pool.Release(flight.image); yield break; }
                yield return null;
            }
        }
        public void Unbind()
        {
            if (window != null) window.CollectCardDepartureRequested -= Begin;
            window = null;
            foreach (var flight in flights) { flight.cancelled = true; if (flight.image != null) pool.Release(flight.image); }
            flights.Clear(); pool?.Dispose(); pool = null; main = null;
        }
        private void OnDestroy() => Unbind();
    }
}
