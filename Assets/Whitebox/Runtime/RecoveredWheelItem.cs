using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredWheelItem : MonoBehaviour
    {
        [SerializeField] private Image jackpot;
        [SerializeField] private RectTransform cash, coin;
        [SerializeField] private TMP_Text cashText, coinText;
        [SerializeField] private Sprite[] jackpotSprites;
        [SerializeField] private int sidewaysIndex;
        [SerializeField] private Vector3 sidewaysRotation;
        public Image Jackpot => jackpot;
        public RectTransform Cash => cash;
        public RectTransform Coin => coin;
        public TMP_Text CashText => cashText;
        public TMP_Text CoinText => coinText;

        // WheelItem.Init, RVA 0x23dda04. The source only changes the special
        // orientation at index 6; initialization does not reset other rotations.
        public void Initialize(RecoveredWheelType type, int index, RecoveredGameplayRules rules, bool isA, int language)
        {
            int sprite;
            switch (type)
            {
                case RecoveredWheelType.Major: sprite = 1; break;
                case RecoveredWheelType.Mini: sprite = 2; break;
                case RecoveredWheelType.Grand: sprite = 0; break;
                case RecoveredWheelType.Cash:
                    coin.gameObject.SetActive(isA);
                    cash.gameObject.SetActive(!isA);
                    (isA ? coinText : cashText).text = RecoveredCurrency.Format(rules.GetWheelReward(index), language, 2);
                    jackpot.gameObject.SetActive(false);
                    return;
                default:
                    jackpot.gameObject.SetActive(false);
                    return;
            }
            jackpot.sprite = jackpotSprites[sprite];
            if (index == sidewaysIndex) jackpot.transform.localRotation = Quaternion.Euler(sidewaysRotation);
            jackpot.gameObject.SetActive(true);
            coin.gameObject.SetActive(false);
            cash.gameObject.SetActive(false);
            jackpot.SetNativeSize();
        }
    }
}
