using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class StartupLoadingView : MonoBehaviour
    {
        [SerializeField] private RawImage artwork;
        [SerializeField] private Text stageLabel;
        [SerializeField] private Image stageFill;
        [SerializeField] private string artworkPath = "Loading/GildedDragonLoading";
        private Texture2D loadedArtwork;
        private string currentMessage;

        private void OnEnable()
        {
            GameLocalization.LanguageChanged += OnLanguageChanged;
            RefreshMessage();
        }

        private void OnDisable() => GameLocalization.LanguageChanged -= OnLanguageChanged;
        private void OnLanguageChanged(int language) => RefreshMessage();
        private void RefreshMessage()
        {
            if (currentMessage != null) stageLabel.text = GameLocalization.Text(currentMessage);
        }

        public IEnumerator Prepare()
        {
            SetStage("Loading...", 0f);
            var request = Resources.LoadAsync<Texture2D>(artworkPath);
            yield return request;
            loadedArtwork = request.asset as Texture2D;
            artwork.texture = loadedArtwork;
            artwork.enabled = loadedArtwork != null;
            // Give the loading canvas a rendered frame before SDK work begins.
            yield return null;
        }

        public void SetStage(string label, float progress)
        {
            currentMessage = label;
            RefreshMessage();
            stageFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        public void ShowFailure(string message)
        {
            currentMessage = message;
            RefreshMessage();
        }

        private void OnDestroy()
        {
            artwork.texture = null;
            if (loadedArtwork != null) Resources.UnloadAsset(loadedArtwork);
        }
    }
}
