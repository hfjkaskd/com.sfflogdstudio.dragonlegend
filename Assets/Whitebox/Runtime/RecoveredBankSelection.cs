using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    public interface IRecoveredBankSelectionView
    {
        int WinCount { get; }
        void HideFinger();
        void ShowAdIndicators(IReadOnlyList<int> selected);
        // Pass completed to RecoveredBankItem.Play: only flight arrival runs it.
        void PlayItem(int itemIndex, float reward, bool firstSelection, Action<float> completed);
        void RevealButtons(float duration);
        void ShowContinueFinger();
        void PlaySound(string name);
        void Hide();
    }

    // UIBankView.OnClickLongZhu MoveNext 2393dbc; ad callbacks 2393384 / 23933f8.
    public sealed class RecoveredBankSelection
    {
        private readonly RecoveredGameplayRules rules;
        private readonly IAdFacade ads;
        private readonly IRecoveredBankSelectionView view;
        private readonly List<int> selected = new List<int>();
        private bool cancelled;
        private int lifetime;
        public IReadOnlyList<int> Selected => selected;
        // Native tempIndex belongs to the window, not to the per-click closure.
        public int TempIndex { get; private set; }
        public bool IsContinue { get; private set; }

        public RecoveredBankSelection(RecoveredGameplayRules config, IAdFacade facade, IRecoveredBankSelectionView presenter)
        {
            rules = config ?? throw new ArgumentNullException(nameof(config));
            ads = facade ?? throw new ArgumentNullException(nameof(facade));
            view = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }
        public void Reset() { lifetime++; selected.Clear(); cancelled = false; IsContinue = false; }
        public void Cancel() { lifetime++; cancelled = true; }
        public void Click(int index)
        {
            if (cancelled || selected.Contains(index)) return;
            view.HideFinger();
            selected.Add(index);
            TempIndex = rules.RandBankIndex();
            float reward = rules.GetBankReward(TempIndex);
            bool first = selected.Count == 1;
            int generation = lifetime;
            Action<float> completed = value => { if (IsCurrent(generation)) ItemCompleted(first, generation); };
            if (first) view.PlayItem(index, reward, true, completed);
            else
            {
                view.ShowAdIndicators(selected);
                ads.PlayRewardAd(
                    () => { if (IsCurrent(generation)) view.PlayItem(index, reward, false, completed); },
                    () => { if (IsCurrent(generation)) selected.Remove(index); },
                    "bank", "itembank");
            }
        }
        private bool IsCurrent(int generation) => !cancelled && generation == lifetime;
        // OnClickButton 2391e78: the continue latch gates these buttons, not individual balls.
        public void ClickButton(string name)
        {
            if (cancelled || IsContinue) return;
            if (name == "OpenBtn")
            {
                view.PlaySound("click"); IsContinue = true;
                int generation = lifetime;
                ads.PlayRewardAd(() => { if (IsCurrent(generation)) OpenRemaining(generation); },
                    () => { if (IsCurrent(generation)) IsContinue = false; }, "bank", "Openbank");
            }
            else if (name == "UnPlayBtn")
            {
                view.PlaySound("click"); ads.PlayInterAd("iv_close", "bank");
                IsContinue = true; view.Hide();
            }
        }
        // PlayAd 2392128 uses the SAME random offset into two differently filtered lists.
        private void OpenRemaining(int generation)
        {
            var remaining = new List<int>(); var categories = new List<int>();
            for (int i = 0; i < view.WinCount; i++)
            {
                if (!selected.Contains(i)) remaining.Add(i);
                if (i != TempIndex) categories.Add(i);
            }
            int offset = UnityEngine.Random.Range(0, remaining.Count);
            int index = remaining[offset];
            float reward = rules.GetBankReward(categories[offset]);
            selected.Add(index); view.ShowAdIndicators(selected);
            view.PlayItem(index, reward, false, value => {
                if (!IsCurrent(generation)) return;
                // 2392ec8: release only after the flight, unless all selections are now filled.
                if (selected.Count != view.WinCount) IsContinue = false;
                else RecoveredReelWait.Delay(.5f, () => { if (IsCurrent(generation)) view.Hide(); }, UnityEngine.Debug.LogException);
            });
        }
        // Native first/later flight continuations: 2393454 and 2393870.
        private void ItemCompleted(bool first, int generation)
        {
            if (first)
            {
                view.ShowAdIndicators(selected);
                view.RevealButtons(.5f);
                RecoveredReelWait.Delay(1, () => { if (IsCurrent(generation)) view.ShowContinueFinger(); }, UnityEngine.Debug.LogException);
            }
            else if (selected.Count == view.WinCount)
                RecoveredReelWait.Delay(.5f, () => { if (IsCurrent(generation)) view.Hide(); }, UnityEngine.Debug.LogException);
        }
    }
}
