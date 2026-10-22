using System.Collections.Generic;
using ColorFlow.Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Menu principal : fond dégradé, blobs flottants, gros bouton « JOUER » 3D
    /// et sélection de niveau en cartes arrondies. Tout est construit par le code.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private readonly List<RectTransform> _floaters = new List<RectTransform>();
        private readonly List<Vector2> _floatHomes = new List<Vector2>();
        private readonly List<float> _floatPhases = new List<float>();
        private readonly List<Image> _chipBorders = new List<Image>();
        private readonly List<Image> _chipStars = new List<Image>();

        private LevelData[] _levels;
        private int _selectedIndex;
        private float _time;

        private void Awake()
        {
            AudioManager.Init();
            UiFactory.EnsureEventSystem();

            var canvas = UiFactory.CreateCanvas("Canvas_Menu");
            var root = UiFactory.CreateRect("Root", canvas.transform);

            BuildBackground(root);
            BuildTitle(root);
            BuildFloatingBlobs(root);
            BuildLevelChips(root);
            BuildPlayButton(root);
            BuildHint(root);

            SceneTransition.FadeIn();
        }

        private void Update()
        {
            _time += Time.deltaTime;

            for (var i = 0; i < _floaters.Count; i++)
            {
                var rect = _floaters[i];
                if (rect == null)
                {
                    continue;
                }

                var phase = _floatPhases[i];
                var home = _floatHomes[i];
                rect.anchoredPosition = home + new Vector2(
                    Mathf.Sin(_time * 0.9f + phase) * 14f,
                    Mathf.Sin(_time * 1.4f + phase) * 22f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * 0.8f + phase) * 7f);
            }
        }

        private void BuildBackground(RectTransform root)
        {
            var background = UiFactory.CreateImage("Background", root, Color.white);
            background.sprite = ArtFactory.VerticalGradient(JuicePalette.SkyBottom, JuicePalette.SkyTop, 256);
            background.type = Image.Type.Simple;
            background.preserveAspect = false;

            var sun = UiFactory.CreateGlow("Sun", root, new Color(1f, 1f, 1f, 0.55f));
            UiFactory.SetRect(
                sun.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(140f, 150f),
                new Vector2(640f, 640f));

            var sunDisc = UiFactory.CreateDot("SunDisc", root, JuicePalette.WithAlpha(JuicePalette.Sun, 0.85f));
            UiFactory.SetRect(
                sunDisc.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(160f, 200f),
                new Vector2(230f, 230f));

            var haze = UiFactory.CreateGlow("Haze", root, new Color(1f, 1f, 1f, 0.45f));
            UiFactory.SetRect(
                haze.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(-160f, -220f),
                new Vector2(680f, 520f));
        }

        private void BuildTitle(RectTransform root)
        {
            var title = UiFactory.CreateText(
                "Title",
                root,
                "COLOR FLOW",
                132,
                JuicePalette.Paper,
                TextAnchor.MiddleCenter,
                true,
                true);
            UiFactory.SetRect(
                title.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 610f),
                new Vector2(1000f, 220f));

            var subtitle = UiFactory.CreateText(
                "Subtitle",
                root,
                "Attrape les blobs, peins le motif !",
                44,
                JuicePalette.Ink,
                TextAnchor.MiddleCenter);
            UiFactory.SetRect(
                subtitle.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 470f),
                new Vector2(900f, 70f));

            TweenRunner.Run(Tween.PopIn(title.transform, 0.6f));
        }

        private void BuildFloatingBlobs(RectTransform root)
        {
            var positions = new[]
            {
                new Vector2(-360f, 300f),
                new Vector2(-170f, 355f),
                new Vector2(20f, 300f),
                new Vector2(210f, 350f),
                new Vector2(390f, 295f)
            };

            for (var i = 0; i < positions.Length; i++)
            {
                var blob = UiFactory.CreateImage("Floater_" + i, root, JuicePalette.CreatureColor(i));
                blob.sprite = ArtFactory.Blob();
                blob.preserveAspect = true;
                blob.raycastTarget = false;

                var size = 120f + (i % 3) * 22f;
                var rect = blob.rectTransform;
                UiFactory.SetRect(
                    rect,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    positions[i],
                    new Vector2(size, size));

                _floaters.Add(rect);
                _floatHomes.Add(positions[i]);
                _floatPhases.Add(i * 1.3f);
            }
        }

        private void BuildLevelChips(RectTransform root)
        {
            _levels = LevelCatalog.All();
            if (_levels.Length == 0)
            {
                var error = UiFactory.CreateText(
                    "NoLevels",
                    root,
                    "Aucun niveau dans Resources/Levels",
                    44,
                    JuicePalette.Accent);
                UiFactory.SetRect(
                    error.rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -200f),
                    new Vector2(900f, 120f));
                return;
            }

            var count = _levels.Length;
            const float width = 300f;
            const float gap = 30f;
            var total = count * width + (count - 1) * gap;

            for (var i = 0; i < count; i++)
            {
                var index = i;
                var level = _levels[i];

                var chip = UiFactory.CreateRect("Chip_" + i, root);
                UiFactory.SetRect(
                    chip,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-total * 0.5f + width * 0.5f + i * (width + gap), -230f),
                    new Vector2(width, 150f));

                var shadow = UiFactory.CreateGlow("Shadow", chip, JuicePalette.ShadowSoft);
                UiFactory.Stretch(shadow.rectTransform, -18f, -26f, -18f, -14f);

                var border = UiFactory.CreateRounded("Border", chip, JuicePalette.Paper, 36f);
                UiFactory.Stretch(border.rectTransform, 0f, 0f, 0f, 0f);

                var panel = UiFactory.CreateRounded("Panel", chip, JuicePalette.Paper, 30f, true);
                UiFactory.Stretch(panel.rectTransform, 6f, 6f, 6f, 6f);

                var button = panel.gameObject.AddComponent<Button>();
                button.targetGraphic = panel;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => Select(index));
                JuicyButton.Attach(button, panel.rectTransform, 8f);

                var label = UiFactory.CreateText(
                    "Name",
                    panel.transform,
                    level.DisplayName,
                    36,
                    JuicePalette.Ink,
                    TextAnchor.MiddleCenter);
                UiFactory.Stretch(label.rectTransform, 12f, 52f, 12f, 8f);

                BuildPaletteDots(panel.transform, level);

                var star = UiFactory.CreateImage("Star", chip, JuicePalette.Sun);
                star.sprite = ArtFactory.Star(64, 5, 0.46f);
                star.preserveAspect = true;
                UiFactory.SetRect(
                    star.rectTransform,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-16f, -14f),
                    new Vector2(52f, 52f));

                _chipBorders.Add(border);
                _chipStars.Add(star);
            }

            var initial = 0;
            if (LevelSelection.Current != null)
            {
                for (var i = 0; i < _levels.Length; i++)
                {
                    if (_levels[i] == LevelSelection.Current)
                    {
                        initial = i;
                        break;
                    }
                }
            }

            Select(initial, false);
        }

        private static void BuildPaletteDots(Transform parent, LevelData level)
        {
            var palette = level.Palette;
            if (palette == null || palette.Length == 0)
            {
                return;
            }

            var count = Mathf.Min(3, palette.Length);
            const float dot = 30f;
            const float gap = 12f;
            var total = count * dot + (count - 1) * gap;

            for (var i = 0; i < count; i++)
            {
                if (!ColorUtility.TryParseHtmlString(palette[i], out var color))
                {
                    color = JuicePalette.Accent;
                }

                var image = UiFactory.CreateDot("Dot_" + i, parent, color);
                UiFactory.SetRect(
                    image.rectTransform,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(-total * 0.5f + dot * 0.5f + i * (dot + gap), 24f),
                    new Vector2(dot, dot));
            }
        }

        private void BuildPlayButton(RectTransform root)
        {
            var play = UiFactory.CreateButton3D(
                "Play",
                root,
                "JOUER",
                JuicePalette.Accent,
                Color.white,
                78,
                20f);

            UiFactory.SetRect(
                play.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 30f),
                new Vector2(720f, 190f));

            play.onClick.AddListener(StartSelected);

            var footer = UiFactory.CreateText(
                "Footer",
                root,
                "Sprint 1 — Juice & UI/UX",
                34,
                JuicePalette.WithAlpha(JuicePalette.Ink, 0.55f));
            UiFactory.SetRect(
                footer.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 70f),
                new Vector2(900f, 60f));
        }

        private void BuildHint(RectTransform root)
        {
            var hint = UiFactory.CreateCard("Hint", root, JuicePalette.WithAlpha(JuicePalette.Paper, 0.92f), 36f);
            UiFactory.SetRect(
                hint,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -450f),
                new Vector2(800f, 100f));

            var label = UiFactory.CreateText(
                "HintText",
                hint,
                "Tape un blob pour peindre sa couleur !",
                34,
                JuicePalette.InkSoft);
            UiFactory.Stretch(label.rectTransform, 26f, 8f, 26f, 8f);
        }

        private void Select(int index, bool withSound = true)
        {
            if (_levels == null || _levels.Length == 0)
            {
                return;
            }

            _selectedIndex = Mathf.Clamp(index, 0, _levels.Length - 1);
            LevelSelection.Current = _levels[_selectedIndex];

            for (var i = 0; i < _chipBorders.Count; i++)
            {
                var selected = i == _selectedIndex;
                _chipBorders[i].color = selected ? JuicePalette.Sun : JuicePalette.WithAlpha(JuicePalette.Sky, 0.28f);
                _chipStars[i].enabled = selected;
                if (selected)
                {
                    TweenRunner.Run(Tween.Pop(_chipBorders[i].transform.parent, 0.12f, 0.2f));
                }
            }

            if (withSound)
            {
                AudioManager.PlayPop();
            }
        }

        private void StartSelected()
        {
            if (_levels == null || _levels.Length == 0)
            {
                return;
            }

            LevelSelection.Current = _levels[_selectedIndex];
            SceneTransition.Load("Game");
        }
    }
}
