using System.Globalization;

namespace ColorFlow.Core
{
    /// <summary>Évaluation et description des objectifs de niveau.</summary>
    public static class GoalRules
    {
        /// <summary>Vrai si l'objectif est atteint.</summary>
        public static bool IsReached(GoalType goal, bool gridComplete, int score, int targetScore)
        {
            switch (goal)
            {
                case GoalType.ScoreTarget:
                    return score >= targetScore;
                default:
                    return gridComplete;
            }
        }

        /// <summary>Vrai si le niveau est soumis à une limite de temps.</summary>
        public static bool HasTimeLimit(GoalType goal)
        {
            return goal == GoalType.ScoreTarget || goal == GoalType.TimeAttack;
        }

        /// <summary>Durée par défaut d'un objectif (60 s score, 30 s course, 0 sinon).</summary>
        public static float DefaultTimeLimit(GoalType goal)
        {
            switch (goal)
            {
                case GoalType.ScoreTarget:
                    return 60f;
                case GoalType.TimeAttack:
                    return 30f;
                default:
                    return 0f;
            }
        }

        /// <summary>Libellé court de l'objectif, affiché dans le HUD.</summary>
        public static string Describe(GoalType goal, int targetScore, float timeLimitSeconds)
        {
            var seconds = FormatSeconds(timeLimitSeconds);

            switch (goal)
            {
                case GoalType.ScoreTarget:
                    return "Objectif : " + targetScore + " pts en " + seconds + " s";
                case GoalType.TimeAttack:
                    return "Objectif : remplir en " + seconds + " s";
                default:
                    return "Objectif : remplir tout le motif";
            }
        }

        /// <summary>Raison de l'échec, affichée sur l'écran de fin.</summary>
        public static string DescribeFailure(GoalType goal, int score, int targetScore, float timeLimitSeconds)
        {
            switch (goal)
            {
                case GoalType.ScoreTarget:
                    return score >= targetScore
                        ? "Objectif atteint : " + targetScore + " pts"
                        : "Objectif manqué : " + score + " / " + targetScore + " pts";
                case GoalType.TimeAttack:
                    return "Objectif manqué : temps écoulé (" + FormatSeconds(timeLimitSeconds) + " s)";
                default:
                    return "Objectif manqué : motif incomplet";
            }
        }

        private static string FormatSeconds(float seconds)
        {
            return seconds.ToString("0", CultureInfo.InvariantCulture);
        }
    }
}
