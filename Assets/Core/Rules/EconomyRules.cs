namespace ColorFlow.Core
{
    /// <summary>
    /// Conversion score → pièces d'or, à la fin d'une partie.
    /// Le barème est volontairement généreux : une partie perdue rapporte
    /// toujours un peu, pour donner envie de rejouer.
    /// </summary>
    public static class EconomyRules
    {
        /// <summary>Nombre de points de score nécessaires pour une pièce.</summary>
        public const int ScorePerCoin = 40;

        /// <summary>Prime fixe en cas de victoire.</summary>
        public const int WinBonus = 20;

        /// <summary>Consolation minimale en cas de défaite.</summary>
        public const int LossConsolation = 3;

        public static int CoinsForRun(int score, bool won)
        {
            var safeScore = score > 0 ? score : 0;
            return safeScore / ScorePerCoin + (won ? WinBonus : LossConsolation);
        }
    }
}
