using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredAccountWindow : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nameInput, emailInput;
        [SerializeField] private TMP_Text detail,accountPlaceholder;
        [SerializeField] private Button submit, close;
        [SerializeField] private string[] providers;
        [SerializeField] private string promptFormat, requiredMessage,accountPlaceholderFormat;
        private Func<string,string,bool> accepted;
        private string currentPrompt;
        public TMP_InputField NameInput => nameInput;
        public TMP_InputField EmailInput => emailInput;
        public Button SubmitButton => submit;
        public Button CloseButton => close;
        public event Action<string> SoundRequested;
        private void Awake()
        {
            submit.onClick.AddListener(Submit);
            close.onClick.AddListener(()=>{SoundRequested?.Invoke("click");Cancel();});
            nameInput.onValueChanged.AddListener(ResetPrompt);emailInput.onValueChanged.AddListener(ResetPrompt);
        }
        public void Show(int type, RecoveredPlayerProgress player, Camera camera, Func<string,string,bool> callback)
        {
            accepted=callback;
            GetComponent<Canvas>().worldCamera=camera;
            nameInput.text=emailInput.text=string.Empty;
            foreach(var account in player.CashOutAccounts)
                if(account.type==type){nameInput.text=account.accountName;emailInput.text=account.emailName;break;}
            currentPrompt=GameLocalization.Format(promptFormat,GameLocalization.CurrentLanguage,providers[type-1]);detail.text=currentPrompt;
            accountPlaceholder.text=GameLocalization.Format(accountPlaceholderFormat,GameLocalization.CurrentLanguage,providers[type-1]);
            gameObject.SetActive(true);
        }
        private void ResetPrompt(string value){if(currentPrompt!=null)detail.text=currentPrompt;}
        private void Submit()
        {
            SoundRequested?.Invoke("click");
            string name=nameInput.text.Trim(), email=emailInput.text.Trim();
            if(string.IsNullOrEmpty(name)||string.IsNullOrEmpty(email)){detail.text=GameLocalization.Text(requiredMessage);return;}
            var callback=accepted;
            // Keep the form open if persistence/submission throws, so the user can retry.
            if(callback!=null&&callback(name,email))Cancel();
        }
        public void Cancel(){accepted=null;gameObject.SetActive(false);}
    }
}
