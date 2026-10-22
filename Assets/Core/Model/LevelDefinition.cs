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
            float spawnInterval,
            GoalType goalType = GoalType.FillAll,
            int targetScore = 0,
            float timeLimitSeconds = 0f)
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

            GoalType = goalType;
            if (goalType == GoalType.ScoreTarget && targetScore <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetScore), "Un objectif de score doit être strictement positif.");
            }

            TargetScore = goalType == GoalType.ScoreTarget ? targetScore : 0;

            // Valeurs par défaut du cahier des charges : 60 s pour le score,
            // 30 s pour la course au remplissage.
            if (timeLimitSeconds <= 0f && GoalRules.HasTimeLimit(goalType))
            {
                timeLimitSeconds = GoalRules.DefaultTimeLimit(goalType);
            }

            TimeLimitSeconds = GoalRules.HasTimeLimit(goalType) ? timeLimitSeconds : 0f;

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

        public GoalType GoalType { get; }

        /// <summary>Score à atteindre (objectif <see cref="GoalType.ScoreTarget"/>).</summary>
        public int TargetScore { get; }

        /// <summary>Durée maximale en secondes (0 = pas de limite).</summary>
        public float TimeLimitSeconds { get; }

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

        /// <summary>Libellé de l'objectif, prêt à afficher.</summary>
        public string GoalDescription => GoalRules.Describe(GoalType, TargetScore, TimeLimitSeconds);
    }
}
