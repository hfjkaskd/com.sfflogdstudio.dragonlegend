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
        private bool isA;
        private int language;
        public Button Button => button;
        public RectTransform Destination => destination;
        public RecoveredCollectWindow Window { get; private set; }
        public event Action<string> SoundRequested;
        private void Awake() => button.onClick.AddListener(Open);
        public void Bind(RecoveredPlayerProgress progress, RecoveredGameplayRules config, Transform mainWindow, bool profileA, int languageType)
        {
            player = progress; rules = config; main = mainWindow; isA = profileA; language = languageType;
        }
        private void Open()
        {
            Sound("click");
            if (Window == null) {
                Window = Instantiate(windowPrefab, main, false);
                Window.Bind(player, rules, main, isA, language);
                Window.SoundRequested += Sound;
            }
            Window.Show();
        }
        private void Sound(string name) => SoundRequested?.Invoke(name);
        private void OnDestroy()
        {
            if (Window != null) { Window.SoundRequested -= Sound; Destroy(Window.gameObject); Window = null; }
        }
    }
}
