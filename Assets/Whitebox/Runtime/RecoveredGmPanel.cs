using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Local test controls; authored separately from the recovered game interface.
    public sealed class RecoveredGmPanel : MonoBehaviour
    {
        [SerializeField] private Button toggle;
        [SerializeField] private Button[] profileButtons;
        [SerializeField] private CanvasGroup[] groups;
        [SerializeField] private Text toggleLabel;
        [SerializeField] private string closedCaption,openCaption;
        public bool IsOpen {get;private set;}
        public Button ToggleButton=>toggle;
        private void OnEnable()
        {
            toggle.onClick.AddListener(Toggle);
            foreach(var button in profileButtons)button.onClick.AddListener(Close);
            Close();
        }
        private void OnDisable()
        {
            toggle.onClick.RemoveListener(Toggle);
            foreach(var button in profileButtons)button.onClick.RemoveListener(Close);
        }
        private void Toggle()=>SetOpen(!IsOpen);
        private void Close()=>SetOpen(false);
        private void SetOpen(bool value)
        {
            IsOpen=value;
            foreach(var group in groups){group.alpha=value?1:0;group.interactable=value;group.blocksRaycasts=value;}
            toggleLabel.text=value?openCaption:closedCaption;
        }
    }
}
