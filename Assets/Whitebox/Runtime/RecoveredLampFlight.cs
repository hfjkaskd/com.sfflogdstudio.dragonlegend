using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredLampFlight : MonoBehaviour
    {
        public enum Curve { InOutSine, InQuad }
        [SerializeField] private Curve curve;
        [SerializeField] private float duration;
        [SerializeField] private float automaticArcRatio;
        [SerializeField] private int sortingOffset;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private SortingGroup sortingGroup;
        private Vector3 start, control, end;
        private float elapsed;
        public bool IsFlying { get; private set; }
        public Transform Destination { get; private set; }
        public Vector3 StartPoint => start;
        public Vector3 ControlPoint => control;
        public Vector3 EndPoint => end;
        public event Action<RecoveredLampFlight> Arrived;
        public void Begin(Transform destination, int canvasOrder)
        {
            Destination = destination;
            int order = canvasOrder + sortingOffset;
            sortingGroup.sortingOrder = order;
            for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder = order;
            start = transform.position; end = destination.position;
            control = (start + end) * .5f + Vector3.up * (Vector3.Distance(start, end) * automaticArcRatio);
            elapsed = 0; IsFlying = true;
        }
        private void Update()
        {
            if (!IsFlying) return;
            elapsed += Time.deltaTime;
            float t = duration == 0 ? 1 : Mathf.Clamp01(elapsed / duration);
            float eased = curve == Curve.InQuad ? t * t : (1 - Mathf.Cos(Mathf.PI * t)) * .5f;
            float inverse = 1 - eased;
            transform.position = start * (inverse * inverse) + control * (eased * (inverse + inverse)) + end * (eased * eased);
            if (t < 1) return;
            IsFlying = false; Arrived?.Invoke(this);
        }
        private void OnDisable() { IsFlying = false; }
    }
}
