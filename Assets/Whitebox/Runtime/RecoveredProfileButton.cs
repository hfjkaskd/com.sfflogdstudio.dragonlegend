using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredProfileButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameEntry entry;
        [SerializeField] private LaunchProfile profile;
        private void OnEnable()=>button.onClick.AddListener(Select);
        private void OnDisable()=>button.onClick.RemoveListener(Select);
        private void Select()=>entry.Select(profile);
    }
}
