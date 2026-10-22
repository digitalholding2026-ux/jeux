using System;
using System.Collections;
using ColorFlow.Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Boutique affichée en surimpression du menu principal : quatre skins,
    /// achat en pièces d'or, équipement immédiat, sauvegarde PlayerPrefs.
    /// Tout est construit par le code, aucun asset.
    /// </summary>
    public sealed class ShopPanel
    {
        private sealed class Row
        {
            public SkinId Skin;
            public Image Preview;
            public Image Accessory;
            public Text Name;
            public Text Price;
            public Button Action;
            public Text ActionLabel;
            public Image Face;
            public Image Base;
        }

        private readonly RectTransform _overlay;
        private readonly CanvasGroup _dimGroup;
        private readonly RectTransform _card;
        private readonly Text _coins;
        private readonly Row[] _rows;
        private readonly Action _onChanged;
        private readonly Text _feedback;
        private readonly CanvasGroup _feedbackGroup;
        private Coroutine _feedbackRoutine;
        private Coroutine _shakeRoutine;

        public ShopPanel(RectTransform root, Action onChanged)
        {
            _onChanged = onChanged;

            _overlay = UiFactory.CreateRect("ShopOverlay", root);

            var dim = UiFactory.CreateImage(
                "Dim",
                _overlay,
                JuicePalette.WithAlpha(JuicePalette.Ink, 0.52f),
                true);
            UiFactory.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            _dimGroup = dim.gameObject.AddComponent<CanvasGroup>();

            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.targetGraphic = dim;
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Close);

            _card = UiFactory.CreateCard("ShopCard", UiFactory.CreateSafeArea("ShopSafe", _overlay), JuicePalette.Paper, 52f);
            UiFactory.SetRect(
                _card,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(960f, 1340f));

            // La carte doit bloquer les clics : sinon un tap dans une zone vide
            // traverse jusqu'au fond assombri et ferme la boutique.
            var cardPanel = _card.Find("Panel")?.GetComponent<Image>();
            if (cardPanel != null)
            {
                cardPanel.raycastTarget = true;
            }

            var title = UiFactory.CreateText("Title", _card, "BOUTIQUE", 82, JuicePalette.Ink);
            UiFactory.SetRect(
                title.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 560f),
                new Vector2(800f, 120f));

            // --- Bourse ---
            var wallet = UiFactory.CreateRounded("Wallet", _card, JuicePalette.Cream, 40f);
            UiFactory.SetRect(
                wallet.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 446f),
                new Vector2(300f, 86f));

            var coin = UiFactory.CreateImage("Coin", wallet.transform, Color.white);
            coin.sprite = ArtFactory.CoinIcon();
            coin.preserveAspect = true;
            UiFactory.SetRect(
                coin.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(20f, 0f),
                new Vector2(58f, 58f));

            _coins = UiFactory.CreateText("Coins", wallet.transform, "0", 40, JuicePalette.Ink, TextAnchor.MiddleLeft);
            UiFactory.Stretch(_coins.rectTransform, 92f, 6f, 20f, 6f);

            // --- Lignes de skins ---
            var all = SkinCatalog.All;
            _rows = new Row[all.Length];

            const float rowHeight = 196f;
            const float rowGap = 18f;
            var firstY = 300f;

            for (var i = 0; i < all.Length; i++)
            {
                _rows[i] = BuildRow(all[i], firstY - i * (rowHeight + rowGap), rowHeight);
            }

            // Message de retour (achat réussi / fonds insuffisants).
            _feedback = UiFactory.CreateText("Feedback", _card, string.Empty, 30, JuicePalette.PinkDeep);
            UiFactory.SetRect(
                _feedback.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -464f),
                new Vector2(840f, 48f));
            _feedbackGroup = _feedback.gameObject.AddComponent<CanvasGroup>();
            _feedbackGroup.alpha = 0f;
            _feedbackGroup.blocksRaycasts = false;

            var close = UiFactory.CreateButton3D(
                "Close",
                _card,
                "FERMER",
                JuicePalette.Sky,
                Color.white,
                44,
                14f);
            UiFactory.SetRect(
                close.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -558f),
                new Vector2(340f, 120f));
            close.onClick.AddListener(Close);

            _overlay.gameObject.SetActive(false);
            Refresh();
        }

        public bool IsOpen => _overlay.gameObject.activeSelf;

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            Refresh();
            FitToSafeArea();
            _overlay.gameObject.SetActive(true);
            _overlay.SetAsLastSibling();
            _dimGroup.alpha = 0f;
            _feedbackGroup.alpha = 0f;
            TweenRunner.Run(Tween.Fade(_dimGroup, 0f, 1f, 0.2f));
            TweenRunner.Run(Tween.PopIn(_card, 0.3f));
            AudioManager.PlayClick();
        }

        /// <summary>Réduit la carte si l'aire sûre est plus petite que 960×1340.</summary>
        private void FitToSafeArea()
        {
            var safe = _card.parent as RectTransform;
            if (safe == null)
            {
                return;
            }

            var size = safe.rect.size;
            if (size.x <= 1f || size.y <= 1f)
            {
                _card.localScale = Vector3.one;
                return;
            }

            var scale = Mathf.Min(1f, (size.x - 24f) / 960f, (size.y - 24f) / 1340f);
            _card.localScale = Vector3.one * Mathf.Clamp(scale, 0.55f, 1f);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _overlay.gameObject.SetActive(false);
            AudioManager.PlayClick();
        }

        private Row BuildRow(SkinInfo info, float y, float height)
        {
            var row = new Row { Skin = info.Id };

            var rowCard = UiFactory.CreateCard("Row_" + info.Id, _card, JuicePalette.Cream, 32f);
            UiFactory.SetRect(
                rowCard,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, y),
                new Vector2(880f, height));

            var preview = UiFactory.CreateImage(
                "Preview",
                rowCard,
                JuicePalette.CreatureColor((int)info.Id));
            preview.sprite = ArtFactory.Blob();
            preview.preserveAspect = true;
            UiFactory.SetRect(
                preview.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-330f, 0f),
                new Vector2(150f, 150f));
            row.Preview = preview;

            var accessory = UiFactory.CreateImage("Accessory", preview.transform, Color.white);
            accessory.preserveAspect = true;
            UiFactory.SetRect(
                accessory.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f),
                new Vector2(112f, 96f));
            row.Accessory = accessory;

            row.Name = UiFactory.CreateText(
                "Name",
                rowCard,
                info.DisplayName,
                38,
                JuicePalette.Ink,
                TextAnchor.MiddleLeft);
            UiFactory.SetRect(
                row.Name.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-70f, 34f),
                new Vector2(400f, 58f));

            row.Price = UiFactory.CreateText(
                "Price",
                rowCard,
                string.Empty,
                27,
                JuicePalette.InkSoft,
                TextAnchor.MiddleLeft);
            UiFactory.SetRect(
                row.Price.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-70f, -32f),
                new Vector2(400f, 44f));

            var action = UiFactory.CreateButton3D(
                "Action",
                rowCard,
                "ACHETER",
                JuicePalette.Violet,
                Color.white,
                32,
                10f);
            UiFactory.SetRect(
                action.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(286f, 0f),
                new Vector2(250f, 100f));
            row.Action = action;
            row.ActionLabel = action.transform.Find("Face/Label")?.GetComponent<Text>();
            row.Face = action.transform.Find("Face")?.GetComponent<Image>();
            row.Base = action.transform.Find("Base")?.GetComponent<Image>();

            var skin = info.Id;
            action.onClick.AddListener(() => OnAction(skin));
            return row;
        }

        private void OnAction(SkinId skin)
        {
            if (MetaProgress.Owns(skin))
            {
                if (MetaProgress.SelectedSkin != skin)
                {
                    MetaProgress.SelectedSkin = skin;
                    AudioManager.PlayClick();
                    ShowFeedback("Skin équipé !", JuicePalette.MintDeep);
                    TweenRunner.Run(Tween.Pop(FindRow(skin)?.Preview.transform, 0.16f, 0.3f));
                }

                Refresh();
                _onChanged?.Invoke();
                return;
            }

            if (MetaProgress.TryBuy(skin))
            {
                MetaProgress.SelectedSkin = skin;
                AudioManager.PlayPop();
                Haptics.Impact();
                ShowFeedback("Skin débloqué et équipé !", JuicePalette.MintDeep);
                Celebrate(skin);
                Refresh();
                _onChanged?.Invoke();
                return;
            }

            // Fonds insuffisants : message rouge bref + secousse du bouton.
            AudioManager.PlayError();
            Haptics.Tap();
            ShowFeedback("Pas assez de pièces !", JuicePalette.PinkDeep);
            ShakeRow(skin);
            Refresh();
        }

        private Row FindRow(SkinId skin)
        {
            for (var i = 0; i < _rows.Length; i++)
            {
                if (_rows[i].Skin == skin)
                {
                    return _rows[i];
                }
            }

            return null;
        }

        private void ShakeRow(SkinId skin)
        {
            var row = FindRow(skin);
            if (row?.Action == null)
            {
                return;
            }

            var rect = row.Action.GetComponent<RectTransform>();
            if (_shakeRoutine != null)
            {
                TweenRunner.Stop(_shakeRoutine);
                rect.anchoredPosition = new Vector2(286f, 0f);
            }

            _shakeRoutine = TweenRunner.Run(Tween.Shake(rect, new Vector2(286f, 0f), 18f, 0.36f));
        }

        /// <summary>Pop du blob, du portefeuille et gerbe d'étincelles à l'achat.</summary>
        private void Celebrate(SkinId skin)
        {
            var row = FindRow(skin);
            if (row == null)
            {
                return;
            }

            TweenRunner.Run(Tween.Pop(row.Preview.transform, 0.28f, 0.42f));
            TweenRunner.Run(Tween.Pop(_coins.rectTransform, 0.14f, 0.3f));

            var local = _overlay.InverseTransformPoint(row.Preview.transform.position);
            UiParticles.Sparkle(_overlay, local, JuicePalette.Sun, 12);
            UiParticles.Sparkle(_overlay, local, JuicePalette.Mint, 8);
        }

        private void ShowFeedback(string message, Color color)
        {
            _feedback.text = message;
            _feedback.color = color;

            if (_feedbackRoutine != null)
            {
                TweenRunner.Stop(_feedbackRoutine);
            }

            _feedbackGroup.alpha = 1f;
            _feedback.rectTransform.localScale = Vector3.one * 0.7f;
            TweenRunner.Run(Tween.Scale(
                _feedback.rectTransform,
                Vector3.one * 0.7f,
                Vector3.one,
                0.22f,
                Ease.OutBack));
            _feedbackRoutine = TweenRunner.Run(FadeFeedback(1.25f));
        }

        private IEnumerator FadeFeedback(float delay)
        {
            yield return Tween.Delay(delay);
            yield return Tween.Fade(_feedbackGroup, 1f, 0f, 0.35f);
            _feedbackRoutine = null;
        }

        private void Refresh()
        {
            _coins.text = MetaProgress.Coins.ToString();
            var selected = MetaProgress.SelectedSkin;

            for (var i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                var info = SkinCatalog.Get(row.Skin);
                var owned = MetaProgress.Owns(row.Skin);
                var affordable = MetaProgress.Coins >= info.Price;

                row.Name.text = info.DisplayName;
                row.Price.text = owned ? info.Description : info.Price + " pièces";
                row.Price.color = owned
                    ? JuicePalette.MintDeep
                    : (affordable ? JuicePalette.InkSoft : JuicePalette.PinkDeep);

                var accessory = ArtFactory.SkinAccessory(row.Skin);
                row.Accessory.enabled = accessory != null;
                if (accessory != null)
                {
                    row.Accessory.sprite = accessory;
                }

                if (!owned)
                {
                    row.ActionLabel.text = "ACHETER";
                    SetFace(row, affordable ? JuicePalette.Violet : JuicePalette.InkSoft);

                    // Toujours cliquable : un joueur sans assez de pièces doit
                    // pouvoir déclencher le feedback « Pas assez de pièces ! ».
                    row.Action.interactable = true;
                }
                else if (selected == row.Skin)
                {
                    row.ActionLabel.text = "ÉQUIPÉ";
                    SetFace(row, JuicePalette.Mint);
                    row.Action.interactable = false;
                }
                else
                {
                    row.ActionLabel.text = "ÉQUIPER";
                    SetFace(row, JuicePalette.Sky);
                    row.Action.interactable = true;
                }
            }
        }

        private static void SetFace(Row row, Color color)
        {
            if (row.Face != null)
            {
                row.Face.color = color;
            }

            if (row.Base != null)
            {
                row.Base.color = JuicePalette.Darken(color, 0.28f);
            }
        }
    }
}
