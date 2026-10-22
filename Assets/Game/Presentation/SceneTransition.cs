using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Fondu entre les scènes : un voile persistant est créé une seule fois,
    /// puis animé à l'entrée et à la sortie. Aucun asset : une Image unie.
    /// </summary>
    public static class SceneTransition
    {
        private static Image _veil;
        private static CanvasGroup _group;

        public static void FadeIn(float duration = 0.30f)
        {
            Ensure();
            _group.blocksRaycasts = true;
            TweenRunner.Run(FadeRoutine(1f, 0f, duration, false));
        }

        public static void Load(string sceneName, float duration = 0.26f)
        {
            Ensure();
            TweenRunner.Run(LoadRoutine(sceneName, duration));
        }

        private static void Ensure()
        {
            if (_veil != null)
            {
                return;
            }

            var host = new GameObject("~SceneTransition", typeof(Canvas), typeof(CanvasGroup));
            Object.DontDestroyOnLoad(host);

            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            _group = host.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _veil = UiFactory.CreateImage("Veil", host.transform, JuicePalette.Cream);
            _veil.raycastTarget = true;
        }

        private static System.Collections.IEnumerator FadeRoutine(float from, float to, float duration, bool block)
        {
            _group.blocksRaycasts = block;
            yield return Tween.Fade(_group, from, to, duration, Ease.OutQuad);
            _group.blocksRaycasts = false;
        }

        private static System.Collections.IEnumerator LoadRoutine(string sceneName, float duration)
        {
            _group.blocksRaycasts = true;
            yield return Tween.Fade(_group, 0f, 1f, duration, Ease.InQuad);
            SceneManager.LoadScene(sceneName);
        }
    }
}
