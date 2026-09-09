using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // UITipsView 23daf80 / CloseWindow 23db094, Update (PlayerLoopTiming 8).
    public sealed class RecoveredTipsWindow:MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float visibleSeconds;
        private readonly List<RecoveredReelWait> waits=new List<RecoveredReelWait>(1);
        public TMP_Text Label=>label;
        public void Bind(Camera camera)=>GetComponent<Canvas>().worldCamera=camera;
        public void Show(string text)
        {
            label.text=text;gameObject.SetActive(true);RecoveredReelWait wait=null;
            wait=RecoveredReelWait.Delay(visibleSeconds,()=>{waits.Remove(wait);gameObject.SetActive(false);},error=>Debug.LogException(error,this));
            waits.Add(wait);
        }
        public void Cancel(){foreach(var wait in waits)wait.Cancel();waits.Clear();gameObject.SetActive(false);}
        private void OnDestroy(){foreach(var wait in waits)wait.Cancel();waits.Clear();}
    }
}
