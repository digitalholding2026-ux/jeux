using System;
using UnityEngine;
using UnityEngine.UI;
using ColorFlow.Core;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Fabrique l'UI à l'exécution. Sprint 0 : pas de prefabs ni d'art final,
    /// uniquement des Image/Text/Bouton placeholders.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color Background = new Color(0.055f, 0.063f, 0.094f, 1f);
        public static readonly Color Panel = new Color(0.106f, 0.122f, 0.180f, 1f);
        public static readonly Color PanelLight = new Color(0.149f, 0.169f, 0.239f, 1f);
        public static readonly Color TextPrimary = new Color(0.961f, 0.969f, 1f, 1f);
        public static readonly Color TextMuted = new Color(0.655f, 0.682f, 0.776f, 1f);
        public static readonly Color Accent = new Color(1f, 0.420f, 0.290f, 1f);
        public static readonly Color Success = new Color(0.298f, 0.851f, 0.392f, 1f);

        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        public static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Color background,
            Color labelColor,
            int fontSize = 48)
        {
            var image = CreateImage(name, parent, background, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText("Label", image.transform, label, fontSize, labelColor);
            return button;
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
    }
}
