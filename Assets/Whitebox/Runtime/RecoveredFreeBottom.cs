using TMPro;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredFreeBottom : MonoBehaviour
    {
        [SerializeField] private GameObject main, free;
        [SerializeField] private TextMeshProUGUI count;
        [SerializeField] private string countFormat;
        private RecoveredPlayerProgress player;
        private RecoveredFreeSpinEntry entry;
        public GameObject Main => main;
        public GameObject Free => free;
        public TextMeshProUGUI Count => count;
        public void Bind(RecoveredPlayerProgress progress, RecoveredFreeSpinEntry spinEntry)
        {
            Unbind();player=progress;entry=spinEntry;
            entry.PresentationRequested+=RefreshCount;
            Apply(player.GameSlotType);
        }
        // SetInitShow only switches Main/Free; InitFreeSpinTimes is a separate call.
        public void Apply(RecoveredSlotType mode)
        {
            bool isBase=mode==RecoveredSlotType.Base;
            main.SetActive(isBase);free.SetActive(!isBase);
        }
        public void RefreshCount()=>count.text=string.Format(countFormat,player.FreeSpinCount);
        public void Unbind()
        {
            if(entry!=null)entry.PresentationRequested-=RefreshCount;
            player=null;entry=null;
        }
        private void OnDestroy()=>Unbind();
    }
}
