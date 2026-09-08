using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredDownWinText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float delay;
        private readonly Dictionary<GameObject,float> rewards=new Dictionary<GameObject,float>(15);
        private readonly List<RecoveredReelWait> pending=new List<RecoveredReelWait>(3);
        private float temporaryTotal;
        private int language;
        public float Total { get; private set; }
        public float TemporaryTotal=>temporaryTotal;
        public void SetTemporaryTotal(float value)=>temporaryTotal=value;
        public void ShowAmountOnly(float value)=>label.text=RecoveredCurrency.Format(value,language,2);
        public TextMeshProUGUI Label=>label;
        public Exception Error { get; private set; }
        public event Action<float> Changed;
        public void Bind(int languageType) { Cancel();language=languageType;Started(); }
        public void Started() { label.text="GOOD LUCK"; }
        // CheckPlayBonusAnim clears the accumulator/map, not DownWinCount or its label.
        public void BeginScan() { temporaryTotal=0;rewards.Clear(); }
        public void Register(GameObject effect,float reward) { rewards.Add(effect,reward); }
        public void PresentationFinished(GameObject effect)
        {
            if(effect==null)return;
            RecoveredReelWait wait=null;
            wait=RecoveredReelWait.Delay(delay,()=>{
                pending.Remove(wait);
                // Native reads the dictionary after the delay, not a captured amount.
                rewards.TryGetValue(effect,out float amount);
                temporaryTotal+=amount;Total=temporaryTotal;
                label.text=RecoveredCurrency.Format(Total,language);
                Changed?.Invoke(Total);
            },error=>{pending.Remove(wait);Error=error;});
            pending.Add(wait);
        }
        public void Cancel() { foreach(var wait in pending)wait.Cancel();pending.Clear();rewards.Clear();Error=null; }
        private void OnDisable()=>Cancel();
    }
}
