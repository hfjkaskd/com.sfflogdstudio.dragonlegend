using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredUtilityWindow : MonoBehaviour
    {
        [SerializeField] private Button close, help, contact, terms, music, sound, left, right;
        [SerializeField] private GameObject musicOn, musicOff, soundOn, soundOff;
        [SerializeField] private GameObject[] pages;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private string contactEmail;
        private GameEntry game;
        private RecoveredMainUtility owner;
        public Button CloseButton=>close;
        public Button MusicButton=>music;
        public Button SoundButton=>sound;
        public Button HelpButton=>help;
        public Button TermsButton=>terms;
        public Button LeftButton=>left;
        public Button RightButton=>right;
        public int CurrentPage{get;private set;}
        private void Awake()
        {
            close.onClick.AddListener(Close);
            if(help!=null)help.onClick.AddListener(()=>owner.OpenHelp());
            if(terms!=null)terms.onClick.AddListener(()=>owner.OpenPrivacy());
            if(contact!=null)contact.onClick.AddListener(Contact);
            if(music!=null)music.onClick.AddListener(ToggleMusic);
            if(sound!=null)sound.onClick.AddListener(ToggleSound);
            if(left!=null)left.onClick.AddListener(()=>ChangePage(-1));
            if(right!=null)right.onClick.AddListener(()=>ChangePage(1));
        }
        public void Bind(GameEntry context,RecoveredMainUtility entry)
        {game=context;owner=entry;GetComponent<Canvas>().worldCamera=game.GetComponent<Canvas>().worldCamera;}
        public void Show(){CurrentPage=0;Refresh();gameObject.SetActive(true);if(scroll!=null)scroll.verticalNormalizedPosition=1;}
        public void Hide()=>gameObject.SetActive(false);
        private void Click()=>game.CoreAudio?.Manager.PlaySound("click");
        private void Close(){Click();Hide();}
        private void ToggleMusic()
        {
            Click();game.PlayerStore.Data.IsMusic=!game.PlayerStore.Data.IsMusic;
            game.PlayerStore.Save();game.CoreAudio?.Manager.SetMusic();Refresh();
        }
        // Original Sound button controls IsVibrate (+0x39), not a separate audio flag.
        private void ToggleSound(){Click();game.PlayerStore.Data.IsVibrate=!game.PlayerStore.Data.IsVibrate;game.PlayerStore.Save();Refresh();}
        private void Contact(){Click();Application.OpenURL("mailto:"+contactEmail+"?subject="+Uri.EscapeDataString(Application.productName));Hide();}
        private void ChangePage(int delta){Click();CurrentPage=(CurrentPage+delta+pages.Length)%pages.Length;Refresh();}
        private void Refresh()
        {
            if(musicOn!=null){musicOn.SetActive(game.PlayerStore.Data.IsMusic);musicOff.SetActive(!game.PlayerStore.Data.IsMusic);}
            if(soundOn!=null){soundOn.SetActive(game.PlayerStore.Data.IsVibrate);soundOff.SetActive(!game.PlayerStore.Data.IsVibrate);}
            if(pages!=null)for(int i=0;i<pages.Length;i++)pages[i].SetActive(i==CurrentPage);
        }
    }
}
