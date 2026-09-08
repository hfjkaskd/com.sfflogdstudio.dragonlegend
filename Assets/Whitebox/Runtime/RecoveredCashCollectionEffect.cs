using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashCollectionEffect:MonoBehaviour
    {
        [SerializeField] private RecoveredRegionAnimator animator;
        private Action finish;
        public event Action<RecoveredCashCollectionEffect> Completed;
        private void Awake()=>finish=Finish;
        public void Begin(Transform target)
        {
            transform.SetParent(target,false);transform.localPosition=Vector3.zero;
            transform.localRotation=Quaternion.identity;transform.localScale=Vector3.one;
            animator.PlayOnce(0,finish);
        }
        private void Finish()=>Completed?.Invoke(this);
    }
}
