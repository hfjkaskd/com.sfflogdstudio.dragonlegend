using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredMainUtility : MonoBehaviour
    {
        [SerializeField] private Button helpButton, settingsButton;
        [SerializeField] private RecoveredUtilityWindow helpPrefab, settingsPrefab, privacyPrefab;
        private GameEntry game;
        public RecoveredUtilityWindow Help { get; private set; }
        public RecoveredUtilityWindow Settings { get; private set; }
        public RecoveredUtilityWindow Privacy { get; private set; }
        public Button HelpButton => helpButton;
        public Button SettingsButton => settingsButton;
        public event Action<Component> WindowShowRequested;
        private void Awake(){helpButton.onClick.AddListener(OpenHelp);settingsButton.onClick.AddListener(OpenSettings);}
        public void Bind(GameEntry context) => game=context;
        private RecoveredUtilityWindow Create(RecoveredUtilityWindow prefab)
        {
            var window=Instantiate(prefab,game.CoreRound.PopupRoot,false);
            window.Bind(game,this);return window;
        }
        private void Show(RecoveredUtilityWindow window)
        {
            game.CoreAudio?.Manager.PlaySound("click");
            if(window.gameObject.activeSelf)return;
            WindowShowRequested?.Invoke(window);window.Show();
        }
        public void OpenHelp(){if(Settings!=null)Settings.Hide();if(Help==null)Help=Create(helpPrefab);Show(Help);}
        public void OpenSettings(){if(Settings==null)Settings=Create(settingsPrefab);Show(Settings);}
        public void OpenPrivacy(){if(Settings!=null)Settings.Hide();if(Privacy==null)Privacy=Create(privacyPrefab);Show(Privacy);}
        private void OnDestroy(){if(Help!=null)Destroy(Help.gameObject);if(Settings!=null)Destroy(Settings.gameObject);if(Privacy!=null)Destroy(Privacy.gameObject);}
    }
}
