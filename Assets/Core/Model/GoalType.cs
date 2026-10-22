namespace ColorFlow.Core
{
    /// <summary>Objectif d'un niveau.</summary>
    public enum GoalType
    {
        /// <summary>Remplir tout le motif (mode historique).</summary>
        FillAll = 0,

        /// <summary>Atteindre un score cible avant la fin du temps imparti.</summary>
        ScoreTarget = 1,

        /// <summary>Remplir le motif avant la fin du temps imparti.</summary>
        TimeAttack = 2
    }
}
