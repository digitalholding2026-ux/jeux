using System;
using System.Collections.Generic;

namespace ColorFlow.Core
{
    /// <summary>
    /// Budget de peinture « équitable » des créatures spéciales.
    ///
    /// Une bombe ou une ligne peint plusieurs pixels d'un coup, éventuellement
    /// d'autres couleurs. Sans garde-fou, elle pourrait épuiser une couleur
    /// alors qu'une créature de cette couleur attend sur le tapis : cette
    /// créature deviendrait inutilisable (« morte »), ce que la règle d'équité
    /// interdit depuis le Sprint 0.
    ///
    /// Pour chaque couleur c :
    ///     allowed(c) = pixelsRestants(c) − créaturesDeCouleurCSurLeTapis
    ///
    /// La créature en cours de tap n'est pas comptée dans le tapis : elle est
    /// consommée par l'action, son propre pixel reste donc toujours autorisé.
    /// </summary>
    public sealed class PaintAllowance
    {
        private readonly List<RgbaColor> _colors = new List<RgbaColor>(8);
        private readonly List<int> _allowed = new List<int>(8);

        public PaintAllowance(GridModel grid, IReadOnlyList<Creature> onBelt, Creature tapped)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            Grid = grid;

            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    if (grid.IsPainted(x, y) || !grid.HasTargetAt(x, y))
                    {
                        continue;
                    }

                    Add(grid.TargetColorAt(x, y), 1);
                }
            }

            if (onBelt == null)
            {
                return;
            }

            for (var i = 0; i < onBelt.Count; i++)
            {
                var creature = onBelt[i];
                if (creature == null || ReferenceEquals(creature, tapped))
                {
                    continue;
                }

                Add(creature.Color, -1);
            }
        }

        public GridModel Grid { get; }

        /// <summary>Nombre de pixels encore peignables de cette couleur.</summary>
        public int AllowedFor(RgbaColor color)
        {
            var index = IndexOf(color);
            return index >= 0 ? _allowed[index] : 0;
        }

        public bool CanPaint(RgbaColor color)
        {
            return AllowedFor(color) > 0;
        }

        /// <summary>Consomme un pixel (à appeler après chaque peinture réussie).</summary>
        public void Consume(RgbaColor color)
        {
            var index = IndexOf(color);
            if (index >= 0 && _allowed[index] > 0)
            {
                _allowed[index] = _allowed[index] - 1;
            }
        }

        private void Add(RgbaColor color, int delta)
        {
            var index = IndexOf(color);
            if (index < 0)
            {
                _colors.Add(color);
                _allowed.Add(delta);
                return;
            }

            var value = _allowed[index] + delta;
            _allowed[index] = value < 0 ? 0 : value;
        }

        private int IndexOf(RgbaColor color)
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
