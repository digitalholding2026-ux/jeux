using System;
using UnityEngine;
using UnityEngine.UI;
using ColorFlow.Core;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// HUD « bubbles » : nom du niveau, compteur de pixels et barre de progression,
    /// puis écran de fin animé (confettis en victoire, chute du texte en défaite).
    /// </summary>
    public sealed class HudView
    {
        private readonly RectTransform _root;
        private readonly Text _levelName;
        private readonly Text _progress;
        private readonly RectTransform _progressFill;
        private readonly RectTransform _progressBubble;
        private readonly RectTransform _hintCard;
        private readonly CanvasGroup _hintGroup;
        private readonly Text _hint;
        private readonly GameObject _overlay;
        private readonly CanvasGroup _overlayGroup;
        private readonly RectTransform _card;
        private readonly Text _result;
        private readonly Text _reason;

        private float _hintTimer;
        private float _ratio;
        private float _targetRatio;
        private float _fillWidth;
        private float _trackWidth;

        public HudView(RectTransform root)
        {
            _root = root;

            var hud = UiFactory.CreateRect("HUD", root);
            UiFactory.SetRect(
                hud,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(0f, 340f));

            // --- Nom du niveau dans une bulle ---
            var levelBubble = UiFactory.CreateCard("LevelBubble", hud, JuicePalette.Paper, 40f);
            UiFactory.SetRect(
                levelBubble,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(40f, -26f),
                new Vector2(560f, 112f));

            _levelName = UiFactory.CreateText(
                "LevelName",
                levelBubble,
                string.Empty,
                46,
                JuicePalette.Ink,
                TextAnchor.MiddleLeft);
            UiFactory.Stretch(_levelName.rectTransform, 34f, 8f, 24f, 8f);

            // --- Compteur de pixels ---
            var progressBubble = UiFactory.CreateCard("ProgressBubble", hud, JuicePalette.Paper, 40f);
            UiFactory.SetRect(
                progressBubble,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-40f, -26f),
                new Vector2(300f, 112f));
            _progressBubble = progressBubble;

            var star = UiFactory.CreateImage("Star", progressBubble, JuicePalette.Sun);
            star.sprite = ArtFactory.Star(64, 5, 0.46f);
            star.preserveAspect = true;
            UiFactory.SetRect(
                star.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(24f, 0f),
                new Vector2(56f, 56f));

            _progress = UiFactory.CreateText(
                "Progress",
                progressBubble,
                string.Empty,
                44,
                JuicePalette.Ink,
                TextAnchor.MiddleLeft);
            UiFactory.Stretch(_progress.rectTransform, 96f, 8f, 20f, 8f);

            // --- Barre de progression ---
            var track = UiFactory.CreateRounded("ProgressTrack", hud, new Color(1f, 1f, 1f, 0.55f), 18f);
            UiFactory.SetRect(
                track.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -158f),
                new Vector2(780f, 28f));
            _trackWidth = 762f;

            _progressFill = UiFactory.CreateRect("Fill", track.transform);
            var fillImage = _progressFill.gameObject.AddComponent<Image>();
            fillImage.sprite = ArtFactory.RoundedRect(18f);
            fillImage.type = Image.Type.Sliced;
            fillImage.color = JuicePalette.Accent;
            _progressFill.anchorMin = new Vector2(0f, 0.5f);
            _progressFill.anchorMax = new Vector2(0f, 0.5f);
            _progressFill.pivot = new Vector2(0f, 0.5f);
            _progressFill.anchoredPosition = new Vector2(9f, 0f);
            _progressFill.sizeDelta = new Vector2(0f, 26f);
            _fillWidth = _trackWidth;

            // --- Message d'aide ---
            _hintCard = UiFactory.CreateCard("HintBubble", root, JuicePalette.Paper, 34f);
            UiFactory.SetRect(
                _hintCard,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -520f),
                new Vector2(760f, 88f));
            _hintGroup = _hintCard.gameObject.AddComponent<CanvasGroup>();
            _hintGroup.alpha = 0f;
            _hintGroup.blocksRaycasts = false;

            _hint = UiFactory.CreateText("HintText", _hintCard, string.Empty, 38, JuicePalette.Ink);
            UiFactory.Stretch(_hint.rectTransform, 24f, 6f, 24f, 6f);

            // --- Écran de fin ---
            var overlayRoot = UiFactory.CreateRect("Overlay", root);
            _overlay = overlayRoot.gameObject;

            // Le voile a son propre CanvasGroup : la carte reste toujours opaque.
            var dim = UiFactory.CreateImage(
                "Dim",
                _overlay.transform,
                JuicePalette.WithAlpha(JuicePalette.Ink, 0.34f),
                true);
            UiFactory.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            _overlayGroup = dim.gameObject.AddComponent<CanvasGroup>();
            _overlayGroup.alpha = 0f;

            _card = UiFactory.CreateCard("Card", _overlay.transform, JuicePalette.Paper, 56f);
            UiFactory.SetRect(
                _card,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 720f));

            _result = UiFactory.CreateText("Result", _card, string.Empty, 112, JuicePalette.Mint, TextAnchor.MiddleCenter, true, true);
            UiFactory.SetRect(
                _result.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 170f),
                new Vector2(820f, 190f));

            _reason = UiFactory.CreateText("Reason", _card, string.Empty, 44, JuicePalette.InkSoft);
            UiFactory.SetRect(
                _reason.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f),
                new Vector2(780f, 90f));

            var replay = UiFactory.CreateButton3D("Replay", _card, "REJOUER", JuicePalette.Mint, Color.white, 52, 14f);
            UiFactory.SetRect(
                replay.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-200f, -170f),
                new Vector2(380f, 140f));
            replay.onClick.AddListener(() => ReplayRequested?.Invoke());

            var menu = UiFactory.CreateButton3D("Menu", _card, "MENU", JuicePalette.Hex("E8F4FC"), JuicePalette.Ink, 52, 14f);
            UiFactory.SetRect(
                menu.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(200f, -170f),
                new Vector2(380f, 140f));
            menu.onClick.AddListener(() => MenuRequested?.Invoke());

            // La face est très claire : on force un socle bleu pour garder le relief.
            var menuBase = menu.transform.Find("Base")?.GetComponent<Image>();
            if (menuBase != null)
            {
                menuBase.color = JuicePalette.Hex("B9D7E8");
            }

            _overlay.SetActive(false);
        }

        public event Action ReplayRequested;

        public event Action MenuRequested;

        public void SetLevelName(string value)
        {
            _levelName.text = value;
        }

        public void SetProgress(int painted, int total)
        {
            _progress.text = painted + " / " + total;
            _targetRatio = total > 0 ? Mathf.Clamp01((float)painted / total) : 0f;
            TweenRunner.Run(Tween.Pop(_progressBubble, 0.10f, 0.18f));
        }

        public void ShowHint(string message, float seconds = 1.6f)
        {
            _hint.text = message;
            _hintTimer = seconds;
            _hintGroup.alpha = 1f;
            _hintCard.localScale = Vector3.one;
            TweenRunner.Run(Tween.PopIn(_hintCard, 0.22f));
        }

        public void ShowResult(GameState state)
        {
            var won = state == GameState.Won;
            _result.text = won ? "GAGNÉ !" : "PERDU";
            _result.color = won ? JuicePalette.Mint : JuicePalette.Pink;
            _reason.text = won ? "Motif terminé, bravo !" : "Le tapis est plein";

            _overlay.SetActive(true);
            _overlayGroup.alpha = 0f;
            TweenRunner.Run(Tween.Fade(_overlayGroup, 0f, 1f, 0.22f));
            TweenRunner.Run(Tween.PopIn(_card, 0.34f));

            var resultRect = _result.rectTransform;
            if (won)
            {
                resultRect.anchoredPosition = new Vector2(0f, 170f);
                TweenRunner.Run(Tween.PopIn(resultRect, 0.5f));
                UiParticles.Confetti(_root, 110);
            }
            else
            {
                resultRect.anchoredPosition = new Vector2(0f, 170f);
                TweenRunner.Run(Tween.Move(resultRect, new Vector2(0f, 470f), new Vector2(0f, 170f), 0.55f, Ease.OutBounce));
                AudioManager.PlayLose();
            }
        }

        public void HideResult()
        {
            _overlay.SetActive(false);
            _overlayGroup.alpha = 0f;
        }

        /// <summary>Fait expirer le message d'aide et lisse la barre de progression.</summary>
        public void Tick(float deltaTime)
        {
            if (_hintTimer > 0f)
            {
                _hintTimer -= deltaTime;
                if (_hintTimer <= 0f)
                {
                    TweenRunner.Run(FadeHint());
                }
            }

            _ratio = Mathf.Lerp(_ratio, _targetRatio, Mathf.Clamp01(deltaTime * 10f));
            var width = Mathf.Max(0f, _trackWidth * _ratio);
            if (Mathf.Abs(width - _fillWidth) > 0.5f)
            {
                _fillWidth = width;
                _progressFill.sizeDelta = new Vector2(width, 26f);
            }
        }

        private System.Collections.IEnumerator FadeHint()
        {
            yield return Tween.Fade(_hintGroup, 1f, 0f, 0.35f);
            _hint.text = string.Empty;
        }
    }
}
