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
        private Text _coinLabel;
        private Text _hintLabel;
        private ShopPanel _shop;

        private void Awake()
        {
            AudioManager.Init();
            UiFactory.EnsureEventSystem();

            var canvas = UiFactory.CreateCanvas("Canvas_Menu");
            var root = UiFactory.CreateRect("Root", canvas.transform);

            BuildBackground(root);
            BuildFloatingBlobs(root);

            // Toute l'UI interactive vit dans l'aire sûre : aucun bouton ne peut
            // être rogné par une encoche ou une barre de navigation (20:9).
            var safe = UiFactory.CreateSafeArea("SafeArea", root);
            BuildTitle(safe);
            BuildWallet(safe);
            BuildLevelChips(safe);
            BuildHint(safe);

            // Barre basse étirée entre les bords sûrs : quelle que soit la
            // largeur (16:9 → 21:9), JOUER et BOUTIQUE gardent 24 px de marge
            // et ne peuvent jamais se chevaucher.
            var bottomBar = UiFactory.CreateRect("BottomBar", safe);
            UiFactory.SetRect(
                bottomBar,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(-48f, 200f));
            BuildPlayButton(bottomBar);
            BuildShopButton(bottomBar);
            BuildShop(root);

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
                126,
                JuicePalette.Paper,
                TextAnchor.MiddleCenter,
                true,
                true);
            UiFactory.SetRect(
                title.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -16f),
                new Vector2(900f, 168f));

            var subtitle = UiFactory.CreateText(
                "Subtitle",
                root,
                "Glisse, aligne, explose !",
                42,
                JuicePalette.Ink,
                TextAnchor.MiddleCenter);
            UiFactory.SetRect(
                subtitle.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -186f),
                new Vector2(860f, 62f));

            TweenRunner.Run(Tween.PopIn(title.transform, 0.6f));
        }

        private void BuildFloatingBlobs(RectTransform root)
        {
            var positions = new[]
            {
                new Vector2(-380f, -250f),
                new Vector2(-190f, -205f),
                new Vector2(0f, -255f),
                new Vector2(200f, -200f),
                new Vector2(390f, -260f)
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

        private void BuildWallet(RectTransform root)
        {
            var wallet = UiFactory.CreateCard("Wallet", root, JuicePalette.Paper, 36f);
            UiFactory.SetRect(
                wallet,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -20f),
                new Vector2(240f, 86f));

            var coin = UiFactory.CreateImage("Coin", wallet, Color.white);
            coin.sprite = ArtFactory.CoinIcon();
            coin.preserveAspect = true;
            UiFactory.SetRect(
                coin.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(18f, 0f),
                new Vector2(56f, 56f));

            _coinLabel = UiFactory.CreateText(
                "Coins",
                wallet,
                MetaProgress.Coins.ToString(),
                38,
                JuicePalette.Ink,
                TextAnchor.MiddleLeft);
            UiFactory.Stretch(_coinLabel.rectTransform, 84f, 6f, 16f, 6f);
        }

        private void BuildShopButton(RectTransform root)
        {
            var shop = UiFactory.CreateButton3D(
                "Shop",
                root,
                "BOUTIQUE",
                JuicePalette.Violet,
                Color.white,
                32,
                14f);
            UiFactory.SetRect(
                shop.GetComponent<RectTransform>(),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 46f),
                new Vector2(300f, 124f));

            // Icône dessinée par le code (sac de boutique) + libellé décalé.
            var face = shop.transform.Find("Face");
            if (face != null)
            {
                var label = face.Find("Label")?.GetComponent<RectTransform>();
                if (label != null)
                {
                    UiFactory.Stretch(label, 96f, 12f, 12f, 6f);
                    var labelText = label.GetComponent<Text>();
                    if (labelText != null)
                    {
                        labelText.resizeTextForBestFit = true;
                        labelText.resizeTextMinSize = 18;
                        labelText.resizeTextMaxSize = 32;
                    }
                }

                var icon = UiFactory.CreateImage("ShopIcon", face, Color.white);
                icon.sprite = ArtFactory.ShopIcon(128);
                icon.preserveAspect = true;
                UiFactory.SetRect(
                    icon.rectTransform,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(20f, 4f),
                    new Vector2(64f, 64f));
            }

            shop.onClick.AddListener(() => _shop?.Toggle());
        }

        private void BuildShop(RectTransform root)
        {
            _shop = new ShopPanel(root, RefreshWallet);
        }

        private void RefreshWallet()
        {
            if (_coinLabel != null)
            {
                _coinLabel.text = MetaProgress.Coins.ToString();
            }
        }

        private void BuildLevelChips(RectTransform root)
        {
            var all = LevelCatalog.All();
            var visible = new List<LevelData>(all.Length);
            for (var i = 0; i < all.Length; i++)
            {
                if (MetaProgress.IsUnlocked(all[i]))
                {
                    visible.Add(all[i]);
                }
            }

            _levels = visible.ToArray();
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
            const int columns = 3;
            const float width = 296f;
            const float chipHeight = 132f;
            const float gapX = 24f;
            const float gapY = 22f;
            const float firstY = -286f;

            for (var i = 0; i < count; i++)
            {
                var index = i;
                var level = _levels[i];

                var row = i / columns;
                var column = i % columns;
                var inRow = Mathf.Min(columns, count - row * columns);
                var x = -((inRow - 1) * (width + gapX)) * 0.5f + column * (width + gapX);
                var y = firstY - row * (chipHeight + gapY);

                var chip = UiFactory.CreateRect("Chip_" + i, root);
                UiFactory.SetRect(
                    chip,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(x, y),
                    new Vector2(width, chipHeight));

                var shadow = UiFactory.CreateGlow("Shadow", chip, JuicePalette.ShadowSoft);
                UiFactory.Stretch(shadow.rectTransform, -18f, -26f, -18f, -14f);

                var border = UiFactory.CreateRounded("Border", chip, JuicePalette.Paper, 34f);
                UiFactory.Stretch(border.rectTransform, 0f, 0f, 0f, 0f);

                var panel = UiFactory.CreateRounded("Panel", chip, JuicePalette.Paper, 28f, true);
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
                    32,
                    JuicePalette.Ink,
                    TextAnchor.MiddleCenter);
                UiFactory.Stretch(label.rectTransform, 12f, 44f, 12f, 8f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 18;
                label.resizeTextMaxSize = 32;

                BuildPaletteDots(panel.transform, level);

                var star = UiFactory.CreateImage("Star", chip, JuicePalette.Sun);
                star.sprite = ArtFactory.Star(64, 5, 0.46f);
                star.preserveAspect = true;
                UiFactory.SetRect(
                    star.rectTransform,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-14f, -12f),
                    new Vector2(46f, 46f));

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
                72,
                20f);

            UiFactory.SetRect(
                play.GetComponent<RectTransform>(),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 24f),
                new Vector2(580f, 176f));

            play.onClick.AddListener(StartSelected);
        }

        private void BuildHint(RectTransform root)
        {
            var hint = UiFactory.CreateCard("Hint", root, JuicePalette.WithAlpha(JuicePalette.Paper, 0.92f), 34f);
            UiFactory.SetRect(
                hint,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 224f),
                new Vector2(840f, 86f));

            var label = UiFactory.CreateText(
                "HintText",
                hint,
                "Glisse les blobs du tapis vers le motif !",
                32,
                JuicePalette.InkSoft);
            UiFactory.Stretch(label.rectTransform, 24f, 6f, 24f, 6f);
            _hintLabel = label;
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

            if (_hintLabel != null)
            {
                _hintLabel.text = _levels[_selectedIndex].GoalDescription;
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
