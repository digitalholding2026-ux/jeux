namespace ColorFlow.Core
{
    /// <summary>
    /// Barème de score et fenêtre de combo. Sprint 2 : la profondeur vient du
    /// choix de l'ordre des taps (enchaîner la même couleur paie jusqu'à ×4).
    /// </summary>
    public static class ScoreRules
    {
        /// <summary>Points de base par pixel peint.</summary>
        public const int PointsPerPixel = 10;

        /// <summary>Deux taps de la même couleur à moins de 1,5 s enchaînent un combo.</summary>
        public const float ComboWindowSeconds = 1.5f;

        /// <summary>Multiplicateur maximal du combo.</summary>
        public const int MaxMultiplier = 4;

        /// <summary>Multiplicateur pour une chaîne de N taps identiques consécutifs.</summary>
        public static int MultiplierFor(int chain)
        {
            if (chain < 1)
            {
                return 1;
            }

            return chain > MaxMultiplier ? MaxMultiplier : chain;
        }

        /// <summary>Points gagnés pour une action.</summary>
        public static int PointsFor(int paintedPixels, int multiplier)
        {
            var pixels = paintedPixels > 0 ? paintedPixels : 0;
            var factor = multiplier < 1 ? 1 : (multiplier > MaxMultiplier ? MaxMultiplier : multiplier);
            return pixels * PointsPerPixel * factor;
        }
    }
}
