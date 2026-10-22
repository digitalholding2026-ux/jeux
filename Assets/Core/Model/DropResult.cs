using System.Collections.Generic;

namespace ColorFlow.Core
{
    /// <summary>Raison pour laquelle un glisser-déposer n'a pas pu être appliqué.</summary>
    public enum DropFailureReason
    {
        None = 0,
        NoCreature,
        GameNotPlaying,
        UnknownCreature,
        NotInPattern,
        CellAlreadyPainted,
        CellOccupied,
        NoEffect
    }

    /// <summary>Une case peinte par une action (et sa couleur définitive).</summary>
    public sealed class PaintedCell
    {
        public PaintedCell(int x, int y, RgbaColor color)
        {
            X = x;
            Y = y;
            Color = color;
        }

        public int X { get; }

        public int Y { get; }

        public RgbaColor Color { get; }
    }

    /// <summary>
    /// Résultat complet d'un glisser-déposer : placement, alignement détecté,
    /// cases peintes et éventuel blob spécial créé.
    /// </summary>
    public sealed class DropResult
    {
        private DropResult(DropFailureReason failure)
        {
            Failure = failure;
        }

        public DropFailureReason Failure { get; private set; }

        public bool IsSuccess => Failure == DropFailureReason.None;

        /// <summary>Le blob a-t-il été accepté par la grille ?</summary>
        public bool Placed { get; private set; }

        /// <summary>Un alignement d'au moins 3 blobs a-t-il été résolu ?</summary>
        public bool Matched { get; private set; }

        public int X { get; private set; }

        public int Y { get; private set; }

        /// <summary>Couleur du blob joué / de l'alignement.</summary>
        public RgbaColor Color { get; private set; } = RgbaColor.Transparent;

        /// <summary>Longueur du plus grand alignement (0 si aucun).</summary>
        public int MatchLength { get; private set; }

        public bool HasSpecial { get; private set; }

        /// <summary>Blob spécial créé par l'alignement (4 = ligne, 5 = bombe).</summary>
        public CreatureKind SpecialKind { get; private set; } = CreatureKind.Normal;

        public bool LevelCompleted { get; private set; }

        /// <summary>Cases peintes par l'action, dans l'ordre de peinture.</summary>
        public List<PaintedCell> PaintedCells { get; } = new List<PaintedCell>();

        /// <summary>Cases qui faisaient partie de l'alignement (pour les explosions).</summary>
        public List<PaintedCell> MatchedCells { get; } = new List<PaintedCell>();

        public int PaintedCount => PaintedCells.Count;

        public static DropResult Fail(DropFailureReason reason)
        {
            return new DropResult(reason);
        }

        public static DropResult Ok(int x, int y, RgbaColor color)
        {
            return new DropResult(DropFailureReason.None)
            {
                Placed = true,
                X = x,
                Y = y,
                Color = color
            };
        }

        public DropResult WithMatch(int x, int y, RgbaColor color, int length, CreatureKind special)
        {
            Matched = true;
            X = x;
            Y = y;
            Color = color;
            MatchLength = length;
            HasSpecial = special != CreatureKind.Normal;
            SpecialKind = special;
            return this;
        }

        public DropResult SetLevelCompleted(bool completed)
        {
            LevelCompleted = completed;
            return this;
        }
    }
}
