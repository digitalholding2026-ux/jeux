using UnityEngine;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Applique <see cref="Screen.safeArea"/> au RectTransform : encoche,
    /// barre de navigation et coins arrondis ne peuvent plus rogner l'UI.
    /// À placer sur un RectTransform enfant plein écran du canvas.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _applied = new Rect(-1f, -1f, -1f, -1f);
        private int _screenWidth;
        private int _screenHeight;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply(true);
        }

        private void OnEnable()
        {
            Apply(true);
        }

        private void Update()
        {
            Apply(false);
        }

        /// <summary>Recalcule les ancres depuis l'aire sûre de l'écran.</summary>
        public void Apply(bool force)
        {
            if (_rect == null)
            {
                _rect = (RectTransform)transform;
            }

            var safe = Screen.safeArea;
            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);

            if (!force && safe == _applied && width == _screenWidth && height == _screenHeight)
            {
                return;
            }

            _applied = safe;
            _screenWidth = width;
            _screenHeight = height;

            // Cas dégénéré (éditeur sans device simulé) : plein écran.
            if (safe.width <= 1f || safe.height <= 1f)
            {
                _rect.anchorMin = Vector2.zero;
                _rect.anchorMax = Vector2.one;
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
                return;
            }

            _rect.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
            _rect.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
