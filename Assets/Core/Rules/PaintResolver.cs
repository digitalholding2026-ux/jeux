using System;

namespace ColorFlow.Core
{
    /// <summary>
    /// Logique pure de résolution d'un tap : décide quel pixel peindre,
    /// ou pourquoi l'action est invalide. Aucune référence Unity.
    /// </summary>
    public sealed class PaintResolver
    {
        /// <summary>
        /// Applique la couleur de la créature au prochain pixel compatible de la grille.
        /// </summary>
        public PaintResult Resolve(Creature creature, GridModel grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (creature == null)
            {
                return PaintResult.Fail(PaintFailureReason.NoCreature);
            }

            if (grid.IsComplete)
            {
                return PaintResult.Fail(PaintFailureReason.LevelComplete);
            }

            if (!grid.TryPaintNext(creature.Color, out var x, out var y))
            {
                return PaintResult.Fail(PaintFailureReason.NoCompatiblePixel);
            }

            return PaintResult.Ok(x, y, creature.Color, grid.IsComplete);
        }
    }
}
