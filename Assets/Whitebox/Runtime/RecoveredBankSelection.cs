using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    public interface IRecoveredBankSelectionView
    {
        void HideFinger();
        void ShowAdIndicators(IReadOnlyList<int> selected);
        // The real item animation owns its completion and subsequent reward presentation.
        void PlayItem(int itemIndex, float reward, bool firstSelection);
    }

    // UIBankView.OnClickLongZhu MoveNext 2393dbc; ad callbacks 2393384 / 23933f8.
    public sealed class RecoveredBankSelection
    {
        private readonly RecoveredGameplayRules rules;
        private readonly IAdFacade ads;
        private readonly IRecoveredBankSelectionView view;
        private readonly List<int> selected = new List<int>();
        private bool cancelled;
        public IReadOnlyList<int> Selected => selected;
        // Native tempIndex belongs to the window, not to the per-click closure.
        public int TempIndex { get; private set; }

        public RecoveredBankSelection(RecoveredGameplayRules config, IAdFacade facade, IRecoveredBankSelectionView presenter)
        {
            rules = config ?? throw new ArgumentNullException(nameof(config));
            ads = facade ?? throw new ArgumentNullException(nameof(facade));
            view = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }
        public void Reset() { selected.Clear(); cancelled = false; }
        public void Cancel() => cancelled = true;
        public void Click(int index)
        {
            if (cancelled || selected.Contains(index)) return;
            view.HideFinger();
            selected.Add(index);
            TempIndex = rules.RandBankIndex();
            float reward = rules.GetBankReward(TempIndex);
            bool first = selected.Count == 1;
            if (first) view.PlayItem(index, reward, true);
            else
            {
                view.ShowAdIndicators(selected);
                ads.PlayRewardAd(
                    () => { if (!cancelled) view.PlayItem(index, reward, false); },
                    () => { if (!cancelled) selected.Remove(index); },
                    "bank", "itembank");
            }
        }
    }
}
