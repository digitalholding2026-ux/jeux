using System;
using UnityEngine;
using UnityEngine.UI;
using ColorFlow.Core;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Fabrique l'UI à l'exécution. Sprint 1 : thème clair, formes arrondies,
    /// ombres douces et boutons 3D — toujours sans aucun asset importé.
    /// </summary>
    public static class UiFactory
    {
        // Compatibilité avec les appels existants (thème clair).
        public static readonly Color Background = JuicePalette.SkyBottom;
        public static readonly Color Panel = JuicePalette.Paper;
        public static readonly Color PanelLight = JuicePalette.Cream;
        public static readonly Color TextPrimary = JuicePalette.Ink;
        public static readonly Color TextMuted = JuicePalette.InkSoft;
        public static readonly Color Accent = JuicePalette.Accent;
        public static readonly Color Success = JuicePalette.Mint;

        private static Font _font;

        /// <summary>Police ronde si le système en fournit une, sinon police intégrée.</summary>
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = LoadFont();
                }

                return _font;
            }
        }

        public static Color ToColor(RgbaColor color)
        {
            return new Color32(color.R, color.G, color.B, color.A);
        }

        /// <summary>Canvas portrait 1080×1920, Scale With Screen Size.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget = false)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>Image à coins arrondis (sprite généré + 9 tranches).</summary>
        public static Image CreateRounded(
            string name,
            Transform parent,
            Color color,
            float radius = 32f,
            bool raycastTarget = false)
        {
            var image = CreateImage(name, parent, color, raycastTarget);
            image.sprite = ArtFactory.RoundedRect(radius);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            return image;
        }

        /// <summary>Pastille ronde parfaite.</summary>
        public static Image CreateDot(string name, Transform parent, Color color, bool raycastTarget = false)
        {
            var image = CreateImage(name, parent, color, raycastTarget);
            image.sprite = ArtFactory.Circle(64);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            return image;
        }

        /// <summary>Halo/ombre douce : dégradé radial généré par le code.</summary>
        public static Image CreateGlow(string name, Transform parent, Color color)
        {
            var image = CreateImage(name, parent, color);
            image.sprite = ArtFactory.RadialGlow(128);
            image.type = Image.Type.Simple;
            return image;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter,
            bool bold = true,
            bool outline = false)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            if (outline)
            {
                var effect = text.gameObject.AddComponent<Outline>();
                effect.effectColor = JuicePalette.WithAlpha(JuicePalette.Ink, 0.85f);
                effect.effectDistance = new Vector2(4f, -4f);
                effect.useGraphicAlpha = true;
            }

            return text;
        }

        /// <summary>Bulle / carte arrondie avec ombre douce derrière (à dimensionner par l'appelant).</summary>
        public static RectTransform CreateCard(
            string name,
            Transform parent,
            Color color,
            float radius = 40f,
            bool withShadow = true)
        {
            var card = CreateRect(name, parent);

            if (withShadow)
            {
                var shadow = CreateGlow("Shadow", card, JuicePalette.ShadowSoft);
                Stretch(shadow.rectTransform, -26f, -34f, -26f, -18f);
            }

            var panel = CreateRounded("Panel", card, color, radius);
            Stretch(panel.rectTransform, 0f, 0f, 0f, 0f);
            return card;
        }

        /// <summary>
        /// Bouton « 3D » : socle plus foncé, face colorée, reflet et ombre portée.
        /// Le retour visuel est géré par <see cref="JuicyButton"/>.
        /// </summary>
        public static Button CreateButton3D(
            string name,
            Transform parent,
            string label,
            Color face,
            Color labelColor,
            int fontSize = 52,
            float depth = 16f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var root = (RectTransform)go.transform;
            root.SetParent(parent, false);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);

            // Zone cliquable transparente qui couvre tout le bouton.
            var hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            var shadow = CreateGlow("Shadow", root, JuicePalette.Shadow);
            Stretch(shadow.rectTransform, -30f, -56f, -30f, -12f);

            var baseImage = CreateRounded("Base", root, JuicePalette.Darken(face, 0.28f), 30f);
            Stretch(baseImage.rectTransform, 0f, 0f, 0f, 0f);

            var faceImage = CreateRounded("Face", root, face, 30f);
            Stretch(faceImage.rectTransform, 0f, depth, 0f, 0f);

            var gloss = CreateRounded("Gloss", faceImage.transform, JuicePalette.Gloss, 22f);
            Stretch(gloss.rectTransform, 22f, depth * 0.9f, 22f, 26f);

            var text = CreateText("Label", faceImage.transform, label, fontSize, labelColor);
            Stretch(text.rectTransform, 16f, 12f, 16f, 6f);
            var textShadow = text.gameObject.AddComponent<Shadow>();
            textShadow.effectColor = JuicePalette.WithAlpha(JuicePalette.Darken(labelColor, 0.55f), 0.45f);
            textShadow.effectDistance = new Vector2(0f, -3f);

            JuicyButton.Attach(button, faceImage.rectTransform, depth);
            return button;
        }

        /// <summary>Ancien nom conservé : construit désormais un bouton 3D.</summary>
        public static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Color background,
            Color labelColor,
            int fontSize = 48)
        {
            return CreateButton3D(name, parent, label, background, labelColor, fontSize);
        }

        /// <summary>Place un RectTransform avec des ancres explicites.</summary>
        public static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        /// <summary>Étire un RectTransform sur son parent avec des marges.</summary>
        public static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static Font LoadFont()
        {
            try
            {
                var installed = Font.GetOSInstalledFontNames();
                var candidates = new[]
                {
                    "Arial Rounded MT Bold",
                    "Arial Rounded MT",
                    "Comic Sans MS",
                    "Trebuchet MS",
                    "Verdana"
                };

                foreach (var candidate in candidates)
                {
                    for (var i = 0; i < installed.Length; i++)
                    {
                        if (!string.Equals(installed[i], candidate, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var font = Font.CreateDynamicFontFromOSFont(candidate, 64);
                        if (font != null)
                        {
                            return font;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Sur Android, aucune police système de ce nom : on retombe sur la police intégrée.
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
