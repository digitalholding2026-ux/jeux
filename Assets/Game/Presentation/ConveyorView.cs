using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ColorFlow.Core;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Tapis roulant : plateau arrondi, rayures qui défilent et cinq emplacements.
    /// Chaque créature est un blob qui respire, arrive en glissant depuis la droite
    /// avec un rebond élastique et réagit au tap.
    /// </summary>
    public sealed class ConveyorView
    {
        private const float SlotSize = 150f;
        private const float SlotGap = 24f;
        private const int StripeCount = 10;
        private const float BeltWidth = 1000f;
        private const float BeltHeight = 262f;

        private readonly RectTransform _root;
        private readonly RectTransform[] _slotRoots;
        private readonly RectTransform[] _holders;
        private readonly RectTransform[] _breathers;
        private readonly Image[] _blobs;
        private readonly Image[] _shadows;
        private readonly Button[] _buttons;
        private readonly Creature[] _creatures;
        private readonly RectTransform[] _stripes;
        private readonly float[] _phases;
        private readonly int _capacity;

        private float _time;
        private float _scroll;

        public ConveyorView(RectTransform root, int capacity = ConveyorQueue<Creature>.DefaultCapacity)
        {
            _root = root;
            _capacity = Mathf.Max(1, capacity);
            _slotRoots = new RectTransform[_capacity];
            _holders = new RectTransform[_capacity];
            _breathers = new RectTransform[_capacity];
            _blobs = new Image[_capacity];
            _shadows = new Image[_capacity];
            _buttons = new Button[_capacity];
            _creatures = new Creature[_capacity];
            _phases = new float[_capacity];

            BuildBelt();
            _stripes = BuildStripes();

            for (var i = 0; i < _capacity; i++)
            {
                BuildSlot(i);
            }
        }

        public event Action<Creature> CreatureTapped;

        public Button GetButton(int index)
        {
            return index >= 0 && index < _capacity ? _buttons[index] : null;
        }

        private void BuildBelt()
        {
            var tray = UiFactory.CreateGlow("TrayShadow", _root, JuicePalette.Shadow);
            UiFactory.SetRect(
                tray.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(BeltWidth + 60f, BeltHeight + 90f));

            var trayPlate = UiFactory.CreateRounded("Tray", _root, JuicePalette.BeltTray, 50f);
            UiFactory.SetRect(
                trayPlate.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f),
                new Vector2(BeltWidth + 46f, BeltHeight + 46f));

            var belt = UiFactory.CreateRounded("Belt", _root, JuicePalette.Belt, 40f);
            UiFactory.SetRect(
                belt.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(BeltWidth, BeltHeight));
        }

        private RectTransform[] BuildStripes()
        {
            var stripes = new RectTransform[StripeCount];
            var inner = BeltWidth - 150f;
            var spacing = inner / StripeCount;

            for (var i = 0; i < StripeCount; i++)
            {
                var stripe = UiFactory.CreateRounded("Stripe_" + i, _root, JuicePalette.BeltStripe, 14f);
                var rect = stripe.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(24f, BeltHeight * 0.90f);
                rect.anchoredPosition = new Vector2(-inner * 0.5f + i * spacing, 0f);
                rect.localRotation = Quaternion.Euler(0f, 0f, 24f);
                stripes[i] = rect;
            }

            return stripes;
        }

        private void BuildSlot(int index)
        {
            var totalWidth = _capacity * SlotSize + (_capacity - 1) * SlotGap;
            var x = -totalWidth * 0.5f + SlotSize * 0.5f + index * (SlotSize + SlotGap);

            var slot = UiFactory.CreateRect("Slot_" + index, _root);
            slot.anchorMin = new Vector2(0.5f, 0.5f);
            slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.sizeDelta = new Vector2(SlotSize, SlotSize);
            slot.anchoredPosition = new Vector2(x, 12f);

            var shadow = UiFactory.CreateGlow("Shadow", slot, JuicePalette.Shadow);
            UiFactory.SetRect(
                shadow.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -74f),
                new Vector2(150f, 46f));

            var holder = UiFactory.CreateRect("Holder", slot);
            UiFactory.Stretch(holder, 0f, 0f, 0f, 0f);

            var breather = UiFactory.CreateRect("Breather", holder);
            UiFactory.Stretch(breather, 0f, 0f, 0f, 0f);

            var blob = UiFactory.CreateImage("Blob", breather, Color.white, true);
            blob.sprite = ArtFactory.Blob();
            blob.preserveAspect = true;
            UiFactory.SetRect(
                blob.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 6f),
                new Vector2(164f, 164f));

            var button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = blob;
            button.transition = Selectable.Transition.None;
            var captured = index;
            button.onClick.AddListener(() => OnSlotClicked(captured));

            _slotRoots[index] = slot;
            _holders[index] = holder;
            _breathers[index] = breather;
            _blobs[index] = blob;
            _shadows[index] = shadow;
            _buttons[index] = button;
            _phases[index] = index * 0.9f;

            holder.gameObject.SetActive(false);
            button.interactable = false;
        }

        /// <summary>Aligne l'affichage sur l'état courant de la file.</summary>
        public void Render(IReadOnlyList<Creature> creatures)
        {
            var count = creatures != null ? creatures.Count : 0;

            for (var i = 0; i < _capacity; i++)
            {
                var hasCreature = i < count;
                _buttons[i].interactable = hasCreature;

                if (!hasCreature)
                {
                    _creatures[i] = null;
                    _holders[i].gameObject.SetActive(false);
                    continue;
                }

                var creature = creatures[i];
                var previous = _creatures[i];
                _creatures[i] = creature;
                _blobs[i].color = UiFactory.ToColor(creature.Color);
                _holders[i].gameObject.SetActive(true);

                if (ReferenceEquals(previous, creature))
                {
                    continue;
                }

                if (previous == null)
                {
                    PlaySpawn(i);
                }
                else
                {
                    PlayShift(i);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;

            for (var i = 0; i < _capacity; i++)
            {
                if (!_holders[i].gameObject.activeSelf)
                {
                    continue;
                }

                var phase = _phases[i];
                var breathe = 1f + Mathf.Sin(_time * 2.1f + phase) * 0.045f;
                _breathers[i].localScale = new Vector3(breathe, 1f / breathe, 1f);
                _breathers[i].anchoredPosition = new Vector2(0f, Mathf.Sin(_time * 1.7f + phase) * 5f);
                _shadows[i].rectTransform.sizeDelta = new Vector2(150f * (0.9f + 0.1f * breathe), 46f);
            }

            _scroll += deltaTime * 150f;
            var inner = BeltWidth - 150f;
            var spacing = inner / StripeCount;

            for (var i = 0; i < _stripes.Length; i++)
            {
                var offset = Mathf.Repeat(i * spacing + _scroll, inner);
                _stripes[i].anchoredPosition = new Vector2(-inner * 0.5f + offset, 0f);
            }
        }

        public bool TryGetSlotScreenCenter(Creature creature, out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;
            if (creature == null)
            {
                return false;
            }

            var index = Array.IndexOf(_creatures, creature);
            if (index < 0 || index >= _capacity || _breathers[index] == null)
            {
                return false;
            }

            screenPosition = RectTransformUtility.WorldToScreenPoint(null, _breathers[index].position);
            return true;
        }

        /// <summary>Petit rebond quand la créature est touchée.</summary>
        public void PlayTapBounce(Creature creature)
        {
            var index = Array.IndexOf(_creatures, creature);
            if (index < 0)
            {
                return;
            }

            TweenRunner.Run(Tween.Pop(_holders[index], 0.22f, 0.16f));
        }

        /// <summary>Secousse latérale quand la créature est refusée.</summary>
        public void PlayRefuse(Creature creature)
        {
            var index = Array.IndexOf(_creatures, creature);
            if (index < 0)
            {
                return;
            }

            TweenRunner.Run(Tween.Shake(_holders[index], Vector2.zero, 14f, 0.28f));
        }

        private void PlaySpawn(int index)
        {
            var holder = _holders[index];
            holder.anchoredPosition = new Vector2(320f, 0f);
            holder.localScale = Vector3.one * 0.25f;

            TweenRunner.Run(Tween.Scale(holder, Vector3.one * 0.25f, Vector3.one, 0.55f, Ease.OutElastic));
            TweenRunner.Run(Tween.Move(holder, new Vector2(320f, 0f), Vector2.zero, 0.38f, Ease.OutCubic));
        }

        private void PlayShift(int index)
        {
            var holder = _holders[index];
            holder.localScale = Vector3.one;
            TweenRunner.Run(Tween.Move(holder, new Vector2(SlotSize + SlotGap, 0f), Vector2.zero, 0.22f, Ease.OutCubic));
        }

        private void OnSlotClicked(int index)
        {
            var creature = _creatures[index];
            if (creature != null)
            {
                CreatureTapped?.Invoke(creature);
            }
        }
    }
}
