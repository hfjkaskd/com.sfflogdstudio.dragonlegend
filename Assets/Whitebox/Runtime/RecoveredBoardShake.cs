using System.Collections;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredBoardShake : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float duration,intensity,frequency;
        [SerializeField] private AnimationCurve falloff;
        private Vector2 originalPosition;
        private bool initialized;
        private Coroutine routine;
        private static RecoveredBoardShake current;
        public bool IsShaking { get; private set; }
        public RectTransform Target=>target;
        public Vector2 OriginalPosition=>originalPosition;
        public void Initialize() {Cancel();originalPosition=target.anchoredPosition;initialized=true;}
        public void Begin()
        {
            if(!initialized)Initialize();
            if(current!=null)current.Stop(false);
            current=this;IsShaking=true;routine=StartCoroutine(Run());
        }
        private IEnumerator Run()
        {
            float remaining=duration;
            while(remaining>0) {
                remaining-=Time.deltaTime;
                float weight=falloff.Evaluate(1-remaining/duration);
                target.anchoredPosition=originalPosition+new Vector2(
                    (Mathf.PerlinNoise(Time.time*frequency,0)*2-1)*intensity*weight,
                    (Mathf.PerlinNoise(0,Time.time*frequency)*2-1)*intensity*weight);
                yield return null;
            }
            target.anchoredPosition=originalPosition;IsShaking=false;routine=null;
            if(current==this)current=null;
        }
        private void Stop(bool restore)
        {
            if(routine!=null)StopCoroutine(routine);routine=null;IsShaking=false;
            if(restore&&initialized&&target!=null)target.anchoredPosition=originalPosition;
            if(current==this)current=null;
        }
        public void Cancel()=>Stop(true);
        private void OnDisable()=>Cancel();
    }
}
