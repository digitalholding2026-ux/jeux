using System;
using System.Collections.Generic;

namespace ColorFlow.Core
{
    /// <summary>
    /// Génère des créatures avec un tirage pondéré par l'équité :
    ///
    ///     weight(c) = max(0, remainingPixels(c) − creaturesOnBelt(c))
    ///
    /// - remainingPixels(c) : pixels cibles non peints de la couleur c ;
    /// - creaturesOnBelt(c) : créatures de couleur c déjà présentes sur le tapis.
    ///
    /// Une couleur épuisée (remaining = 0) a donc toujours un poids nul : aucune
    /// créature "morte" ne peut apparaître. Si la somme des poids est nulle, aucune
    /// créature n'est générée : le joueur doit taper pour libérer du poids.
    ///
    /// Pur C# : le spawn est piloté par <see cref="Tick"/>.
    /// </summary>
    public sealed class CreatureSpawner
    {
        private const float MinInterval = 0.05f;

        private readonly List<RgbaColor> _colors = new List<RgbaColor>(8);
        private readonly List<int> _weights = new List<int>(8);
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
        public void Tick(float deltaTime, GridModel grid, IReadOnlyList<Creature> onBelt, IRandomSource random)
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
                if (SpawnNow(grid, onBelt, random) == null)
                {
                    // Somme des poids nulle : on attend un tap, pas de burst au tick suivant.
                    _timer = 0f;
                    return;
                }
            }
        }

        /// <summary>Génère immédiatement une créature (tests, debug).</summary>
        public Creature SpawnNow(GridModel grid, IReadOnlyList<Creature> onBelt, IRandomSource random)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (!TryPickColor(grid, onBelt, random, out var color))
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

        /// <summary>
        /// Poids de tirage d'une couleur : max(0, remaining − onBelt).
        /// </summary>
        public int GetWeight(GridModel grid, IReadOnlyList<Creature> onBelt, RgbaColor color)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var weight = CountRemaining(grid, color) - CountOnBelt(onBelt, color);
            return weight > 0 ? weight : 0;
        }

        /// <summary>Somme des poids de toutes les couleurs encore utiles.</summary>
        public int GetTotalWeight(GridModel grid, IReadOnlyList<Creature> onBelt)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            _colors.Clear();
            _weights.Clear();
            CollectRemaining(grid);

            var total = 0;
            for (var i = 0; i < _colors.Count; i++)
            {
                var weight = _weights[i] - CountOnBelt(onBelt, _colors[i]);
                if (weight > 0)
                {
                    total += weight;
                }
            }

            return total;
        }

        /// <summary>Nombre de pixels cibles encore non peints de cette couleur.</summary>
        public static int CountRemaining(GridModel grid, RgbaColor color)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (color.IsTransparent)
            {
                return 0;
            }

            var count = 0;
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    if (!grid.IsPainted(x, y) && grid.HasTargetAt(x, y) && grid.TargetColorAt(x, y).Equals(color))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Nombre de créatures de cette couleur présentes sur le tapis.</summary>
        public static int CountOnBelt(IReadOnlyList<Creature> onBelt, RgbaColor color)
        {
            if (onBelt == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < onBelt.Count; i++)
            {
                var creature = onBelt[i];
                if (creature != null && creature.Color.Equals(color))
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryPickColor(GridModel grid, IReadOnlyList<Creature> onBelt, IRandomSource random, out RgbaColor color)
        {
            _colors.Clear();
            _weights.Clear();
            CollectRemaining(grid);

            var total = 0;
            for (var i = 0; i < _weights.Count; i++)
            {
                var weight = _weights[i] - CountOnBelt(onBelt, _colors[i]);
                if (weight < 0)
                {
                    weight = 0;
                }

                _weights[i] = weight;
                total += weight;
            }

            if (total <= 0)
            {
                color = RgbaColor.Transparent;
                return false;
            }

            var roll = random.Range(0, total);
            var cumulative = 0;
            for (var i = 0; i < _weights.Count; i++)
            {
                cumulative += _weights[i];
                if (roll < cumulative)
                {
                    color = _colors[i];
                    return true;
                }
            }

            // Inatteignable si random respecte le contrat [0, total[, mais on reste sûr.
            color = _colors[_colors.Count - 1];
            return true;
        }

        /// <summary>Compte les pixels restants par couleur, dans l'ordre row-major.</summary>
        private void CollectRemaining(GridModel grid)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    if (grid.IsPainted(x, y) || !grid.HasTargetAt(x, y))
                    {
                        continue;
                    }

                    var candidate = grid.TargetColorAt(x, y);
                    var index = IndexOfColor(candidate);
                    if (index < 0)
                    {
                        _colors.Add(candidate);
                        _weights.Add(1);
                    }
                    else
                    {
                        _weights[index] = _weights[index] + 1;
                    }
                }
            }
        }

        private int IndexOfColor(RgbaColor color)
        {
            for (var i = 0; i < _colors.Count; i++)
            {
                if (_colors[i].Equals(color))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
