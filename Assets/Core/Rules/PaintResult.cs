namespace ColorFlow.Core
{
    /// <summary>Résultat d'une tentative de peinture (immuable).</summary>
    public readonly struct PaintResult
    {
        private PaintResult(bool isSuccess, int x, int y, RgbaColor color, PaintFailureReason failure, bool levelCompleted)
        {
            IsSuccess = isSuccess;
            X = x;
            Y = y;
            Color = color;
            Failure = failure;
            LevelCompleted = levelCompleted;
        }

        public bool IsSuccess { get; }

        public int X { get; }

        public int Y { get; }

        public RgbaColor Color { get; }

        public PaintFailureReason Failure { get; }

        /// <summary>Vrai si cette peinture termine le motif.</summary>
        public bool LevelCompleted { get; }

        public static PaintResult Ok(int x, int y, RgbaColor color, bool levelCompleted)
        {
            return new PaintResult(true, x, y, color, PaintFailureReason.None, levelCompleted);
        }

        public static PaintResult Fail(PaintFailureReason reason)
        {
            return new PaintResult(false, -1, -1, RgbaColor.Transparent, reason, false);
        }

        public override string ToString()
        {
            return IsSuccess
                ? "Paint OK (" + X + ", " + Y + ") " + Color.ToHex() + (LevelCompleted ? " [victoire]" : string.Empty)
                : "Paint refusé : " + Failure;
        }
    }
}
