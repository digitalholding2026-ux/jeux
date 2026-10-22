using System;
using ColorFlow.Core;
using UnityEngine;

namespace ColorFlow.Game.Data
{
    /// <summary>
    /// Niveau décrit en données (ScriptableObject) : aucune dimension ni motif
    /// n'est codé en dur dans le contrôleur.
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "Color Flow/Niveau", order = 0)]
    public sealed class LevelData : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField] private string levelId = "niveau";
        [SerializeField] private string displayName = "Niveau";

        [Header("Motif")]
        [Tooltip("Une ligne par rangée. '.' = pas de pixel, '1'..'9' = index dans la palette.")]
        [SerializeField] private string[] rows = new string[0];

        [Tooltip("Couleurs hexadécimales (ex. #FF7A59). Le chiffre 1 des lignes renvoie à palette[0].")]
        [SerializeField] private string[] palette = new string[0];

        [Header("Rythme")]
        [Tooltip("Secondes entre deux créatures.")]
        [SerializeField, Min(0.2f)] private float spawnInterval = 1.6f;

        [Header("Objectif")]
        [Tooltip("FillAll : remplir le motif. ScoreTarget : atteindre un score en un temps limité. TimeAttack : remplir avant la limite.")]
        [SerializeField] private GoalType goalType = GoalType.FillAll;

        [Tooltip("Score à atteindre (objectif ScoreTarget uniquement).")]
        [SerializeField, Min(0)] private int targetScore = 300;

        [Tooltip("Durée maximale en secondes. 0 = valeur par défaut de l'objectif (60 s score, 30 s course).")]
        [SerializeField, Min(0f)] private float timeLimitSeconds = 0f;

        public string LevelId => levelId;

        public string DisplayName => displayName;

        public float SpawnInterval => spawnInterval;

        public GoalType GoalType => goalType;

        public int TargetScore => targetScore;

        public float TimeLimitSeconds => timeLimitSeconds;

        public string[] Rows => rows;

        public string[] Palette => palette;

        /// <summary>Libellé d'objectif prêt à afficher (menu et HUD).</summary>
        public string GoalDescription => GoalRules.Describe(
            goalType,
            targetScore,
            timeLimitSeconds > 0f ? timeLimitSeconds : GoalRules.DefaultTimeLimit(goalType));

        /// <summary>Convertit les données Unity en modèle du domaine (Core).</summary>
        public bool TryBuild(out LevelDefinition definition, out string error)
        {
            return LevelBuilder.TryFromRows(
                levelId,
                displayName,
                rows,
                palette,
                spawnInterval,
                goalType,
                targetScore,
                timeLimitSeconds,
                out definition,
                out error);
        }

        public LevelDefinition ToDefinition()
        {
            if (!TryBuild(out var definition, out var error))
            {
                throw new InvalidOperationException("LevelData '" + name + "' invalide : " + error);
            }

            return definition;
        }
    }
}
