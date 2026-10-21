using System;
using System.Collections.Generic;

namespace ColorFlow.Core
{
    /// <summary>
    /// Description immuable d'un niveau, indépendante d'Unity.
    /// Les cases sont stockées en ligne-major : index = y * Width + x.
    /// Une case transparente signifie "pas de pixel cible".
    /// </summary>
    public sealed class LevelDefinition
    {
        private readonly RgbaColor[] _cells;
        private readonly RgbaColor[] _palette;

        public LevelDefinition(
            string id,
            string displayName,
            int width,
            int height,
            RgbaColor[] cells,
            RgbaColor[] palette,
            float spawnInterval)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("La grille doit avoir des dimensions strictement positives.");
            }

            if (cells == null || cells.Length != width * height)
            {
                throw new ArgumentException("Le tableau de cases ne correspond pas aux dimensions de la grille.", nameof(cells));
            }

            if (spawnInterval <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnInterval), "L'intervalle de spawn doit être strictement positif.");
            }

            Id = string.IsNullOrEmpty(id) ? "niveau" : id;
            DisplayName = string.IsNullOrEmpty(displayName) ? "Niveau" : displayName;
            Width = width;
            Height = height;
            _cells = (RgbaColor[])cells.Clone();
            _palette = palette != null ? (RgbaColor[])palette.Clone() : new RgbaColor[0];
            SpawnInterval = spawnInterval;

            var targets = 0;
            for (var i = 0; i < _cells.Length; i++)
            {
                if (!_cells[i].IsTransparent)
                {
                    targets++;
                }
            }

            TargetPixelCount = targets;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public int Width { get; }

        public int Height { get; }

        public float SpawnInterval { get; }

        public int TargetPixelCount { get; }

        public IReadOnlyList<RgbaColor> Palette => _palette;

        public bool IsInside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public RgbaColor CellAt(int x, int y)
        {
            if (!IsInside(x, y))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Coordonnées hors de la grille (" + x + ", " + y + ").");
            }

            return _cells[y * Width + x];
        }

        public bool HasPixelAt(int x, int y)
        {
            return IsInside(x, y) && !_cells[y * Width + x].IsTransparent;
        }
    }
}
