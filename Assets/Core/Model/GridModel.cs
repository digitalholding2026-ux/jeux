using System;

namespace ColorFlow.Core
{
    /// <summary>
    /// Grille N×M : motif cible + état de remplissage.
    /// Toute la logique de recherche de pixel est row-major (haut-gauche → bas-droite),
    /// ce qui rend le comportement déterministe et testable.
    /// </summary>
    public sealed class GridModel
    {
        private readonly RgbaColor[] _targets;
        private readonly bool[] _painted;

        public GridModel(LevelDefinition level)
            : this(level.Width, level.Height, CopyTargets(level))
        {
        }

        public GridModel(int width, int height, RgbaColor[] targets)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("La grille doit avoir des dimensions strictement positives.");
            }

            if (targets == null || targets.Length != width * height)
            {
                throw new ArgumentException("Le tableau de cibles ne correspond pas aux dimensions.", nameof(targets));
            }

            Width = width;
            Height = height;
            _targets = (RgbaColor[])targets.Clone();
            _painted = new bool[targets.Length];

            var count = 0;
            for (var i = 0; i < _targets.Length; i++)
            {
                if (!_targets[i].IsTransparent)
                {
                    count++;
                }
            }

            TargetCount = count;
        }

        /// <summary>Déclenché après chaque peinture réussie.</summary>
        public event Action<int, int, RgbaColor> PixelPainted;

        public int Width { get; }

        public int Height { get; }

        public int TargetCount { get; }

        public int PaintedCount { get; private set; }

        public int RemainingCount => TargetCount - PaintedCount;

        public bool IsComplete => TargetCount > 0 && PaintedCount >= TargetCount;

        public bool IsInside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public bool HasTargetAt(int x, int y)
        {
            return IsInside(x, y) && !_targets[y * Width + x].IsTransparent;
        }

        public RgbaColor TargetColorAt(int x, int y)
        {
            return IsInside(x, y) ? _targets[y * Width + x] : RgbaColor.Transparent;
        }

        public bool IsPainted(int x, int y)
        {
            return IsInside(x, y) && _painted[y * Width + x];
        }

        /// <summary>
        /// Cherche le prochain pixel cible non peint de la couleur demandée,
        /// en parcours row-major. Retourne false si aucun pixel compatible.
        /// </summary>
        public bool TryFindNextPixel(RgbaColor color, out int x, out int y)
        {
            x = -1;
            y = -1;

            if (color.IsTransparent)
            {
                return false;
            }

            for (var i = 0; i < _targets.Length; i++)
            {
                if (_painted[i] || !_targets[i].Equals(color))
                {
                    continue;
                }

                x = i % Width;
                y = i / Width;
                return true;
            }

            return false;
        }

        /// <summary>Peint une case précise si la couleur correspond au motif.</summary>
        public bool TryPaint(int x, int y, RgbaColor color)
        {
            if (!IsInside(x, y))
            {
                return false;
            }

            var index = y * Width + x;
            if (_painted[index] || _targets[index].IsTransparent)
            {
                return false;
            }

            if (!_targets[index].Equals(color))
            {
                return false;
            }

            return PaintIndex(index, x, y);
        }

        /// <summary>
        /// Peint la case cible sans vérifier la couleur fournie : utilisé par les
        /// créatures spéciales (bombe, ligne, arc-en-ciel) qui peignent plusieurs
        /// pixels d'un coup. La couleur peinte reste celle du motif.
        /// </summary>
        public bool TryPaintTarget(int x, int y)
        {
            if (!IsInside(x, y))
            {
                return false;
            }

            var index = y * Width + x;
            if (_painted[index] || _targets[index].IsTransparent)
            {
                return false;
            }

            return PaintIndex(index, x, y);
        }

        /// <summary>Premier pixel cible non peint, toutes couleurs confondues.</summary>
        public bool TryFindNextAnyPixel(out int x, out int y)
        {
            for (var i = 0; i < _targets.Length; i++)
            {
                if (_painted[i] || _targets[i].IsTransparent)
                {
                    continue;
                }

                x = i % Width;
                y = i / Width;
                return true;
            }

            x = -1;
            y = -1;
            return false;
        }

        private bool PaintIndex(int index, int x, int y)
        {
            _painted[index] = true;
            PaintedCount++;
            PixelPainted?.Invoke(x, y, _targets[index]);
            return true;
        }

        /// <summary>Peint le prochain pixel compatible avec la couleur.</summary>
        public bool TryPaintNext(RgbaColor color, out int x, out int y)
        {
            if (!TryFindNextPixel(color, out x, out y))
            {
                return false;
            }

            return TryPaint(x, y, color);
        }

        private static RgbaColor[] CopyTargets(LevelDefinition level)
        {
            var targets = new RgbaColor[level.Width * level.Height];
            for (var y = 0; y < level.Height; y++)
            {
                for (var x = 0; x < level.Width; x++)
                {
                    targets[y * level.Width + x] = level.CellAt(x, y);
                }
            }

            return targets;
        }
    }
}
