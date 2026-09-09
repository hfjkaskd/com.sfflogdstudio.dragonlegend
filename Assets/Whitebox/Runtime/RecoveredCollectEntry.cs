using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Main's Treasure button and TreasureRect share this single authored icon.
    public sealed class RecoveredCollectEntry : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform destination;
        [SerializeField] private RecoveredCollectWindow windowPrefab;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private Transform main;
        private Transform windowRoot;
        private bool isA;
        private int language;
        public Button Button => button;
        public RectTransform Destination => destination;
        public RecoveredCollectWindow Window { get; private set; }
        public event Action<string> SoundRequested;
        public event Action<Component> WindowShowRequested;
        private void Awake() => button.onClick.AddListener(Open);
        public void Bind(RecoveredPlayerProgress progress, RecoveredGameplayRules config, Transform mainWindow, bool profileA, int languageType,Transform popupRoot=null)
        {
            player = progress; rules = config; main = mainWindow; isA = profileA; language = languageType;
            windowRoot=popupRoot!=null?popupRoot:mainWindow;
        }
        private void Open()
        {
            Sound("click");
            if(Window!=null&&Window.gameObject.activeSelf)return;
            if (Window == null) {
                Window = Instantiate(windowPrefab, windowRoot, false);
                Window.Bind(player, rules, main, isA, language);
                Window.SoundRequested += Sound;
            }
            WindowShowRequested?.Invoke(Window);Window.Show();
        }
        private void Sound(string name) => SoundRequested?.Invoke(name);
        private void OnDestroy()
        {
            if (Window != null) { Window.SoundRequested -= Sound; Destroy(Window.gameObject); Window = null; }
        }
    }
}
