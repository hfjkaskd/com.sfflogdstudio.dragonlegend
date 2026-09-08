using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Numeric reward display from LuckySpinColumn, not the main gameplay reels.
    public sealed class RecoveredLuckySpinColumn : MonoBehaviour
    {
        [SerializeField] private RectTransform rotNode;
        [SerializeField] private Text[] digitTexts;
        [SerializeField] private float itemHeight,maxSpeed,accelTime,minSnapTime,maxSnapTime,bounceHeight,bounceUpTime,bounceDownTime;
        private RectTransform[] textRects;
        private int baseDigit;
        private float currentSpeed,accelElapsed,snapElapsed,snapDuration,snapDistance,traveled;
        private bool accelerating,stopping,snapping,scrollDone;
        private Coroutine speedCoroutine;
        private Action bounceCall;
        private readonly List<Bounce> bounces=new List<Bounce>(2);
        private sealed class Bounce { public float elapsed,from; public bool returning; }
        private static readonly string[] Numbers=CreateNumbers();
        private static string[] CreateNumbers(){var values=new string[21];for(int i=0;i<21;i++)values[i]=(i-10).ToString(CultureInfo.InvariantCulture);return values;}
        public bool IsDot {get;private set;}
        public bool IsStopped {get;private set;}
        public bool IsSpinning {get;private set;}
        public int TargetDigit {get;private set;}
        public int ColumnIndex {get;private set;}
        public RectTransform RotNode=>rotNode;
        public Text Digit(int index)=>digitTexts[index];
        public float CurrentSpeed=>currentSpeed;
        private void Awake()
        {
            textRects=new RectTransform[digitTexts.Length];
            for(int i=0;i<digitTexts.Length;i++)if(digitTexts[i]!=null)textRects[i]=digitTexts[i].rectTransform;
        }
        public void Initialize(int column,int number,bool dot=false)
        {
            KillTweens();ColumnIndex=column;IsDot=dot;IsStopped=false;IsSpinning=false;stopping=false;currentSpeed=0;
            for(int i=0;i<digitTexts.Length;i++)if(digitTexts[i]!=null)digitTexts[i].gameObject.SetActive(!dot);
            if(dot)return;
            baseDigit=number;Reposition();SetY(0);
        }
        public void SetTarget(int digit)=>TargetDigit=digit;
        public void StartSpin()
        {
            if(IsSpinning||IsDot)return;
            IsStopped=false;IsSpinning=true;stopping=false;currentSpeed=0;accelElapsed=0;accelerating=true;
        }
        public void StopRoll(Action<RecoveredLuckySpinColumn> completed,Action bounce)
        {
            bounceCall=bounce;
            if(IsDot){IsStopped=true;IsSpinning=false;completed?.Invoke(this);return;}
            stopping=true;StartCoroutine(WaitUntilDone(completed));
        }
        private IEnumerator WaitUntilDone(Action<RecoveredLuckySpinColumn> completed)
        {yield return new WaitWhile(()=>IsSpinning);completed?.Invoke(this);}
        public void StopImmediate(){stopping=true;IsStopped=true;IsSpinning=false;KillTweens();SetY(0);}
        private void KillTweens()
        {accelerating=false;snapping=false;if(speedCoroutine!=null){StopCoroutine(speedCoroutine);speedCoroutine=null;}}
        private void OnDestroy()=>KillTweens();
        private void Update()
        {
            float delta=Time.deltaTime;bool snapAtStart=snapping;
            // Untracked native bounce sequences survive Init/StopImmediate; preserve them.
            for(int i=0;i<bounces.Count;i++) {
                var bounce=bounces[i];bounce.elapsed+=delta;
                float t=Mathf.Clamp01(bounce.elapsed/(bounce.returning?bounceDownTime:bounceUpTime));
                float ease=bounce.returning?t*t:1-(1-t)*(1-t);
                SetY(Mathf.LerpUnclamped(bounce.from,bounce.returning?0:bounceHeight,ease));
                if(t<1)continue;
                if(!bounce.returning){bounce.returning=true;bounce.from=rotNode.anchoredPosition.y;bounce.elapsed=0;}
                else {bounces.RemoveAt(i);i--;}
            }
            if(accelerating) {
                accelElapsed+=delta;float t=Mathf.Clamp01(accelElapsed/accelTime);
                currentSpeed=maxSpeed*(1-Mathf.Cos(t*Mathf.PI*.5f)); // Ease.InSine=2
                Move(currentSpeed*delta);
                if(t>=1){accelerating=false;speedCoroutine=StartCoroutine(ConstantSpeed());}
            }
            if(snapAtStart&&snapping) {
                snapElapsed+=delta;float t=Mathf.Clamp01(snapElapsed/snapDuration);
                float next=snapDistance*(1-Mathf.Pow(1-t,3)); // Ease.OutCubic=9
                Move(next-traveled);traveled=next;
                if(t>=1){snapping=false;SetY(0);scrollDone=true;}
            }
        }
        private IEnumerator ConstantSpeed()
        {
            while(!stopping){Move(currentSpeed*Time.deltaTime);yield return null;}
            yield return StartCoroutine(Decelerate());
        }
        private IEnumerator Decelerate()
        {
            int cycle=ColumnIndex==1||ColumnIndex==2?11:10;
            int steps=(TargetDigit-baseDigit+cycle)%cycle;float y=rotNode.anchoredPosition.y;
            if(Mathf.Abs(y)>.5f&&steps==0)steps=cycle;
            snapDistance=y+itemHeight*steps+.5f;
            if(snapDistance<3){SetY(0);currentSpeed=0;IsStopped=true;IsSpinning=false;yield break;}
            snapDuration=Mathf.Clamp(snapDistance*1.5f/currentSpeed,minSnapTime,maxSnapTime);
            IsStopped=true;traveled=0;snapElapsed=0;scrollDone=false;snapping=true;
            yield return new WaitWhile(()=>!scrollDone);
            bounces.Add(new Bounce{from=rotNode.anchoredPosition.y});bounceCall?.Invoke();
            currentSpeed=0;IsSpinning=false;
        }
        private void Move(float distance)
        {
            float y=rotNode.anchoredPosition.y-distance;
            int cycle=ColumnIndex==0||ColumnIndex==3?10:11;
            while(y < -itemHeight){baseDigit=(baseDigit+1)%cycle;y+=itemHeight;}
            SetY(y);Reposition();
        }
        private void SetY(float y){var position=rotNode.anchoredPosition;position.y=y;rotNode.anchoredPosition=position;}
        private void Reposition()
        {
            int cycle=ColumnIndex==1||ColumnIndex==2?11:10;
            for(int i=0;i<digitTexts.Length;i++)if(digitTexts[i]!=null) {
                int value=(baseDigit+i)%cycle;
                digitTexts[i].text=cycle==11&&value==10?".":Numbers[value+10];
                textRects[i].anchoredPosition=new Vector2(0,itemHeight*i);
            }
        }
    }
}
