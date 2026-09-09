using System;
using System.Collections;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // UIMainView.CheckRewardCollect 23ceeb4; the ledger is read AFTER each flight delay.
    public sealed class RecoveredFreeRewardCollect : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeReels reels;
        [SerializeField] private RecoveredDownWinFlight flights;
        [SerializeField] private float pulseDuration, coinScale, ballScale;
        [SerializeField] private float flightWait, rewardWait, finishWait, countDuration;
        [SerializeField] private AnimationCurve pulseCurve, countCurve;
        private RecoveredFreeSpinResult result;
        private RecoveredPlayerProgress player;
        private RecoveredDownWinText display;
        private RecoveredFreeEntryFlow entry;
        private RecoveredReelWait wait;
        private int position, countVersion;
        public float FreeReward { get; private set; }
        public float CoinReward { get; private set; }
        public bool IsRunning { get; private set; }
        public Exception Error { get; private set; }
        public RecoveredDownWinFlight Flights => flights;
        public event Action Completed;

        public void Bind(RecoveredFreeSpinResult source, RecoveredPlayerProgress progress,
            RecoveredDownWinText text, RectTransform bottom, int order, RecoveredFreeEntryFlow entryFlow = null)
        {
            if (result != null) throw new InvalidOperationException("Free reward collection is already bound.");
            result = source ?? throw new ArgumentNullException(nameof(source));
            player = progress ?? throw new ArgumentNullException(nameof(progress));
            display = text ?? throw new ArgumentNullException(nameof(text));
            flights.Bind(order, text.transform, bottom);
            entry = entryFlow;
            if (entry != null) entry.RewardCountersResetRequested += ResetSession;
            reels.BallScan.Completed += Begin;
        }
        // CheckFreeGame 23c95bc: both fields are reset on Free entry, not per collection.
        public void ResetSession() { FreeReward = 0; CoinReward = 0; }
        public void Begin()
        {
            if (result == null) throw new InvalidOperationException("Free reward collection is not bound.");
            if (IsRunning) throw new InvalidOperationException("Free reward collection is already running.");
            Error = null; position = 0; IsRunning = true; Advance();
        }
        private Transform Bonus(RecoveredReelView reel, int id)
        {
            Component value = id == 9 ? (Component)reels.Specials.CurrentStoppedCoin(reel) : reels.Specials.CurrentStoppedBall(reel);
            return value == null ? null : value.transform;
        }
        private void Advance()
        {
            wait = null;
            try
            {
                while (position < 15)
                {
                    int column = position / 3, row = position % 3; position++;
                    int id = result.GetSymbol(column, row);
                    if (id != 9 && id != 11) continue;
                    var reel = reels.At(column, row);
                    RecoveredTreasureCardRunner.Run(Pulse(Bonus(reel, id), Vector3.one, () =>
                        RecoveredTreasureCardRunner.Run(Pulse(Bonus(reel, id), Vector3.one * (id == 9 ? coinScale : ballScale), null))));
                    flights.Play(reel.transform);
                    wait = RecoveredReelWait.Delay(flightWait, () => ReadReward(reel, id), Fail);
                    return;
                }
                player.SetGreenCount(player.GreenCount + CoinReward);
                wait = RecoveredReelWait.Delay(finishWait, Finish, Fail);
            }
            catch (Exception error) { Fail(error); }
        }
        private void ReadReward(RecoveredReelView reel, int id)
        {
            wait = null;
            if (reels.CoinScan.Rewards.TryGetValue(reel.gameObject, out float reward) && reward > 0)
            {
                int version = ++countVersion; // Kill the preceding numeric tween without completing it.
                float from = FreeReward;
                FreeReward += reward;
                RecoveredTreasureCardRunner.Run(Count(from, FreeReward, version));
                if (id == 9) CoinReward += reward;
            }
            wait = RecoveredReelWait.Delay(rewardWait, Advance, Fail);
        }
        private IEnumerator Count(float from, float to, int version)
        {
            float elapsed = 0;
            while (this != null && version == countVersion)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / countDuration);
                display.ShowAmountOnly(Mathf.LerpUnclamped(from, to, countCurve.Evaluate(t)));
                if (t >= 1) yield break;
                yield return null;
            }
        }
        private IEnumerator Pulse(Transform target, Vector3 to, Action completed)
        {
            if (this == null || target == null) yield break;
            Vector3 from = target.localScale;
            float elapsed = 0;
            while (this != null && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / pulseDuration);
                target.localScale = Vector3.LerpUnclamped(from, to, pulseCurve.Evaluate(t));
                // Requery at completion; the new tween starts on the following runner update.
                if (t >= 1) { completed?.Invoke(); yield break; }
                yield return null;
            }
        }
        private void Finish() { wait = null; IsRunning = false; Completed?.Invoke(); }
        private void Fail(Exception error) { Error = error; IsRunning = false; wait?.Cancel(); wait = null; }
        private void OnDestroy()
        {
            wait?.Cancel(); countVersion++;
            if (reels != null && reels.BallScan != null) reels.BallScan.Completed -= Begin;
            if (entry != null) entry.RewardCountersResetRequested -= ResetSession;
        }
    }
}
