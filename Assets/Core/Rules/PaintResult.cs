namespace ColorFlow.Core
{
    /// <summary>Résultat d'une tentative de peinture (immuable).</summary>
    public readonly struct PaintResult
    {
        private PaintResult(
            bool isSuccess,
            int x,
            int y,
            RgbaColor color,
            PaintFailureReason failure,
            bool levelCompleted,
            int paintedCount,
            CreatureKind kind)
        {
            IsSuccess = isSuccess;
            X = x;
            Y = y;
            Color = color;
            Failure = failure;
            LevelCompleted = levelCompleted;
            PaintedCount = paintedCount;
            Kind = kind;
        }

        public bool IsSuccess { get; }

        public int X { get; }

        public int Y { get; }

        public RgbaColor Color { get; }

        public PaintFailureReason Failure { get; }

        /// <summary>Vrai si cette peinture termine le motif.</summary>
        public bool LevelCompleted { get; }

        /// <summary>Nombre de pixels peints par cette action (bombe et ligne > 1).</summary>
        public int PaintedCount { get; }

        /// <summary>Type de la créature à l'origine de l'action.</summary>
        public CreatureKind Kind { get; }

        public static PaintResult Ok(int x, int y, RgbaColor color, bool levelCompleted)
        {
            return Ok(x, y, color, levelCompleted, 1, CreatureKind.Normal);
        }

        public static PaintResult Ok(int x, int y, RgbaColor color, bool levelCompleted, int paintedCount, CreatureKind kind)
        {
            return new PaintResult(true, x, y, color, PaintFailureReason.None, levelCompleted, paintedCount < 1 ? 1 : paintedCount, kind);
        }

        public static PaintResult Fail(PaintFailureReason reason)
        {
            return new PaintResult(false, -1, -1, RgbaColor.Transparent, reason, false, 0, CreatureKind.Normal);
        }

        public override string ToString()
        {
            return IsSuccess
                ? "Paint OK (" + X + ", " + Y + ") " + Color.ToHex() + " ×" + PaintedCount + (LevelCompleted ? " [victoire]" : string.Empty)
                : "Paint refusé : " + Failure;
        }
    }
}
