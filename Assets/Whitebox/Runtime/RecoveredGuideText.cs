using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // GuideTxtAnim.SetTxt 2372468 / PlayTxtAnim.MoveNext 2372bd4.
    public sealed class RecoveredGuideText : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float charsPerSecond;
        [SerializeField] private string firstSpinText,extraWildText;
        private string currentText=string.Empty;
        private Coroutine latest;
        public TMP_Text Label=>label;
        public void SetText(int step,Action completed=null)
        {
            if(step==1)currentText=firstSpinText;
            else if(step==2)currentText=extraWildText;
            // Source does not cancel a previously started coroutine here.
            latest=StartCoroutine(Reveal(currentText,completed));
        }
        private IEnumerator Reveal(string text,Action completed)
        {
            label.text=string.Empty;
            float interval=1/charsPerSecond;
            for(int index=0;index<text.Length;index++)
            {
                label.text=text.Substring(0,index+1);
                yield return new WaitForSeconds(interval);
            }
            completed?.Invoke();
        }
        // Guide.OnClickBgMask stops only the latest handle and does not call completion.
        public void RevealAll()
        {
            if(latest!=null){StopCoroutine(latest);latest=null;}
            label.text=currentText;
        }
        public void Cancel(){StopAllCoroutines();latest=null;}
        private void OnDestroy()=>Cancel();
    }
}
