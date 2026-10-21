using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ColorFlow.Core;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Tapis roulant : 5 emplacements fixes, remplis depuis la file du domaine.
    /// Aucune animation au Sprint 0, uniquement des Image colorées cliquables.
    /// </summary>
    public sealed class ConveyorView
    {
        private const float SlotSize = 150f;
        private const float SlotGap = 24f;

        private readonly Image[] _slotImages;
        private readonly Button[] _slotButtons;
        private readonly Creature[] _slotCreatures;
        private readonly int _capacity;

        public ConveyorView(RectTransform root, int capacity = ConveyorQueue<Creature>.DefaultCapacity)
        {
            _capacity = capacity;
            _slotImages = new Image[capacity];
            _slotButtons = new Button[capacity];
            _slotCreatures = new Creature[capacity];

            var belt = UiFactory.CreateImage("Belt", root, new Color(1f, 1f, 1f, 0.045f));
            UiFactory.Stretch(belt.rectTransform, 40f, 60f, 40f, 60f);

            var totalWidth = capacity * SlotSize + (capacity - 1) * SlotGap;

            for (var i = 0; i < capacity; i++)
            {
                var index = i;
                var image = UiFactory.CreateImage("Slot_" + i, root, Color.clear, true);
                var rect = image.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(SlotSize, SlotSize);
                rect.anchoredPosition = new Vector2(
                    -totalWidth * 0.5f + SlotSize * 0.5f + i * (SlotSize + SlotGap),
                    0f);

                image.enabled = false;

                var button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnSlotClicked(index));

                _slotImages[i] = image;
                _slotButtons[i] = button;
            }
        }

        public event Action<Creature> CreatureTapped;

        public Button GetButton(int index)
        {
            return index >= 0 && index < _capacity ? _slotButtons[index] : null;
        }

        /// <summary>Aligne l'affichage sur l'état courant de la file.</summary>
        public void Render(IReadOnlyList<Creature> creatures)
        {
            var count = creatures != null ? creatures.Count : 0;

            for (var i = 0; i < _capacity; i++)
            {
                if (i < count)
                {
                    var creature = creatures[i];
                    _slotCreatures[i] = creature;
                    _slotImages[i].color = UiFactory.ToColor(creature.Color);
                    _slotImages[i].enabled = true;
                }
                else
                {
                    _slotCreatures[i] = null;
                    _slotImages[i].enabled = false;
                }
            }
        }

        private void OnSlotClicked(int index)
        {
            var creature = _slotCreatures[index];
            if (creature != null)
            {
                CreatureTapped?.Invoke(creature);
            }
        }
    }
}
