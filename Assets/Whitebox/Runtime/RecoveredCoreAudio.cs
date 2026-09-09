using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Subscribe at the existing forwarding boundaries, once per production graph.
    public sealed class RecoveredCoreAudio : MonoBehaviour
    {
        [SerializeField] private RecoveredSoundManager audioManager;
        [SerializeField] private string reelStopSound, speedupSound, coinShowSound, coinRevealSound, lampArrivalSound, coinBurstSound;
        [SerializeField] private string clickSound, spinSound;
        private GameEntry game;
        public RecoveredSoundManager Manager => audioManager;
        public void Bind(GameEntry context) { Unbind(); game = context; audioManager.Bind(game.PlayerStore.Data); Connect(true); }
        public void Unbind() { if (game == null) return; Connect(false); audioManager.StopAll(); game = null; }
        private void Connect(bool bind)
        {
            var reels = game.Playfield.Reels;
            var coins = game.Playfield.CoinStops;
            var baseFlights = game.Playfield.WinFlight;
            var freeFlights = game.Playfield.ModeView.FreeReels.RewardCollect.Flights;
            if (bind) {
                game.SpinEntry.ClickSoundRequested += SpinClick;
                game.SpinEntry.SpinSoundRequested += SpinStart;
                reels.ReelStopSoundRequested += ReelStop;
                reels.SpeedupSoundRequested += Speedup;
                reels.SpeedupSoundStopRequested += audioManager.StopSound1;
                coins.CoinShowSoundRequested += CoinShow;
                coins.CoinRevealSoundRequested += CoinReveal;
                coins.ExpSoundRequested += LampArrival;
                baseFlights.CoinBurstSoundRequested += CoinBurst;
                freeFlights.CoinBurstSoundRequested += CoinBurst;
            } else {
                game.SpinEntry.ClickSoundRequested -= SpinClick;
                game.SpinEntry.SpinSoundRequested -= SpinStart;
                reels.ReelStopSoundRequested -= ReelStop;
                reels.SpeedupSoundRequested -= Speedup;
                reels.SpeedupSoundStopRequested -= audioManager.StopSound1;
                coins.CoinShowSoundRequested -= CoinShow;
                coins.CoinRevealSoundRequested -= CoinReveal;
                coins.ExpSoundRequested -= LampArrival;
                baseFlights.CoinBurstSoundRequested -= CoinBurst;
                freeFlights.CoinBurstSoundRequested -= CoinBurst;
            }
            var node0 = game.Playfield;
            if (bind) node0.SoundRequested += audioManager.PlaySound; else node0.SoundRequested -= audioManager.PlaySound;
            if (bind) node0.Sound1Requested += audioManager.PlaySound1; else node0.Sound1Requested -= audioManager.PlaySound1;
            if (bind) node0.PauseMusicRequested += audioManager.PauseMusic; else node0.PauseMusicRequested -= audioManager.PauseMusic;
            if (bind) node0.ResumeMusicRequested += audioManager.CtnMusic; else node0.ResumeMusicRequested -= audioManager.CtnMusic;
            if (bind) node0.StopSound1Requested += audioManager.StopSound1; else node0.StopSound1Requested -= audioManager.StopSound1;
            var node1 = game.BonusFlow;
            if (bind) node1.SoundRequested += audioManager.PlaySound; else node1.SoundRequested -= audioManager.PlaySound;
            if (bind) node1.Sound1Requested += audioManager.PlaySound1; else node1.Sound1Requested -= audioManager.PlaySound1;
            if (bind) node1.PauseMusicRequested += audioManager.PauseMusic; else node1.PauseMusicRequested -= audioManager.PauseMusic;
            if (bind) node1.StopSound1Requested += audioManager.StopSound1; else node1.StopSound1Requested -= audioManager.StopSound1;
            if (bind) node1.ChangeMusicRequested += audioManager.ChangeBGM; else node1.ChangeMusicRequested -= audioManager.ChangeBGM;
            var node2 = game.BonusFlow.Window;
            if (bind) node2.SoundRequested += audioManager.PlaySound; else node2.SoundRequested -= audioManager.PlaySound;
            if (bind) node2.Sound1Requested += audioManager.PlaySound1; else node2.Sound1Requested -= audioManager.PlaySound1;
            if (bind) node2.PauseMusicRequested += audioManager.PauseMusic; else node2.PauseMusicRequested -= audioManager.PauseMusic;
            if (bind) node2.ResumeMusicRequested += audioManager.CtnMusic; else node2.ResumeMusicRequested -= audioManager.CtnMusic;
            if (bind) node2.StopSound1Requested += audioManager.StopSound1; else node2.StopSound1Requested -= audioManager.StopSound1;
            var node3 = game.CoreRound.Entry;
            if (bind) node3.SoundRequested += audioManager.PlaySound; else node3.SoundRequested -= audioManager.PlaySound;
            if (bind) node3.Sound1Requested += audioManager.PlaySound1; else node3.Sound1Requested -= audioManager.PlaySound1;
            if (bind) node3.PauseMusicRequested += audioManager.PauseMusic; else node3.PauseMusicRequested -= audioManager.PauseMusic;
            if (bind) node3.StopSound1Requested += audioManager.StopSound1; else node3.StopSound1Requested -= audioManager.StopSound1;
            if (bind) node3.ChangeMusicRequested += audioManager.ChangeBGM; else node3.ChangeMusicRequested -= audioManager.ChangeBGM;
            var node4 = game.CoreRound.Exit;
            if (bind) node4.SoundRequested += audioManager.PlaySound; else node4.SoundRequested -= audioManager.PlaySound;
            if (bind) node4.PauseMusicRequested += audioManager.PauseMusic; else node4.PauseMusicRequested -= audioManager.PauseMusic;
            if (bind) node4.ChangeMusicRequested += audioManager.ChangeBGM; else node4.ChangeMusicRequested -= audioManager.ChangeBGM;
            var node5 = game.Playfield.ModeView.FreeReels.Controller;
            if (bind) node5.SoundRequested += audioManager.PlaySound; else node5.SoundRequested -= audioManager.PlaySound;
            var node6 = game.Playfield.ModeView.FreeReels.Specials;
            if (bind) node6.SoundRequested += audioManager.PlaySound; else node6.SoundRequested -= audioManager.PlaySound;
            var node7 = game.CashFlight;
            if (bind) node7.SoundRequested += audioManager.PlaySound; else node7.SoundRequested -= audioManager.PlaySound;
            var node8 = game.CollectEntry;
            if (bind) node8.SoundRequested += audioManager.PlaySound; else node8.SoundRequested -= audioManager.PlaySound;
            var node9 = game.CoreRound.GetComponentInChildren<RecoveredFreeSlotGame>(true).Window;
            if (bind) node9.SoundRequested += audioManager.PlaySound; else node9.SoundRequested -= audioManager.PlaySound;
            var node10 = game.CoreRound.GetComponentInChildren<RecoveredFreeWheelGame>(true).Window;
            if (bind) node10.SoundRequested += audioManager.PlaySound; else node10.SoundRequested -= audioManager.PlaySound;
            if (bind) node10.Sound1Requested += audioManager.PlaySound1; else node10.Sound1Requested -= audioManager.PlaySound1;
            if (bind) node10.PauseMusicRequested += audioManager.PauseMusic; else node10.PauseMusicRequested -= audioManager.PauseMusic;
            if (bind) node10.ResumeMusicRequested += audioManager.CtnMusic; else node10.ResumeMusicRequested -= audioManager.CtnMusic;
            if (bind) node10.StopSound1Requested += audioManager.StopSound1; else node10.StopSound1Requested -= audioManager.StopSound1;
            var node11 = game.CoreRound.GetComponentInChildren<RecoveredFreeTreasureGame>(true).Window;
            if (bind) node11.SoundRequested += audioManager.PlaySound; else node11.SoundRequested -= audioManager.PlaySound;
            var node12 = game.CoreRound.GetComponentInChildren<RecoveredFreeLuckyGame>(true);
            if (bind) node12.SoundRequested += audioManager.PlaySound; else node12.SoundRequested -= audioManager.PlaySound;
            var node13 = game.CoreRound.MoreSpins;
            if (bind) node13.SoundRequested += audioManager.PlaySound; else node13.SoundRequested -= audioManager.PlaySound;
            var node14 = game.CoreRound.MoreWild;
            if (bind) node14.SoundRequested += audioManager.PlaySound; else node14.SoundRequested -= audioManager.PlaySound;
            var node15 = game.CoreRound.Bank;
            if (bind) node15.SoundRequested += audioManager.PlaySound; else node15.SoundRequested -= audioManager.PlaySound;
            var node16 = game.Playfield.MoreWildEntry;
            if (bind) node16.SoundRequested += audioManager.PlaySound; else node16.SoundRequested -= audioManager.PlaySound;
            var node17 = game.Playfield.BankProgress;
            if (bind) node17.SoundRequested += audioManager.PlaySound; else node17.SoundRequested -= audioManager.PlaySound;
            var node18 = game.CoreRound;
            if (bind) node18.SoundRequested += audioManager.PlaySound; else node18.SoundRequested -= audioManager.PlaySound;
        }
        private void OnDestroy() => Unbind();
        private void ReelStop() => audioManager.PlaySound(reelStopSound);
        private void SpinClick() => audioManager.PlaySound(clickSound);
        private void SpinStart() => audioManager.PlaySound(spinSound);
        private void Speedup() => audioManager.PlaySound1(speedupSound);
        private void CoinShow() => audioManager.PlaySound(coinShowSound);
        private void CoinReveal() => audioManager.PlaySound(coinRevealSound);
        private void LampArrival() => audioManager.PlaySound(lampArrivalSound);
        private void CoinBurst() => audioManager.PlaySound(coinBurstSound);
    }
}
