using System;
using System.Collections.Generic;

namespace ColorFlow.Core
{
    /// <summary>
    /// Génère des créatures dont la couleur existe encore dans le motif cible.
    /// Le spawn est piloté par <see cref="Tick"/> (aucune dépendance Unity).
    /// </summary>
    public sealed class CreatureSpawner
    {
        private const float MinInterval = 0.05f;

        private readonly List<RgbaColor> _availableColors = new List<RgbaColor>(8);
        private float _interval;
        private float _timer;
        private int _nextId = 1;

        public CreatureSpawner(float interval)
        {
            Interval = interval;
        }

        /// <summary>Créature générée (avant tentative d'ajout au tapis).</summary>
        public event Action<Creature> Spawned;

        /// <summary>Intervalle entre deux créatures, en secondes.</summary>
        public float Interval
        {
            get => _interval;
            set => _interval = value < MinInterval ? MinInterval : value;
        }

        public bool Enabled { get; set; } = true;

        public float TimeToNextSpawn => _interval - _timer;

        /// <summary>Avance le temps et génère les créatures dues.</summary>
        public void Tick(float deltaTime, GridModel grid, IRandomSource random)
        {
            if (!Enabled || deltaTime <= 0f || grid == null || random == null)
            {
                return;
            }

            _timer += deltaTime;

            // Garde-fou : après une pause longue, on ne vide pas la file d'un coup.
            var iterations = 0;
            while (_timer >= _interval && iterations++ < 8)
            {
                _timer -= _interval;
                if (SpawnNow(grid, random) == null)
                {
                    _timer = 0f;
                    return;
                }
            }
        }

        /// <summary>Génère immédiatement une créature (tests, debug).</summary>
        public Creature SpawnNow(GridModel grid, IRandomSource random)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (!TryPickColor(grid, random, out var color))
            {
                return null;
            }

            var creature = new Creature(_nextId++, color);
            Spawned?.Invoke(creature);
            return creature;
        }

        public void Reset()
        {
            _timer = 0f;
            _nextId = 1;
        }

        private bool TryPickColor(GridModel grid, IRandomSource random, out RgbaColor color)
        {
            _availableColors.Clear();

            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    if (grid.IsPainted(x, y) || !grid.HasTargetAt(x, y))
                    {
                        continue;
                    }

                    var candidate = grid.TargetColorAt(x, y);
                    if (!_availableColors.Contains(candidate))
                    {
                        _availableColors.Add(candidate);
                    }
                }
            }

            if (_availableColors.Count == 0)
            {
                color = RgbaColor.Transparent;
                return false;
            }

            color = _availableColors[random.Range(0, _availableColors.Count)];
            return true;
        }
    }
}
