using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Native ListView fixed-size vertical path, with UICashOutView card callbacks.
    public sealed class RecoveredCashOutList : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RecoveredCashOutItem template;
        [SerializeField] private RectTransform selectionFrame;
        [SerializeField] private Vector2 cellSize;
        [SerializeField] private Vector3 hidePosition;
        [SerializeField] private float movementThresholdSquared;
        private RecoveredGameplayRules rules;private RecoveredPlayerProgress player;private RecoveredCashOutBottom bottom;private Func<int> clock;
        private RecoveredCashOutItem[] shown;private Vector3[] positions;
        private readonly List<RecoveredCashOutItem> cache=new List<RecoveredCashOutItem>();
        private readonly List<RecoveredCashOutItem> created=new List<RecoveredCashOutItem>();
        private readonly List<int> hidden=new List<int>();
        private Vector2 viewSize;private Vector3 previousPosition;private bool dirty;
        private int selected,type,language;
        public ScrollRect Scroll=>scroll;
        public RectTransform SelectionFrame=>selectionFrame;
        public int InitialSelection=>selected;
        public int CreatedCount=>created.Count;
        public RecoveredCashOutItem ItemAt(int index)=>shown[index];
        public event Action<string> SoundRequested;
        public void Initialize(RecoveredGameplayRules config,RecoveredPlayerProgress model,RecoveredCashOutBottom panel,Func<int> utcClock,Vector2 size,int paymentType,int currencyLanguage)
        {
            rules=config;player=model;bottom=panel;clock=utcClock;type=paymentType;language=currencyLanguage;viewSize=size;
            bottom.Bind(player,rules,clock);bottom.RefreshRequested+=RefreshData;
            ((RectTransform)transform).sizeDelta=size;
            int total=rules.GetCashOutCount();shown=new RecoveredCashOutItem[total];positions=new Vector3[total];hidden.Capacity=total;
            for(int i=0;i<total;i++)positions[i]=new Vector3(cellSize.x*.5f,-(i+.5f)*cellSize.y,0);
            scroll.content.sizeDelta=new Vector2(Mathf.Max(size.x,cellSize.x),Mathf.Max(size.y,Mathf.Max(1,total)*cellSize.y));
            template.transform.localPosition=hidePosition;RefreshData();
        }
        public void RefreshData()
        {
            selected=player.FindCashOutWindowSelection(out bool hide);if(hide)selectionFrame.gameObject.SetActive(false);
            dirty=true;if(selected>=0)bottom.Initialize(selected,type,language);
        }
        public void RefreshPaymentType(int paymentType)
        {
            type=paymentType;
            // Existing card event listeners include cached, still-active objects.
            foreach(var item in created)item.RefreshPaymentType(type);
            bottom.Initialize(selected,type,language);
        }
        private void Select(int index,RectTransform frame)
        {
            foreach(var item in created)item.RefreshSelection(index,frame);
            bottom.Initialize(index,type,language);
            // Native RefreshCashOutItemSelectKuang does not assign selectIndex.
        }
        private void Sound(string value)=>SoundRequested?.Invoke(value);
        private void RefreshItem(RecoveredCashOutItem item,int index)=>item.Initialize(index,selected,type,selectionFrame,language);
        private void Update()
        {
            if(shown==null)return;Vector3 position=scroll.content.localPosition;
            if((position-previousPosition).sqrMagnitude>movementThresholdSquared)previousPosition=position;else if(!dirty)return;
            hidden.Clear();
            for(int i=0;i<shown.Length;i++)
            {
                if(shown[i]==null)hidden.Add(i);
                else if(!Visible(i,position)){shown[i].transform.localPosition=hidePosition;cache.Add(shown[i]);shown[i]=null;}
                else if(dirty)RefreshItem(shown[i],i);
            }
            foreach(int index in hidden)
            {
                if(!Visible(index,position))continue;RecoveredCashOutItem item;
                if(cache.Count==0)
                {
                    item=Instantiate(template,scroll.content,false);item.transform.localScale=Vector3.one;item.Bind(player,rules,clock);
                    item.SelectionRequested+=Select;item.SoundRequested+=Sound;item.CountdownCompleted+=RefreshData;created.Add(item);
                }
                else{item=cache[0];cache.RemoveAt(0);}
                shown[index]=item;if(index==0||shown[index-1]==null)item.transform.SetAsFirstSibling();else item.transform.SetAsLastSibling();
                item.transform.localPosition=positions[index];RefreshItem(item,index);
            }
            dirty=false;
        }
        private bool Visible(int index,Vector3 position)
        {
            Vector2 center=viewSize*.5f,half=cellSize*.5f;
            return !(center.x+half.x<Mathf.Abs(position.x+positions[index].x-center.x)||center.y+half.y<Mathf.Abs(position.y+positions[index].y+center.y));
        }
        private void OnDestroy()
        {
            if(bottom!=null)bottom.RefreshRequested-=RefreshData;
            foreach(var item in created)if(item!=null){item.SelectionRequested-=Select;item.SoundRequested-=Sound;item.CountdownCompleted-=RefreshData;item.Cancel();}
        }
    }
}
