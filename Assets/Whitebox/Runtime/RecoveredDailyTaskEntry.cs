using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredDailyTaskEntry:MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RecoveredDailyTaskWindow windowPrefab;
        private GameEntry game;
        public RecoveredDailyTaskWindow Window{get;private set;}
        public Button Button=>button;
        public event Action<Component> WindowShowRequested;
        private void Awake()=>button.onClick.AddListener(Open);
        public void Bind(GameEntry context){game=context;game.PlayerProgress.InitializeDailyTasks(DateTime.Now);}
        private void Update(){if(Window!=null)Window.AdvanceTimer(Time.deltaTime);}
        private void Open()
        {
            if(Window==null){Window=Instantiate(windowPrefab,game.CoreRound.PopupRoot,false);Window.Bind(game);Window.WindowShowRequested+=Forward;}
            Window.Show();
        }
        private void Forward(Component view)=>WindowShowRequested?.Invoke(view);
        private void OnDestroy(){if(Window!=null)Destroy(Window.gameObject);}
    }
}
