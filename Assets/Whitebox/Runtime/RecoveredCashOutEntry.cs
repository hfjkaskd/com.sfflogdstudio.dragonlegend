using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashOutEntry : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform fingerTarget;
        private Action open;
        public Button Button=>button;
        public RectTransform FingerTarget=>fingerTarget;
        public void Bind(Action show){Unbind();open=show;button.onClick.AddListener(Click);}
        private void Click()=>open?.Invoke();
        public void Unbind(){button.onClick.RemoveListener(Click);open=null;}
        private void OnDestroy()=>Unbind();
    }
}
