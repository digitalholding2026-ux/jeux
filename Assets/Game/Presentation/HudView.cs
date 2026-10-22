using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ColorFlow.Core;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// HUD « bubbles » du Sprint 2 : nom du niveau, score à côté de l'étoile,
    /// objectif, chrono, barre de progression, popup de combo flottant et
    /// écran de fin (objectif atteint ou manqué, score, pièces gagnées).
    /// </summary>
    public sealed class HudView
    {
        private readonly RectTransform _root;
        private readonly Text _levelName;
        private readonly Text _progress;
        private readonly RectTransform _progressFill;
        private readonly RectTransform _progressBubble;
        private readonly RectTransform _scoreBubble;
        private readonly Text _scoreLabel;
        private readonly Text _goalLabel;
        private readonly RectTransform _timerBubble;
        private readonly Image _timerPanel;
        private readonly Text _timerLabel;
        private readonly RectTransform _hintCard;
        private readonly CanvasGroup _hintGroup;
        private readonly Text _hint;
        private readonly GameObject _overlay;
        private readonly CanvasGroup _overlayGroup;
        private readonly RectTransform _card;
        private readonly Text _result;
        private readonly Text _objective;
        private readonly Text _stats;
        private readonly RectTransform _comboRect;
        private readonly Text _comboLabel;
        private readonly CanvasGroup _comboGroup;

        private float _hintTimer;
        private float _ratio;
        private float _targetRatio;
        private float _fillWidth;
        private float _trackWidth;
        private Vector2 _comboHome;
        private Coroutine _comboRoutine;
        private bool _hasTimeLimit;
        private int _lastScore = -1;

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
                new Vector2(0f, 360f));

            // --- Nom du niveau ---
            var levelBubble = UiFactory.CreateCard("LevelBubble", hud, JuicePalette.Paper, 40f);
            UiFactory.SetRect(
                levelBubble,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(36f, -22f),
                new Vector2(380f, 104f));

            _levelName = UiFactory.CreateText(
                "LevelName",
                levelBubble,
                string.Empty,
                34,
                JuicePalette.Ink,
                TextAnchor.MiddleLeft);
            UiFactory.Stretch(_levelName.rectTransform, 28f, 8f, 16f, 8f);

            // --- Compteur de pixels (gauche du score) ---
            var progressBubble = UiFactory.CreateCard("ProgressBubble", hud, JuicePalette.Paper, 40f);
            UiFactory.SetRect(
                progressBubble,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-356f, -22f),
                new Vector2(260f, 104f));
            _progressBubble = progressBubble;

            _progress = UiFactory.CreateText(
                "Progress",
                progressBubble,
                string.Empty,
                38,
                JuicePalette.InkSoft);
            UiFactory.Stretch(_progress.rectTransform, 14f, 8f, 14f, 8f);

            // --- Score, à côté de l'étoile ---
            var scoreBubble = UiFactory.CreateCard("ScoreBubble", hud, JuicePalette.Paper, 40f);
            UiFactory.SetRect(
                scoreBubble,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-36f, -22f),
                new Vector2(300f, 104f));
            _scoreBubble = scoreBubble;

            var star = UiFactory.CreateImage("Star", scoreBubble, JuicePalette.Sun);
            star.sprite = ArtFactory.Star(64, 5, 0.46f);
            star.preserveAspect = true;
            UiFactory.SetRect(
                star.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(20f, 4f),
                new Vector2(54f, 54f));

            _scoreLabel = UiFactory.CreateText(
                "Score",
                scoreBubble,
                "0",
                40,
                JuicePalette.Ink,
                TextAnchor.MiddleLeft);
            UiFactory.Stretch(_scoreLabel.rectTransform, 86f, 10f, 12f, 8f);

            // --- Objectif ---
            var goalCard = UiFactory.CreateCard("GoalBubble", hud, JuicePalette.Paper, 34f);
            UiFactory.SetRect(
                goalCard,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(36f, -142f),
                new Vector2(620f, 72f));

            _goalLabel = UiFactory.CreateText(
                "Goal",
                goalCard,
                string.Empty,
                28,
                JuicePalette.Ink,
                TextAnchor.MiddleLeft);
            UiFactory.Stretch(_goalLabel.rectTransform, 22f, 10f, 18f, 10f);

            // --- Chrono ---
            var timerBubble = UiFactory.CreateCard("TimerBubble", hud, JuicePalette.Sun, 34f);
            UiFactory.SetRect(
                timerBubble,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-36f, -142f),
                new Vector2(200f, 72f));
            _timerBubble = timerBubble;
            _timerPanel = timerBubble.Find("Panel").GetComponent<Image>();

            _timerLabel = UiFactory.CreateText(
                "Timer",
                timerBubble,
                "0 s",
                36,
                JuicePalette.Ink);
            UiFactory.Stretch(_timerLabel.rectTransform, 8f, 8f, 8f, 8f);

            // --- Barre de progression ---
            var track = UiFactory.CreateRounded("ProgressTrack", hud, new Color(1f, 1f, 1f, 0.58f), 18f);
            UiFactory.SetRect(
                track.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -232f),
                new Vector2(1000f, 26f));
            _trackWidth = 988f;

            _progressFill = UiFactory.CreateRect("Fill", track.transform);
            var fillImage = _progressFill.gameObject.AddComponent<Image>();
            fillImage.sprite = ArtFactory.RoundedRect(18f);
            fillImage.type = Image.Type.Sliced;
            fillImage.color = JuicePalette.Accent;
            _progressFill.anchorMin = new Vector2(0f, 0.5f);
            _progressFill.anchorMax = new Vector2(0f, 0.5f);
            _progressFill.pivot = new Vector2(0f, 0.5f);
            _progressFill.anchoredPosition = new Vector2(6f, 0f);
            _progressFill.sizeDelta = new Vector2(0f, 22f);
            _fillWidth = 0f;

            // --- Popup de combo (au-dessus du plateau) ---
            _comboRect = UiFactory.CreateRect("ComboPopup", root);
            UiFactory.SetRect(
                _comboRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 300f),
                new Vector2(920f, 170f));
            _comboHome = _comboRect.anchoredPosition;
            _comboRect.localScale = Vector3.one;

            _comboGroup = _comboRect.gameObject.AddComponent<CanvasGroup>();
            _comboGroup.alpha = 0f;
            _comboGroup.blocksRaycasts = false;
            _comboGroup.interactable = false;

            _comboLabel = UiFactory.CreateText(
                "ComboText",
                _comboRect,
                string.Empty,
                96,
                JuicePalette.Sun,
                TextAnchor.MiddleCenter,
                true,
                true);
            UiFactory.Stretch(_comboLabel.rectTransform, 0f, 0f, 0f, 0f);

            // --- Message d'aide ---
            _hintCard = UiFactory.CreateCard("HintBubble", root, JuicePalette.Paper, 34f);
            UiFactory.SetRect(
                _hintCard,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -520f),
                new Vector2(780f, 88f));
            _hintGroup = _hintCard.gameObject.AddComponent<CanvasGroup>();
            _hintGroup.alpha = 0f;
            _hintGroup.blocksRaycasts = false;

            _hint = UiFactory.CreateText("HintText", _hintCard, string.Empty, 38, JuicePalette.Ink);
            UiFactory.Stretch(_hint.rectTransform, 24f, 6f, 24f, 6f);

            // --- Écran de fin ---
            var overlayRoot = UiFactory.CreateRect("Overlay", root);
            _overlay = overlayRoot.gameObject;

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
                new Vector2(920f, 880f));

            _result = UiFactory.CreateText("Result", _card, string.Empty, 108, JuicePalette.Mint, TextAnchor.MiddleCenter, true, true);
            UiFactory.SetRect(
                _result.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 286f),
                new Vector2(840f, 190f));

            _objective = UiFactory.CreateText("Objective", _card, string.Empty, 38, JuicePalette.Mint);
            UiFactory.SetRect(
                _objective.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 116f),
                new Vector2(820f, 120f));

            _stats = UiFactory.CreateText("Stats", _card, string.Empty, 42, JuicePalette.Ink);
            UiFactory.SetRect(
                _stats.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -6f),
                new Vector2(820f, 80f));

            var replay = UiFactory.CreateButton3D("Replay", _card, "REJOUER", JuicePalette.Mint, Color.white, 50, 14f);
            UiFactory.SetRect(
                replay.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-200f, -250f),
                new Vector2(380f, 140f));
            replay.onClick.AddListener(() => ReplayRequested?.Invoke());

            var menu = UiFactory.CreateButton3D("Menu", _card, "MENU", JuicePalette.Hex("E8F4FC"), JuicePalette.Ink, 50, 14f);
            UiFactory.SetRect(
                menu.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(200f, -250f),
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

        /// <summary>Libellé d'objectif + affichage du chrono si le niveau est limité.</summary>
        public void SetGoal(string label, bool hasTimeLimit)
        {
            _goalLabel.text = label;
            _hasTimeLimit = hasTimeLimit;
            _timerBubble.gameObject.SetActive(hasTimeLimit);
        }

        public void SetTimeRemaining(float seconds)
        {
            if (!_hasTimeLimit)
            {
                return;
            }

            var remaining = Mathf.Max(0f, seconds);
            _timerLabel.text = Mathf.CeilToInt(remaining) + " s";

            var urgent = remaining <= 5f;
            _timerLabel.color = urgent ? JuicePalette.Pink : JuicePalette.Ink;
            if (_timerPanel != null)
            {
                _timerPanel.color = urgent ? JuicePalette.Pink : JuicePalette.Sun;
            }
        }

        public void SetScore(int score)
        {
            if (score == _lastScore)
            {
                return;
            }

            _lastScore = score;
            _scoreLabel.text = FormatScore(score);
            TweenRunner.Run(Tween.Pop(_scoreBubble, 0.08f, 0.16f));
        }

        public void SetProgress(int painted, int total)
        {
            _progress.text = painted + " / " + total;
            _targetRatio = total > 0 ? Mathf.Clamp01((float)painted / total) : 0f;
            TweenRunner.Run(Tween.Pop(_progressBubble, 0.08f, 0.16f));
        }

        /// <summary>Affiche « COMBO ×2 ! » avec un rebond, puis le fait disparaître.</summary>
        public void ShowCombo(int multiplier)
        {
            if (multiplier < 2)
            {
                return;
            }

            _comboLabel.text = "COMBO ×" + multiplier + " !";
            _comboLabel.color = multiplier >= 4
                ? JuicePalette.Violet
                : (multiplier == 3 ? JuicePalette.Pink : JuicePalette.Sun);

            if (_comboRoutine != null)
            {
                TweenRunner.Stop(_comboRoutine);
                _comboRoutine = null;
            }

            _comboGroup.alpha = 1f;
            _comboRect.anchoredPosition = _comboHome;
            _comboRoutine = TweenRunner.Run(ComboRoutine());
        }

        public void HideCombo()
        {
            if (_comboRoutine != null)
            {
                TweenRunner.Stop(_comboRoutine);
                _comboRoutine = null;
            }

            _comboGroup.alpha = 0f;
        }

        public void ShowHint(string message, float seconds = 1.6f)
        {
            _hint.text = message;
            _hintTimer = seconds;
            _hintGroup.alpha = 1f;
            _hintCard.localScale = Vector3.one;
            TweenRunner.Run(Tween.PopIn(_hintCard, 0.22f));
        }

        /// <summary>Écran de fin : objectif atteint/manqué, score et pièces gagnées.</summary>
        public void ShowResult(GameState state, string objective, string stats)
        {
            var won = state == GameState.Won;
            _result.text = won ? "GAGNÉ !" : "PERDU";
            _result.color = won ? JuicePalette.Mint : JuicePalette.Pink;
            _objective.text = objective;
            _objective.color = won ? JuicePalette.Mint : JuicePalette.PinkDeep;
            _stats.text = stats;

            _overlay.SetActive(true);
            _overlayGroup.alpha = 0f;
            TweenRunner.Run(Tween.Fade(_overlayGroup, 0f, 1f, 0.22f));
            TweenRunner.Run(Tween.PopIn(_card, 0.34f));

            var resultRect = _result.rectTransform;
            resultRect.anchoredPosition = new Vector2(0f, 286f);
            if (won)
            {
                TweenRunner.Run(Tween.PopIn(resultRect, 0.5f));
                UiParticles.Confetti(_root, 110);
            }
            else
            {
                TweenRunner.Run(Tween.Move(resultRect, new Vector2(0f, 540f), new Vector2(0f, 286f), 0.55f, Ease.OutBounce));
                AudioManager.PlayLose();
            }
        }

        public void HideResult()
        {
            _overlay.SetActive(false);
            _overlayGroup.alpha = 0f;
        }

        /// <summary>Fait expirer l'aide, lisse la barre et fait pulser le chrono.</summary>
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
                _progressFill.sizeDelta = new Vector2(width, 22f);
            }

            if (_hasTimeLimit && _timerLabel.color == JuicePalette.Pink)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.05f;
                _timerBubble.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                _timerBubble.localScale = Vector3.one;
            }
        }

        private IEnumerator ComboRoutine()
        {
            _comboRect.localScale = Vector3.one * 0.35f;
            yield return Tween.Scale(_comboRect, Vector3.one * 0.35f, Vector3.one, 0.24f, Ease.OutBack);
            yield return Tween.Delay(0.42f);
            yield return Tween.Move(_comboRect, _comboHome, _comboHome + new Vector2(0f, 54f), 0.5f, Ease.OutCubic);
            yield return Tween.Fade(_comboGroup, 1f, 0f, 0.25f);

            _comboGroup.alpha = 0f;
            _comboRect.anchoredPosition = _comboHome;
            _comboRoutine = null;
        }

        private IEnumerator FadeHint()
        {
            yield return Tween.Fade(_hintGroup, 1f, 0f, 0.35f);
            _hint.text = string.Empty;
        }

        private static string FormatScore(int score)
        {
            return score.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
        }
    }
}
