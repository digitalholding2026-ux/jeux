using System;
using UnityEngine;
using UnityEngine.UI;
using ColorFlow.Core;

namespace ColorFlow.Game.Presentation
{
    /// <summary>Bandeau haut + écran de fin (victoire / défaite).</summary>
    public sealed class HudView
    {
        private readonly Text _levelName;
        private readonly Text _progress;
        private readonly Text _hint;
        private readonly GameObject _overlay;
        private readonly Text _result;
        private readonly Text _reason;

        private float _hintTimer;

        public HudView(RectTransform root)
        {
            var hud = UiFactory.CreateRect("HUD", root);
            UiFactory.SetRect(
                hud,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(0f, 300f));

            _levelName = UiFactory.CreateText("LevelName", hud, string.Empty, 56, UiFactory.TextPrimary, TextAnchor.UpperLeft);
            UiFactory.SetRect(
                _levelName.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(48f, -36f),
                new Vector2(620f, 80f));

            _progress = UiFactory.CreateText("Progress", hud, string.Empty, 56, UiFactory.TextMuted, TextAnchor.UpperRight);
            UiFactory.SetRect(
                _progress.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-48f, -36f),
                new Vector2(420f, 80f));

            _hint = UiFactory.CreateText("Hint", hud, string.Empty, 42, UiFactory.TextMuted);
            UiFactory.SetRect(
                _hint.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 16f),
                new Vector2(-96f, 70f));

            var overlayImage = UiFactory.CreateImage("Overlay", root, new Color(0.02f, 0.025f, 0.04f, 0.88f), true);
            _overlay = overlayImage.gameObject;

            var card = UiFactory.CreateImage("Card", _overlay.transform, UiFactory.Panel, true);
            var cardRect = card.rectTransform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(880f, 640f);
            cardRect.anchoredPosition = Vector2.zero;

            _result = UiFactory.CreateText("Result", card.transform, string.Empty, 96, UiFactory.TextPrimary);
            UiFactory.SetRect(
                _result.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f),
                new Vector2(780f, 130f));

            _reason = UiFactory.CreateText("Reason", card.transform, string.Empty, 46, UiFactory.TextMuted);
            UiFactory.SetRect(
                _reason.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 30f),
                new Vector2(760f, 70f));

            var replay = UiFactory.CreateButton("Replay", card.transform, "Rejouer", UiFactory.Accent, Color.white, 54);
            UiFactory.SetRect(
                replay.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -160f),
                new Vector2(520f, 140f));
            replay.onClick.AddListener(() => ReplayRequested?.Invoke());

            _overlay.SetActive(false);
        }

        public event Action ReplayRequested;

        public void SetLevelName(string value)
        {
            _levelName.text = value;
        }

        public void SetProgress(int painted, int total)
        {
            _progress.text = painted + " / " + total;
        }

        public void ShowHint(string message, float seconds = 1.4f)
        {
            _hint.text = message;
            _hintTimer = seconds;
        }

        public void ShowResult(GameState state)
        {
            var won = state == GameState.Won;
            _result.text = won ? "GAGNÉ !" : "PERDU";
            _result.color = won ? UiFactory.Success : UiFactory.Accent;
            _reason.text = won ? "Motif terminé" : "Le tapis est plein";
            _overlay.SetActive(true);
        }

        public void HideResult()
        {
            _overlay.SetActive(false);
        }

        /// <summary>Fait expirer le message d'aide.</summary>
        public void Tick(float deltaTime)
        {
            if (_hintTimer <= 0f)
            {
                return;
            }

            _hintTimer -= deltaTime;
            if (_hintTimer <= 0f)
            {
                _hint.text = string.Empty;
            }
        }
    }
}
