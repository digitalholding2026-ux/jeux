using System;
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

            _card = UiFactory.CreateCard("ShopCard", _overlay, JuicePalette.Paper, 52f);
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
            _overlay.gameObject.SetActive(true);
            _overlay.SetAsLastSibling();
            _dimGroup.alpha = 0f;
            TweenRunner.Run(Tween.Fade(_dimGroup, 0f, 1f, 0.2f));
            TweenRunner.Run(Tween.PopIn(_card, 0.3f));
            AudioManager.PlayClick();
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
                }
            }
            else if (MetaProgress.TryBuy(skin))
            {
                MetaProgress.SelectedSkin = skin;
                AudioManager.PlayPop();
            }
            else
            {
                AudioManager.PlayError();
            }

            Refresh();
            _onChanged?.Invoke();
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
                    row.Action.interactable = affordable;
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
